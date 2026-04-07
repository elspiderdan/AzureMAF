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

        // Add user message to history
        var userMsg = new MAFPRO.Application.Models.Message { Id = Guid.NewGuid(), Role = "User", Content = userMessage, ConversationId = conversation.Id };
        conversation.Messages.Add(userMsg);

        // Convert db messages to AI messages
        var aiMessages = conversation.Messages.Select(m => 
            new ChatMessage(m.Role == "User" ? ChatRole.User : (m.Role == "System" ? ChatRole.System : ChatRole.Assistant), m.Content)).ToList();

        // Check if a human needs to be inserted into the loop
        if (userMessage.Contains("approve", StringComparison.OrdinalIgnoreCase) || userMessage.Contains("deploy", StringComparison.OrdinalIgnoreCase))
        {
            conversation.Status = "WaitingForHuman";
            conversation.Messages.Add(new MAFPRO.Application.Models.Message { Id = Guid.NewGuid(), Role = "System", Content = "The action requires human approval. Plase approve to continue.", ConversationId = conversation.Id });
            await _repository.UpdateAsync(conversation, cancellationToken);
            return conversation;
        }

        // Configure tools
        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(new Tools.WeatherTool().GetWeather)]
        };

        // Call the AI
        var response = await _chatClient.GetResponseAsync(aiMessages, chatOptions, cancellationToken);

        var aiResponseMsg = new MAFPRO.Application.Models.Message { Id = Guid.NewGuid(), Role = "Assistant", Content = response.Messages[0].Text ?? response.Text ?? "", ConversationId = conversation.Id };
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
        conversation.Messages.Add(new MAFPRO.Application.Models.Message { Id = Guid.NewGuid(), Role = "System", Content = "Human approved the action.", ConversationId = conversation.Id });

        // Agent can now continue its work
        var aiMessages = conversation.Messages.Select(m => 
            new ChatMessage(m.Role == "User" ? ChatRole.User : (m.Role == "System" ? ChatRole.System : ChatRole.Assistant), m.Content)).ToList();
            
        var response = await _chatClient.GetResponseAsync(aiMessages, null, cancellationToken);
        
        conversation.Messages.Add(new MAFPRO.Application.Models.Message { Id = Guid.NewGuid(), Role = "Assistant", Content = response.Messages[0].Text ?? response.Text ?? "", ConversationId = conversation.Id });
        
        await _repository.UpdateAsync(conversation, cancellationToken);

        return conversation;
    }
}
