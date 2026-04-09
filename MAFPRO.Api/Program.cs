using MAFPRO.Agents;
using MAFPRO.Application;
using MAFPRO.Application.Interfaces;
using MAFPRO.Application.Models;
using MAFPRO.Infrastructure;
using MAFPRO.Infrastructure.Persistence;
using MAFPRO.Api;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add Architecture Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAgents();

// Add Observability
builder.Services.AddObservability();

// Add Microsoft.Extensions.AI ChatClient (Azure when configured, otherwise Mock)
var openAIApiKey = builder.Configuration["AzureOpenAI:ApiKey"];
var openAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureDeployment = builder.Configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";

// Register IChatClient: Azure OpenAI if fully configured, otherwise MockChatClient
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var isAzureConfigured =
        !string.IsNullOrWhiteSpace(openAIEndpoint) &&
        !string.IsNullOrWhiteSpace(openAIApiKey);

    if (isAzureConfigured)
    {
        try
        {
            logger.LogInformation("Azure OpenAI configured. Endpoint: {Endpoint}, Deployment: {Deployment}", openAIEndpoint, azureDeployment);

            var azureClient = new OpenAI.Chat.ChatClient(
                    azureDeployment!,
                    new ApiKeyCredential(openAIApiKey!),
                    new OpenAIClientOptions { Endpoint = new Uri(openAIEndpoint!) })
                .AsIChatClient();

            var toolEnabledClient = new ChatClientBuilder(azureClient)
                .UseFunctionInvocation()
                .Build();

            return new FallbackChatClient(toolEnabledClient, new MockChatClient(), logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not initialize Azure OpenAI ({Error}). Falling back to MockChatClient.", ex.Message);
        }
    }
    else
    {
        logger.LogWarning("Azure OpenAI is not fully configured (Endpoint/ApiKey). Using MockChatClient.");
    }

    return new MockChatClient();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// --- Minimal API Endpoints ---

var apiGroup = app.MapGroup("/api/workflows").WithTags("Workflows");

apiGroup.MapPost("/{id}/chat", async (Guid id, string message, IAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.ProcessWorkflowAsync(id, message);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

apiGroup.MapPost("/{id}/approve", async (Guid id, IAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.ApproveWorkflowAsync(id);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

apiGroup.MapPost("/{id}/reject", async (Guid id, string? reason, IAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.RejectWorkflowAsync(id, reason);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

apiGroup.MapGet("/{id}/history", async (Guid id, IConversationRepository repository) =>
{
    var conversation = await repository.GetByIdAsync(id);
    if (conversation == null) return Results.NotFound();
    return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
});

var customApiGroup = app.MapGroup("/api/custom-workflows").WithTags("Custom Workflows");

customApiGroup.MapPost("/{id}/chat", async (Guid id, string message, ICustomAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.ProcessWorkflowAsync(id, message);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

customApiGroup.MapPost("/{id}/approve", async (Guid id, ICustomAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.ApproveWorkflowAsync(id);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

customApiGroup.MapPost("/{id}/reject", async (Guid id, string? reason, ICustomAgentOrchestrator orchestrator) =>
{
    try
    {
        var conversation = await orchestrator.RejectWorkflowAsync(id, reason);
        return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

customApiGroup.MapGet("/{id}/history", async (Guid id, IConversationRepository repository) =>
{
    var conversation = await repository.GetByIdAsync(id);
    if (conversation == null) return Results.NotFound();
    return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
});

var promptsGroup = app.MapGroup("/api/prompts").WithTags("Prompts");

promptsGroup.MapGet("/", async (string? agent, IConversationRepository repository) =>
{
    var agentName = string.IsNullOrWhiteSpace(agent) ? "default" : agent;
    var prompts = await repository.GetPromptsAsync(agentName);
    var active = await repository.GetActivePromptAsync(agentName);
    return Results.Ok(new
    {
        Agent = agentName,
        ActivePromptId = active?.Id,
        Items = prompts.Select(p => new { p.Id, p.AgentName, p.Version, p.Content, p.IsActive, p.CreatedAt })
    });
});

promptsGroup.MapPost("/", async (CreatePromptRequest request, IConversationRepository repository) =>
{
    if (string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.Version))
    {
        return Results.BadRequest(new { Error = "Version and content are required." });
    }

    var prompt = await repository.AddPromptAsync(new PromptTemplate
    {
        Id = Guid.NewGuid(),
        AgentName = string.IsNullOrWhiteSpace(request.AgentName) ? "default" : request.AgentName,
        Version = request.Version.Trim(),
        Content = request.Content.Trim(),
        IsActive = false
    });

    return Results.Ok(new { prompt.Id, prompt.AgentName, prompt.Version, prompt.Content, prompt.IsActive, prompt.CreatedAt });
});

promptsGroup.MapPost("/{id}/activate", async (Guid id, string? agent, IConversationRepository repository) =>
{
    var agentName = string.IsNullOrWhiteSpace(agent) ? "default" : agent;
    var active = await repository.SetActivePromptAsync(agentName, id);
    if (active == null) return Results.NotFound();
    return Results.Ok(new { ActivePromptId = active.Id, active.AgentName, active.Version });
});

// Create DB Schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[PromptTemplates]', N'U') IS NULL
        BEGIN
            CREATE TABLE [PromptTemplates](
                [Id] uniqueidentifier NOT NULL,
                [AgentName] nvarchar(450) NOT NULL,
                [Version] nvarchar(450) NOT NULL,
                [Content] nvarchar(max) NOT NULL,
                [IsActive] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_PromptTemplates] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_PromptTemplates_AgentName_Version] ON [PromptTemplates] ([AgentName], [Version]);
        END
        """);

    if (!db.PromptTemplates.Any(p => p.AgentName == "default"))
    {
        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            AgentName = "default",
            Version = "v1",
            Content = "You are an assistant specialized in workflow orchestration. Be concise, safe, and transparent about assumptions. Use tools when they help answer accurately.",
            IsActive = true
        });
    }

    if (!db.PromptTemplates.Any(p => p.AgentName == "custom"))
    {
        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            AgentName = "custom",
            Version = "v1",
            Content = "You are a custom-provider workflow agent operating through APIM. Be explicit, safe, and require human validation for risky operations.",
            IsActive = true
        });
    }

    db.SaveChanges();
}

app.Run();

// Mock client for local run without Azure setup
class MockChatClient : IChatClient
{
    public void Dispose() { }
    
    public ChatClientMetadata Metadata => new ChatClientMetadata("MockProvider", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        var response = string.IsNullOrWhiteSpace(lastUserMessage)
            ? "Mock response: no user message received."
            : $"Mock response: received '{lastUserMessage}'. Configure AzureOpenAI settings to get a real model answer.";

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}

record CreatePromptRequest(string? AgentName, string Version, string Content);

class FallbackChatClient : IChatClient
{
    private readonly IChatClient _primary;
    private readonly IChatClient _fallback;
    private readonly ILogger _logger;

    public FallbackChatClient(IChatClient primary, IChatClient fallback, ILogger logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public ChatClientMetadata Metadata => new ChatClientMetadata("FallbackProvider", new Uri("http://localhost"), "fallback-client");

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Azure OpenAI returned 404 (deployment or endpoint issue). Falling back to mock response.");
            return await _fallback.GetResponseAsync(chatMessages, options, cancellationToken);
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<ChatResponseUpdate> stream;
        try
        {
            stream = _primary.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Azure OpenAI streaming returned 404. Falling back to mock streaming response.");
            stream = _fallback.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        }

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        _primary.GetService(serviceType, serviceKey) ?? _fallback.GetService(serviceType, serviceKey);

    public void Dispose()
    {
        _primary.Dispose();
        _fallback.Dispose();
    }
}
