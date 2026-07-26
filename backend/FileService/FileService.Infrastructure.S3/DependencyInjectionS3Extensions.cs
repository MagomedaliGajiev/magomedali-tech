using Amazon.Runtime;
using Amazon.S3;
using FileService.Core.FilesStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Infrastructure.S3;

public static class DependencyInjectionS3Extensions
{
    public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(nameof(S3Options)));

        S3Options s3Options = configuration.GetSection(nameof(S3Options)).Get<S3Options>()
            ?? throw new InvalidOperationException($"Missing {nameof(S3Options)}.{nameof(S3Options)}.");

        services.AddSingleton<IAmazonS3>(_ =>
        {
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