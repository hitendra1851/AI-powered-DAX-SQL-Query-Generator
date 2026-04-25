using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Parsers;
using QueryMind.Infrastructure.Persistence;
using QueryMind.Infrastructure.Services;

namespace QueryMind.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<QueryMindDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"),
                npgsql => npgsql.MigrationsAssembly(typeof(QueryMindDbContext).Assembly.FullName)));

        services.AddScoped<IStorageService, BlobStorageService>();
        services.AddScoped<ISchemaSearchService, AzureSearchService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<StripeWebhookService>();

        // Register all schema parsers
        services.AddScoped<ISchemaParser, PbixJsonParser>();
        services.AddScoped<ISchemaParser, SqlDdlParser>();
        services.AddScoped<ISchemaParser, CsvHeadersParser>();
        services.AddScoped<ISchemaParser, TabularBimParser>();
        services.AddScoped<ISchemaParser, FabricLakehouseParser>();
        services.AddScoped<ISchemaParser, SoqlParser>();
        services.AddScoped<SchemaParserFactory>();

        return services;
    }
}
