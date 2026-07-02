using EducationContentService.Core.Features.Lessons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationContentService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILessonsRepository, LessonsRepository>();

        services.AddDbContext<EducationDbContext>();

        return services;
    }
}