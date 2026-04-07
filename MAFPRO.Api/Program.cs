using MAFPRO.Agents;
using MAFPRO.Application;
using MAFPRO.Application.Interfaces;
using MAFPRO.Application.Models;
using MAFPRO.Infrastructure;
using MAFPRO.Infrastructure.Persistence;
using MAFPRO.Api;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Architecture Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAgents();

// Add Observability
builder.Services.AddObservability();

// Add Microsoft.Extensions.AI ChatClient (Mock or Azure)
// In a real scenario, use endpoints from configuration
var openAIApiKey = builder.Configuration["AzureOpenAI:ApiKey"] ?? "mock-key";
var openAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? "https://mock.openai.azure.com/";

// Configurar AzureOpenAIChatClient si hay llaves válidas, si no mockearlo o tirar excepción.
// Aquí de demostración inyectamos un cliente genérico. Si fuese real, se usaría:
// IChatClient client = new AzureOpenAIClient(new Uri(openAIEndpoint), new ApiKeyCredential(openAIApiKey))
//    .AsChatClient("gpt-4o");

// Mocking IChatClient for compilation/demo without touching real API Keys unless provided
builder.Services.AddSingleton<IChatClient>(sp => {
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

apiGroup.MapGet("/{id}/history", async (Guid id, IConversationRepository repository) =>
{
    var conversation = await repository.GetByIdAsync(id);
    if (conversation == null) return Results.NotFound();
    return Results.Ok(new { Status = conversation.Status, Messages = conversation.Messages.Select(m => new { m.Role, m.Content }) });
});

// Create DB Schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

// Mock client for local run without Azure setup
class MockChatClient : IChatClient
{
    public void Dispose() { }
    
    public ChatClientMetadata Metadata => new ChatClientMetadata("MockProvider", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Mock response!")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
