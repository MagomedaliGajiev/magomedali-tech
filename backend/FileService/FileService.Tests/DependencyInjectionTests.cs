using FileService.Core;
using FileService.Core.Features;
using FileService.Core.FilesStorage;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FileService.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void MultipartUploadDependenciesCanBeResolved()
    {
        var configurationValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] =
                "Host=localhost;Port=5432;Database=file_service;Username=postgres;Password=postgres",
            ["S3Options:Endpoint"] = "http://localhost:9000",
            ["S3Options:AccessKey"] = "access",
            ["S3Options:SecretKey"] = "secret",
            ["S3Options:MaxConcurrentRequests"] = "4"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddCore();
        services.AddInfrastructurePostgres(configuration);
        services.AddS3(configuration);

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<StartMultipartUploadHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CompleteMultipartUploadHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IMediaAssetsRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<FileServiceDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IFileStorageProvider>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IChunkSizeCalculator>());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "FileService.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}