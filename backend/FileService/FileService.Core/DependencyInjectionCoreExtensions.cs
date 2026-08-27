using FileService.Core.Features;
using FileService.Core.FilesStorage;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FileService.Core;

public static class DependencyInjectionCoreExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<StartMultipartUploadHandler>();
        services.AddScoped<CompleteMultipartUploadHandler>();
        services.AddScoped<DeleteMediaAssetHandler>();
        services.AddScoped<CheckMediaAssetExistsHandler>();
        services.AddScoped<GetMediaAsset.GetMediaAssetUploadHandler>();
        services.AddScoped<GetMediaAssetsUploadHandler>();
        services.AddScoped<PresignedUrlCache>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjectionCoreExtensions).Assembly);
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
