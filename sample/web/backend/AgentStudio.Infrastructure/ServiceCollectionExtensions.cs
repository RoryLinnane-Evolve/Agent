using AgentStudio.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentStudio.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentStudioInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentRuntimeOptions>(options =>
        {
            options.Model = configuration[$"{AgentRuntimeOptions.SectionName}:Model"] ?? options.Model;
        });
        services.AddSingleton<IAgentWorkspace, RagentWorkspace>();
        return services;
    }
}
