using EducationContentService.Core.Database;
using EducationContentService.Core.Features.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EducationContentService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILessonsRepository, LessonsRepository>();

        void ConfigureDbContext(IServiceProvider sp, DbContextOptionsBuilder options)
        {
            string? connectionString = configuration.GetConnectionString(Constants.DATABASE);
            IHostEnvironment hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(connectionString);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseLoggerFactory(loggerFactory);
        }

        services.AddDbContextPool<EducationDbContext>(ConfigureDbContext);
        services.AddDbContextPool<IEducationReadDbContext, EducationDbContext>(ConfigureDbContext);

        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;
    }
}