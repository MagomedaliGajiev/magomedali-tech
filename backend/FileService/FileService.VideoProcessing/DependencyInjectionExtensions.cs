using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FileService.VideoProcessing;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddVideoProcessing(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IProcessingPipeline, ProcessingPipeline>();
        services.AddScoped<VideoProcessingService>();
        return services;
    }

    public static IServiceCollection AddProcessingStep<THandler>(this IServiceCollection services)
        where THandler : class, IProcessingStepHandler
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IProcessingStepHandler, THandler>());
        return services;
    }
}
