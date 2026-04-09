using MAFPRO.Application.Interfaces;
using MAFPRO.Application.Models;
using Microsoft.Extensions.AI;

namespace MAFPRO.Agents;

public class CustomAgentOrchestrator : ICustomAgentOrchestrator
{
    private readonly CustomProviderChatClient _customChatClient;
    private readonly IConversationRepository _repository;

    public CustomAgentOrchestrator(CustomProviderChatClient customChatClient, IConversationRepository repository)
    {
        _customChatClient = customChatClient;
        _repository = repository;
    }

    public async Task<Conversation> ProcessWorkflowAsync(Guid conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            conversation = new Conversation { Id = conversationId, Title = "Custom Provider Workflow" };
            await _repository.AddAsync(conversation, cancellationToken);
        }

        if (conversation.Status.StartsWith("WaitingForHumanStep", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Custom workflow is in human approval process. Approve or reject to continue.");
        }

        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "User",
            Content = userMessage,
            ConversationId = conversation.Id
        });

        var toolMessage = InvokeToolIfRequested(userMessage);
        if (!string.IsNullOrWhiteSpace(toolMessage))
        {
            conversation.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                Role = "Tool",
                Content = toolMessage,
                ConversationId = conversation.Id
            });
        }

        if (RequiresHumanApproval(userMessage))
        {
            conversation.Status = "WaitingForHumanStep1";
            conversation.Messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                Role = "System",
                Content = "Human in the loop started (Step 1/3): validate intent and risk.",
                ConversationId = conversation.Id
            });
            await _repository.UpdateAsync(conversation, cancellationToken);
            return conversation;
        }

        var response = await _customChatClient.GetResponseAsync(
            BuildMessagesWithPrompt(conversation.Messages, await _repository.GetActivePromptAsync("custom", cancellationToken)),
            BuildToolsOptions(),
            cancellationToken);

        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "Assistant",
            Content = response.Text ?? response.Messages[0].Text ?? string.Empty,
            ConversationId = conversation.Id
        });
        await _repository.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }

    public async Task<Conversation> ApproveWorkflowAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null) throw new KeyNotFoundException("Conversation not found.");

        conversation.Status = conversation.Status switch
        {
            "WaitingForHumanStep1" => "WaitingForHumanStep2",
            "WaitingForHumanStep2" => "WaitingForHumanStep3",
            "WaitingForHumanStep3" => "Active",
            _ => throw new InvalidOperationException("Workflow is not waiting for human approval.")
        };

        if (conversation.Status == "WaitingForHumanStep2")
        {
            conversation.Messages.Add(new Message { Id = Guid.NewGuid(), Role = "System", Content = "Step 1 approved. Step 2/3: compliance review pending.", ConversationId = conversation.Id });
            await _repository.UpdateAsync(conversation, cancellationToken);
            return conversation;
        }

        if (conversation.Status == "WaitingForHumanStep3")
        {
            conversation.Messages.Add(new Message { Id = Guid.NewGuid(), Role = "System", Content = "Step 2 approved. Step 3/3: final execution approval pending.", ConversationId = conversation.Id });
            await _repository.UpdateAsync(conversation, cancellationToken);
            return conversation;
        }

        conversation.Messages.Add(new Message { Id = Guid.NewGuid(), Role = "System", Content = "Step 3 approved. Executing with custom AI provider.", ConversationId = conversation.Id });
        var response = await _customChatClient.GetResponseAsync(
            BuildMessagesWithPrompt(conversation.Messages, await _repository.GetActivePromptAsync("custom", cancellationToken)),
            BuildToolsOptions(),
            cancellationToken);
        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "Assistant",
            Content = response.Text ?? response.Messages[0].Text ?? string.Empty,
            ConversationId = conversation.Id
        });

        await _repository.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }

    public async Task<Conversation> RejectWorkflowAsync(Guid conversationId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null) throw new KeyNotFoundException("Conversation not found.");
        if (!conversation.Status.StartsWith("WaitingForHumanStep", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Workflow is not waiting for human approval.");
        }

        conversation.Status = "Rejected";
        conversation.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Role = "System",
            Content = string.IsNullOrWhiteSpace(reason)
                ? "Human rejected the custom workflow."
                : $"Human rejected the custom workflow. Reason: {reason}",
            ConversationId = conversation.Id
        });
        await _repository.UpdateAsync(conversation, cancellationToken);
        return conversation;
    }

    private static bool RequiresHumanApproval(string text) =>
        text.Contains("deploy", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("execute", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("transfer", StringComparison.OrdinalIgnoreCase);

    private static ChatOptions BuildToolsOptions() => new()
    {
        Tools =
        [
            AIFunctionFactory.Create(new Tools.WeatherTool().GetWeather),
            AIFunctionFactory.Create(new Tools.DateTimeTool().GetCurrentDateTime),
            AIFunctionFactory.Create(new Tools.MathTool().Add)
        ]
    };

    private static List<ChatMessage> BuildMessagesWithPrompt(IEnumerable<Message> messages, PromptTemplate? activePrompt)
    {
        var aiMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(activePrompt?.Content))
        {
            aiMessages.Add(new ChatMessage(ChatRole.System, activePrompt.Content));
        }

        aiMessages.AddRange(messages.Select(m =>
            new ChatMessage(
                m.Role == "User" ? ChatRole.User :
                m.Role == "System" ? ChatRole.System :
                m.Role == "Tool" ? ChatRole.System : ChatRole.Assistant,
                m.Content)));
        return aiMessages;
    }

    private static string? InvokeToolIfRequested(string message)
    {
        if (message.Contains("weather", StringComparison.OrdinalIgnoreCase) || message.Contains("clima", StringComparison.OrdinalIgnoreCase))
        {
            return $"WeatherTool: {new Tools.WeatherTool().GetWeather("Mexico City")}";
        }

        if (message.Contains("hora", StringComparison.OrdinalIgnoreCase) || message.Contains("fecha", StringComparison.OrdinalIgnoreCase) || message.Contains("time", StringComparison.OrdinalIgnoreCase))
        {
            return $"DateTimeTool: {new Tools.DateTimeTool().GetCurrentDateTime("UTC")}";
        }

        if (message.Contains("sum", StringComparison.OrdinalIgnoreCase) || message.Contains("suma", StringComparison.OrdinalIgnoreCase))
        {
            return $"MathTool: 8 + 13 = {new Tools.MathTool().Add(8, 13)}";
        }

        return null;
    }
}
