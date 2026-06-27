using EducationContentService.Core.EndpointsSettings;
using Microsoft.OpenApi;

namespace EducationContentService.Core.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddOpenApiSpec()
            .AddEndpoints(typeof(Program).Assembly);
    }

    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Education Content Service",
                Version = "v1",
                Contact = new OpenApiContact
                {
                    Name = "Magomedali Gajiev",
                    Email = "mag198421@gmail.com"
                }
            });
        });

        return services;
    }
}