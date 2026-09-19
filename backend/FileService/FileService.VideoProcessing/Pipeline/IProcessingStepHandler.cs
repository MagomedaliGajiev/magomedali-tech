using FileService.Domain.Processing;

namespace FileService.VideoProcessing.Pipeline;

public interface IProcessingStepHandler
{
    StepType StepType { get; }

    Task<ProcessingResult> ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken = default);
}
