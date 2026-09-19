using System.Data;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Assets;
using FileService.Domain.Processing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.VideoProcessing.Pipeline;

public class ProcessingPipeline : IProcessingPipeline
{
    private readonly IEnumerable<IProcessingStepHandler> _stepHandlers;

    private readonly ILogger<ProcessingPipeline> _logger;

    private readonly IVideoProcessingRepository _videoProcessingRepository;

    private readonly IMediaAssetsRepository _mediaAssetsRepository;

    private readonly ITransactionManager _transactionManager;

    private readonly TimeProvider _timeProvider;

    public ProcessingPipeline(
        ILogger<ProcessingPipeline> logger,
        IVideoProcessingRepository videoProcessingRepository,
        IMediaAssetsRepository mediaAssetsRepository,
        ITransactionManager transactionManager,
        IEnumerable<IProcessingStepHandler> stepHandlers,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _videoProcessingRepository = videoProcessingRepository;
        _mediaAssetsRepository = mediaAssetsRepository;
        _transactionManager = transactionManager;
        _stepHandlers = stepHandlers.ToArray();
        _timeProvider = timeProvider;
    }

    public async Task<UnitResult<Error>> ProcessAllStepsAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ProcessCoreAsync(videoAssetId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Processing cancelled for video {VideoAssetId}", videoAssetId);
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected pipeline error for video {VideoAssetId}", videoAssetId);
            return GeneralErrors.Failure("Непредвиденная ошибка обработки видео");
        }
    }

    private async Task<UnitResult<Error>> ProcessCoreAsync(Guid videoAssetId, CancellationToken cancellationToken)
    {
        Result<ProcessingContext, Error> contextResult = await LoadContextAsync(videoAssetId, cancellationToken);
        if (contextResult.IsFailure)
            return contextResult.Error;

        ProcessingContext context = contextResult.Value;
        VideoProcess process = context.VideoProcessing;
        if (process.Status == ProcessingStatus.COMPLETED)
            return UnitResult.Success<Error>();
        if (process.Status == ProcessingStatus.FAILED)
            return GeneralErrors.Failure(process.ErrorMessage ?? "Процесс обработки завершился с ошибкой");

        while (process.CurrentStep is { } nextStep)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IProcessingStepHandler[] handlers = _stepHandlers.Where(handler => handler.StepType == nextStep.Type).ToArray();
            if (handlers.Length != 1)
            {
                _logger.LogError("Expected one handler for {StepType}, found {Count}", nextStep.Type, handlers.Length);
                return GeneralErrors.Failure($"Для шага {nextStep.Type} должен быть зарегистрирован ровно один обработчик");
            }

            Result<ProcessingStep, Error> startResult = process.ProcessNextStep(_timeProvider.GetUtcNow().UtcDateTime);
            if (startResult.IsFailure)
                return startResult.Error;

            ProcessingStep step = startResult.Value;
            Guid attemptId = step.AttemptId!.Value;
            UnitResult<Error> saveResult = await SaveStateAsync(cancellationToken);
            if (saveResult.IsFailure)
                return saveResult.Error;

            _logger.LogInformation("Executing {StepType} of process {ProcessId}, attempt {AttemptId}", step.Type, process.Id, attemptId);

            ProcessingResult result;
            try
            {
                result = await ExecuteStepSafetyAsync(handlers[0], context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                UnitResult<Error> cancelResult = process.CancelCurrentStep(step.Id, attemptId, _timeProvider.GetUtcNow().UtcDateTime);
                if (cancelResult.IsFailure)
                    return cancelResult.Error;

                saveResult = await SaveStateAsync(CancellationToken.None);
                return saveResult.IsFailure ? saveResult.Error : GeneralErrors.OperationCancelled();
            }

            if (result.IsSuccess)
            {
                context = result.Context;
                UnitResult<Error> completeResult = CompleteStep(context, step, attemptId, result.ResultData);
                if (completeResult.IsFailure)
                    result = ProcessingResult.Failure(completeResult.Error);
            }

            if (result.IsFailure)
            {
                UnitResult<Error> failureResult = result.IsCritical
                    ? process.FailCurrentStep(step.Id, attemptId, result.Error.GetMessage(), true, _timeProvider.GetUtcNow().UtcDateTime)
                    : process.SkipCurrentStep(step.Id, attemptId, result.Error.GetMessage(), _timeProvider.GetUtcNow().UtcDateTime);

                // The final upload cannot be skipped: no usable video would be produced.
                if (failureResult.IsFailure && !result.IsCritical)
                {
                    result = ProcessingResult.Failure(GeneralErrors.Failure(
                        $"{result.Error.GetMessage()}. {failureResult.Error.GetMessage()}"));
                    failureResult = process.FailCurrentStep(
                        step.Id, attemptId, result.Error.GetMessage(), true, _timeProvider.GetUtcNow().UtcDateTime);
                }

                if (failureResult.IsFailure)
                    return failureResult.Error;

                _logger.LogWarning(
                    "Step {StepType} of process {ProcessId} failed. Critical: {IsCritical}. {Error}",
                    step.Type, process.Id, result.IsCritical, result.Error.GetMessage());
            }

            // Persist the outcome even if cancellation arrives after the handler has finished.
            saveResult = await SaveStateAsync(CancellationToken.None);
            if (saveResult.IsFailure)
                return saveResult.Error;
            if (result.IsFailure && result.IsCritical)
                return result.Error;
        }

        return UnitResult.Success<Error>();
    }

    private async Task<ProcessingResult> ExecuteStepSafetyAsync(
        IProcessingStepHandler handler,
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            ProcessingResult result = await handler.ExecuteAsync(context, cancellationToken);
            if (result.IsSuccess &&
                (!ReferenceEquals(result.Context.VideoAsset, context.VideoAsset) ||
                 !ReferenceEquals(result.Context.VideoProcessing, context.VideoProcessing)))
            {
                return ProcessingResult.Failure(GeneralErrors.Failure("Обработчик вернул контекст другого процесса"));
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {StepType} of process {ProcessId}", handler.StepType, context.VideoProcessing.Id);
            return ProcessingResult.Failure(GeneralErrors.Failure("Непредвиденная ошибка выполнения шага"));
        }
    }

    private UnitResult<Error> CompleteStep(ProcessingContext context, ProcessingStep step, Guid attemptId, string? resultData)
    {
        if (context.Metadata is not null && context.Metadata != context.VideoAsset.Metadata)
        {
            UnitResult<Error> metadataResult = context.VideoAsset.SetMetadata(context.Metadata);
            if (metadataResult.IsFailure)
                return metadataResult.Error;
        }

        bool isLastStep = context.VideoProcessing.Steps.All(item =>
            item.Id == step.Id || item.Status is StepStatus.COMPLETED or StepStatus.SKIPPED);
        return context.VideoProcessing.CompleteCurrentStep(
            step.Id, attemptId, resultData, _timeProvider.GetUtcNow().UtcDateTime,
            isLastStep ? context.FinalKey : null,
            isLastStep ? context.PreviewKey : null);
    }

    private async Task<UnitResult<Error>> SaveStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IDbTransaction transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
            Result<int, Error> result = await _transactionManager.SaveChangesAsync(cancellationToken);
            if (result.IsFailure)
            {
                transaction.Rollback();
                _logger.LogError("Failed to save processing state: {Error}", result.Error.GetMessage());
                return result.Error;
            }

            transaction.Commit();
            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit processing state");
            return GeneralErrors.DatabaseError();
        }
    }

    private async Task<Result<ProcessingContext, Error>> LoadContextAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> assetResult = await _mediaAssetsRepository
            .GetBy(asset => asset.Id == videoAssetId && asset.Status != MediaStatus.DELETED, cancellationToken);
        if (assetResult.IsFailure)
            return assetResult.Error;

        if (assetResult.Value is not VideoAsset videoAsset)
            return GeneralErrors.ValueIsInvalid(nameof(videoAssetId));

        VideoProcess? videoProcess = videoAsset.Process;
        if (videoProcess is null)
        {
            Result<VideoProcess, Error> processingResult = VideoProcess.Create(videoAsset, _timeProvider.GetUtcNow().UtcDateTime);
            if (processingResult.IsFailure)
                return processingResult.Error;

            videoProcess = processingResult.Value;
            _videoProcessingRepository.Add(videoProcess);

            UnitResult<Error> saveResult = await SaveStateAsync(cancellationToken);
            if (saveResult.IsFailure)
                return saveResult.Error;

            _logger.LogInformation("Created new VideoProcessing for VideoAssetId: {VideoAssetId}", videoAssetId);
        }

        return new ProcessingContext
        {
            VideoProcessing = videoProcess,
            VideoAsset = videoAsset,
            Metadata = videoAsset.Metadata,
            FinalKey = videoAsset.Key,
            PreviewKey = videoAsset.PreviewKey,
        };
    }
}
