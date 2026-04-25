using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace QueryMind.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));
        return services;
    }
}
