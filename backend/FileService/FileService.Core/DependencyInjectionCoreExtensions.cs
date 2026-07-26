using FileService.Core.Features;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Core;

public static class DependencyInjectionCoreExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<StartMultipartUploadHandler>();
        services.AddScoped<CompleteMultipartUploadHandler>();
        services.AddScoped<GetMediaAsset.GetMediaAssetUploadHandler>();
        services.AddScoped<GetMediaAssetsUploadHandler>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjectionCoreExtensions).Assembly);

        return services;
    }
}