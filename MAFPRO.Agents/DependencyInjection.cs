using MAFPRO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MAFPRO.Agents;

public static class DependencyInjection
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        return services;
    }
}
