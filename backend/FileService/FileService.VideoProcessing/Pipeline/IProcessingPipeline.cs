using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.VideoProcessing.Pipeline;

public interface IProcessingPipeline
{
    Task<UnitResult<Error>> ProcessAllStepsAsync(Guid videoAssetId, CancellationToken cancellationToken = default);
}