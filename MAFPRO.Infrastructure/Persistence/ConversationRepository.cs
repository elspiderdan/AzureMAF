using MAFPRO.Application.Interfaces;
using MAFPRO.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace MAFPRO.Infrastructure.Persistence;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation> AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<Conversation> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        // EF Core puede estar forzando el estado a Modified al añadir objetos a la colección rastreada de una Conversación.
        // Como todos los mensajes nuevos deben ser INSERTADOS, nos aseguramos iterando y forzando explícitamente su estado a Added:
        foreach (var msg in conversation.Messages)
        {
            var entry = _context.Entry(msg);
            // Si el mensaje es una entidad existente no hay problema (Unchanged). 
            // Si EF lo marcó erróneamente, lo forzamos a Added para que genere el INSERT y no un UPDATE de 0 rows.
            if (entry.State == EntityState.Modified || entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
