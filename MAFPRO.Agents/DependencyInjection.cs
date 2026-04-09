using MAFPRO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MAFPRO.Agents;

public static class DependencyInjection
{
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddHttpClient<CustomProviderChatClient>();
        services.AddScoped<ICustomAgentOrchestrator, CustomAgentOrchestrator>();
        return services;
    }
}
