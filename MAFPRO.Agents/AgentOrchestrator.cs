using MAFPRO.Application.Interfaces;
using MAFPRO.Application.Models;
using Microsoft.Extensions.AI;

namespace MAFPRO.Agents;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly IConversationRepository _repository;

    public AgentOrchestrator(IChatClient chatClient, IConversationRepository repository)
    {
        _chatClient = chatClient;
        _repository = repository;
    }

    public async Task<Conversation> ProcessWorkflowAsync(Guid conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            conversation = new Conversation { Id = conversationId, Title = "New Workflow" };
            await _repository.AddAsync(conversation, cancellationToken);
        }

        if (conversation.Status == "WaitingForHuman")
        {
            throw new InvalidOperationException("The workflow is waiting for a human approval.");
        }

        var userMsg = new Message { Id = Guid.NewGuid(), Role = "User", Content = userMessage, ConversationId = conversation.Id };
        conversation.Messages.Add(userMsg);

        // Check if a human needs to be inserted into the loop
        if (userMessage.Contains("approve", StringComparison.OrdinalIgnoreCase) || userMessage.Contains("deploy", StringComparison.OrdinalIgnoreCase))
        {
            conversation.Status = "WaitingForHuman";
            conversation.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                Role = "System",
                Content = "The action requires human approval. Please approve or reject to continue.",
                ConversationId = conversation.Id
            });
            await _repository.UpdateAsync(conversation, cancellationToken);
            return conversation;
        }

        var response = await _chatClient.GetResponseAsync(
            BuildAiMessagesWithPrompt(conversation.Messages, await _repository.GetActivePromptAsync("default", cancellationToken)),
            BuildChatOptions(),
            cancellationToken);

        var aiResponseMsg = new Message
        {
            Id = Guid.NewGuid(),
            Role = "Assistant",
            Content = response.Text ?? response.Messages[0].Text ?? string.Empty,
            ConversationId = conversation.Id
        };
        conversation.Messages.Add(aiResponseMsg);
        
        await _repository.UpdateAsync(conversation, cancellationToken);

        return conversation;
    }

    public async Task<Conversation> ApproveWorkflowAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null) throw new KeyNotFoundException("Conversation not found.");

        if (conversation.Status != "WaitingForHuman")
        {
            throw new InvalidOperationException("Not waiting for human approval.");
        }

        conversation.Status = "Active";
        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "System",
            Content = "Human approved the action. Continue execution.",
            ConversationId = conversation.Id
        });

        var response = await _chatClient.GetResponseAsync(
            BuildAiMessagesWithPrompt(conversation.Messages, await _repository.GetActivePromptAsync("default", cancellationToken)),
            BuildChatOptions(),
            cancellationToken);

        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "Assistant",
            Content = response.Messages[0].Text ?? response.Text ?? string.Empty,
            ConversationId = conversation.Id
        });
        
        await _repository.UpdateAsync(conversation, cancellationToken);

        return conversation;
    }

    public async Task<Conversation> RejectWorkflowAsync(Guid conversationId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null) throw new KeyNotFoundException("Conversation not found.");

        if (conversation.Status != "WaitingForHuman")
        {
            throw new InvalidOperationException("Not waiting for human approval.");
        }

        conversation.Status = "Rejected";
        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "System",
            Content = string.IsNullOrWhiteSpace(reason)
                ? "Human rejected the action. Workflow stopped."
                : $"Human rejected the action. Reason: {reason}",
            ConversationId = conversation.Id
        });

        await _repository.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }

    private static List<ChatMessage> BuildAiMessagesWithPrompt(IEnumerable<Message> messages, PromptTemplate? activePrompt)
    {
        var aiMessages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(activePrompt?.Content))
        {
            aiMessages.Add(new ChatMessage(ChatRole.System, activePrompt.Content));
        }

        aiMessages.AddRange(messages.Select(m =>
            new ChatMessage(m.Role == "User" ? ChatRole.User : (m.Role == "System" ? ChatRole.System : ChatRole.Assistant), m.Content)));

        return aiMessages;
    }

    private static ChatOptions BuildChatOptions()
    {
        return new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(new Tools.WeatherTool().GetWeather),
                AIFunctionFactory.Create(new Tools.DateTimeTool().GetCurrentDateTime),
                AIFunctionFactory.Create(new Tools.MathTool().Add)
            ]
        };
    }
}
