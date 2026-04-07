using MAFPRO.Application.Interfaces;
using MAFPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MAFPRO.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection") 
                              ?? "Data Source=mafpro.db"));

        services.AddScoped<IConversationRepository, ConversationRepository>();

        return services;
    }
}
