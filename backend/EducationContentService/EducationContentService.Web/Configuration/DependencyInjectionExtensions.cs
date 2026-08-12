using EducationContentService.Core;
using EducationContentService.Infrastructure.Postgres;
using Framework.Endpoints;
using Framework.Logging;
using Framework.Swagger;

namespace EducationContentService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors();
        services
            .AddSerilogLogging(configuration, "EducationContentService")
            .AddOpenApiSpec("EducationContentService", "v1")
            .AddEndpoints(typeof(DependencyInjectionCoreExtensions).Assembly);

        services
            .AddCore(configuration)
            .AddInfrastructurePostgres(configuration);

        return services;
    }
}