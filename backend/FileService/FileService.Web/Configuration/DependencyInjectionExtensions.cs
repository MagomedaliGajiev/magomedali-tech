using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Framework.Endpoints;
using Framework.Logging;
using Framework.Swagger;
using Microsoft.Extensions.Caching.Hybrid;

namespace FileService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors();
        services
            .AddSerilogLogging(configuration, "FileService")
            .AddOpenApiSpec("FileService", "v1")
            .AddEndpoints(typeof(DependencyInjectionCoreExtensions).Assembly)
            .AddS3(configuration);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Missing Redis connection string.");
            options.InstanceName = "file-service:";
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Expiration = TimeSpan.FromMinutes(30)
            };
        });

        services
            .AddCore()
            .AddInfrastructurePostgres(configuration);

        return services;
    }
}
