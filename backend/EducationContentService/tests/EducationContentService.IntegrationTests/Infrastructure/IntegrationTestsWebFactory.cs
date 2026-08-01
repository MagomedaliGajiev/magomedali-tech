using CSharpFunctionalExtensions;
using EducationContentService.Core.Database;
using EducationContentService.Infrastructure.Postgres;
using EducationContentService.IntegrationTests.Mocks;
using FileService.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace EducationContentService.IntegrationTests.Infrastructure;

public class IntegrationTestsWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18.4")
        .WithDatabase("education_content_db_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        EducationDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<EducationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<EducationDbContext>();
            services.RemoveAll<IEducationReadDbContext>();

            services.AddDbContextPool<EducationDbContext>((_, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.AddDbContextPool<IEducationReadDbContext, EducationDbContext>((_, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            services.RemoveAll<IFileCommunicationService>();
            services.AddScoped<IFileCommunicationService, FileServiceCommunicationMock>();
        });
    }
}