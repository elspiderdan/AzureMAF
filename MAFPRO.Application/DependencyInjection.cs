using Microsoft.Extensions.DependencyInjection;

namespace MAFPRO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add Application specific services, CQRS handlers, MediatR, etc.
        return services;
    }
}
