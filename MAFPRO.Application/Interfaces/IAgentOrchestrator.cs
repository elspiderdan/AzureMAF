using MAFPRO.Application.Models;

namespace MAFPRO.Application.Interfaces;

public interface IAgentOrchestrator
{
    // Start or continue a workflow for a conversation
    Task<Conversation> ProcessWorkflowAsync(Guid conversationId, string userMessage, CancellationToken cancellationToken = default);
    
    // Human approves a waiting step
    Task<Conversation> ApproveWorkflowAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<Conversation> RejectWorkflowAsync(Guid conversationId, string? reason = null, CancellationToken cancellationToken = default);
}
