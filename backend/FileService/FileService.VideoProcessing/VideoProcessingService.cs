using CSharpFunctionalExtensions;
using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.VideoProcessing;

public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;

    private readonly IProcessingPipeline _pipeline;

    public VideoProcessingService(ILogger<VideoProcessingService> logger, IProcessingPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;
    }

    public async Task<UnitResult<Error>> ProcessVideoAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting video processing for VideoAssetId: {VideoAssetId}", videoAssetId);

        try
        {
            UnitResult<Error> result = await _pipeline.ProcessAllStepsAsync(videoAssetId, cancellationToken);
            if (result.IsFailure)
                _logger.LogWarning("Video processing failed for {VideoAssetId}: {Error}", videoAssetId, result.Error.GetMessage());
            else
                _logger.LogInformation("Completed video processing for {VideoAssetId}", videoAssetId);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Video processing cancelled for {VideoAssetId}", videoAssetId);
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing video {VideoAssetId}", videoAssetId);
            return GeneralErrors.Failure("Непредвиденная ошибка обработки видео");
        }
    }
}
