using FileService.Core;
using FileService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructurePostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMediaAssetsRepository, MediaAssetsRepository>();

        services.AddDbContextPool<FileServiceDbContext>((serviceProvider, options) =>
        {
            string connectionString = configuration.GetConnectionString(Constants.DATABASE)
                ?? throw new InvalidOperationException("Connection string 'Database' is missing.");
            IHostEnvironment hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(connectionString);
            options.UseLoggerFactory(loggerFactory);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        services.AddScoped<IReadDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<FileServiceDbContext>());

        return services;
    }
}