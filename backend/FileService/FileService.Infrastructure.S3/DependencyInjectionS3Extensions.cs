using Amazon.Runtime;
using Amazon.S3;
using FileService.Core.FilesStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class DependencyInjectionS3Extensions
{
    public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(nameof(S3Options)))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "S3 endpoint is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AccessKey), "S3 access key is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey), "S3 secret key is required.")
            .Validate(options => options.DownloadUrlExpirationDays > 0, "Download URL expiration must be positive.")
            .Validate(options => options.MaxConcurrentRequests > 0, "Max concurrent requests must be positive.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            S3Options s3Options = serviceProvider.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.Endpoint,
                UseHttp = !s3Options.WithSsl,
                ForcePathStyle = true,
            };

            var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);

            return new AmazonS3Client(credentials, config);
        });

        services.AddScoped<IFileStorageProvider, S3Provider>();

        services.AddHostedService<S3BucketInitializationService>();

        services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();

        return services;
    }
}