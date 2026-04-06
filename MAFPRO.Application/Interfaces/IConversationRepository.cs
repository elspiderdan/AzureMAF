using MAFPRO.Application.Models;

namespace MAFPRO.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation> AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
