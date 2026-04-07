using MAFPRO.Application.Models;

namespace MAFPRO.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation> AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<List<PromptTemplate>> GetPromptsAsync(string agentName, CancellationToken cancellationToken = default);
    Task<PromptTemplate?> GetActivePromptAsync(string agentName, CancellationToken cancellationToken = default);
    Task<PromptTemplate> AddPromptAsync(PromptTemplate promptTemplate, CancellationToken cancellationToken = default);
    Task<PromptTemplate?> SetActivePromptAsync(string agentName, Guid promptId, CancellationToken cancellationToken = default);
}
