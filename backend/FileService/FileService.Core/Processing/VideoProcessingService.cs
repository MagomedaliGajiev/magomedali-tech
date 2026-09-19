using CSharpFunctionalExtensions;
using FileService.Domain;
using FileService.Domain.Processing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Core.Processing;

// Called by a step executor after it has performed the actual media operation.
public sealed class VideoProcessingService
{
    private readonly IVideoProcessesRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(
        IVideoProcessesRepository repository,
        TimeProvider timeProvider,
        ILogger<VideoProcessingService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<ProcessingStep, Error>> ProcessNextStep(
        Guid processId,
        CancellationToken cancellationToken = default)
    {
        Result<VideoProcess, Error> processResult = await _repository.GetById(processId, cancellationToken);
        if (processResult.IsFailure)
            return processResult.Error;

        Result<ProcessingStep, Error> result = processResult.Value.ProcessNextStep(_timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return result.Error;

        UnitResult<Error> saveResult = await _repository.SaveAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Started step {StepType} of process {ProcessId}, attempt {AttemptId}, retry {RetryCount}",
            result.Value.Type, processId, result.Value.AttemptId, result.Value.RetryCount);
        return result;
    }

    public async Task<UnitResult<Error>> CompleteCurrentStep(
        Guid processId,
        Guid stepId,
        Guid attemptId,
        string? resultData,
        StorageKey? finalKey = null,
        StorageKey? previewKey = null,
        CancellationToken cancellationToken = default)
    {
        Result<VideoProcess, Error> processResult = await _repository.GetById(processId, cancellationToken);
        if (processResult.IsFailure)
            return processResult.Error;

        VideoProcess process = processResult.Value;
        UnitResult<Error> result = process.CompleteCurrentStep(
            stepId, attemptId, resultData, _timeProvider.GetUtcNow().UtcDateTime, finalKey, previewKey);
        if (result.IsFailure)
            return result.Error;

        UnitResult<Error> saveResult = await _repository.SaveAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogInformation(
            "Completed step {StepId} of process {ProcessId}, progress {Progress}, status {Status}",
            stepId, processId, process.Progress, process.Status);
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> FailCurrentStep(
        Guid processId,
        Guid stepId,
        Guid attemptId,
        string errorMessage,
        bool isCriticalFailure,
        CancellationToken cancellationToken = default)
    {
        Result<VideoProcess, Error> processResult = await _repository.GetById(processId, cancellationToken);
        if (processResult.IsFailure)
            return processResult.Error;

        VideoProcess process = processResult.Value;
        UnitResult<Error> result = process.FailCurrentStep(
            stepId, attemptId, errorMessage, isCriticalFailure, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
            return result.Error;

        UnitResult<Error> saveResult = await _repository.SaveAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        _logger.LogWarning(
            "Failed step {StepId} of process {ProcessId}: {ErrorMessage}. Status {Status}, next retry {NextRetryAt}",
            stepId, processId, errorMessage, process.Status, process.CurrentStep?.NextRetryAt);
        return UnitResult.Success<Error>();
    }
}
