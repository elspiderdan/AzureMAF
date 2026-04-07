using MAFPRO.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace MAFPRO.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<PromptTemplate> PromptTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId);

        modelBuilder.Entity<PromptTemplate>()
            .HasIndex(p => new { p.AgentName, p.Version })
            .IsUnique();
    }
}
