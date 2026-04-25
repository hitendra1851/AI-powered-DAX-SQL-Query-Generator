using Microsoft.Extensions.DependencyInjection;
using QueryMind.AI.Services;
using QueryMind.Domain.Interfaces;

namespace QueryMind.AI;

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        services.AddScoped<IQueryMindAgent, QueryMindAgentService>();
        return services;
    }
}
