using CSharpFunctionalExtensions;
using FileService.Domain.Assets;
using Shared.SharedKernel;

namespace FileService.Domain.Processing;

public sealed class VideoProcess
{
    private readonly List<ProcessingStep> _steps = [];

    public Guid Id { get; private set; }

    public Guid VideoAssetId { get; private set; }

    public VideoAsset VideoAsset { get; private set; } = null!;

    public ProcessingStatus Status { get; private set; }

    public decimal Progress { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    public Guid Version { get; private set; }

    public IReadOnlyList<ProcessingStep> Steps => _steps.AsReadOnly();

    public ProcessingStep? CurrentStep => _steps.OrderBy(step => step.Order)
        .FirstOrDefault(step => step.Status != StepStatus.COMPLETED);

    private VideoProcess()
    {
    }

    public static Result<VideoProcess, Error> Create(VideoAsset videoAsset, DateTime utcNow)
    {
        if (!videoAsset.RequiresProcessing() || videoAsset.Status != MediaStatus.UPLOADED)
            return GeneralErrors.Failure("Создать процесс можно только для загруженного видео, требующего обработки");
        if (videoAsset.Process is not null)
            return GeneralErrors.Failure("Для видео уже создан процесс обработки");

        var process = new VideoProcess
        {
            Id = Guid.NewGuid(),
            VideoAssetId = videoAsset.Id,
            VideoAsset = videoAsset,
            Status = ProcessingStatus.IN_PROGRESS,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            Version = Guid.NewGuid(),
        };
        process.InitializeSteps();
        videoAsset.AttachProcess(process);
        return process;
    }

    public UnitResult<Error> InitializeSteps()
    {
        if (_steps.Count != 0)
            return GeneralErrors.Failure("Шаги обработки уже инициализированы");

        _steps.Add(ProcessingStep.Create(StepType.VALIDATE, 0, 10).Value);
        _steps.Add(ProcessingStep.Create(StepType.TRANSCODE, 1, 65).Value);
        _steps.Add(ProcessingStep.Create(StepType.GENERATE_PREVIEW, 2, 15).Value);
        _steps.Add(ProcessingStep.Create(StepType.UPLOAD_RESULT, 3, 10).Value);
        RecalculateProgress();
        return UnitResult.Success<Error>();
    }

    public Result<ProcessingStep, Error> ProcessNextStep(DateTime utcNow)
    {
        if (Status != ProcessingStatus.IN_PROGRESS || VideoAsset.Status != MediaStatus.UPLOADED)
            return GeneralErrors.Failure("Процесс обработки недоступен для запуска шага");

        ProcessingStep? step = CurrentStep;
        if (step is null)
            return GeneralErrors.Failure("Нет следующего шага обработки");

        if (step.Status == StepStatus.FAILED)
        {
            UnitResult<Error> resetResult = step.Reset(utcNow);
            if (resetResult.IsFailure)
                return resetResult.Error;
        }

        UnitResult<Error> startResult = step.Start(utcNow);
        if (startResult.IsFailure)
            return startResult.Error;

        Touch(utcNow);
        return step;
    }

    public UnitResult<Error> CompleteCurrentStep(
        Guid stepId,
        Guid attemptId,
        string? resultData,
        DateTime utcNow,
        StorageKey? finalKey = null,
        StorageKey? previewKey = null)
    {
        ProcessingStep? step = CurrentStep;
        if (Status != ProcessingStatus.IN_PROGRESS || VideoAsset.Status != MediaStatus.UPLOADED ||
            step is null || step.Id != stepId || step.AttemptId != attemptId || step.Status != StepStatus.IN_PROGRESS)
        {
            return GeneralErrors.Failure("Указанный шаг сейчас не выполняется");
        }

        bool isLastStep = _steps.All(item => item.Id == stepId || item.Status == StepStatus.COMPLETED);
        if (isLastStep && finalKey is null)
            return GeneralErrors.ValueIsRequired(nameof(finalKey));
        if (!isLastStep && (finalKey is not null || previewKey is not null))
            return GeneralErrors.Failure("Ключи результата назначаются на последнем шаге");
        if (finalKey is not null && (finalKey == VideoAsset.RawKey || previewKey == finalKey ||
            (previewKey is not null && previewKey == VideoAsset.RawKey)))
        {
            return GeneralErrors.Failure("Исходник, результат и превью должны иметь разные ключи");
        }

        UnitResult<Error> result = step.Complete(resultData, utcNow);
        if (result.IsFailure)
            return result.Error;

        RecalculateProgress();
        Touch(utcNow);
        if (isLastStep)
        {
            Status = ProcessingStatus.COMPLETED;
            CompletedAt = utcNow;
            VideoAsset.SetProcessedKeys(finalKey!, previewKey);
            return VideoAsset.MarkReady();
        }

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> FailCurrentStep(
        Guid stepId,
        Guid attemptId,
        string errorMessage,
        bool isCriticalFailure,
        DateTime utcNow)
    {
        ProcessingStep? step = CurrentStep;
        if (Status != ProcessingStatus.IN_PROGRESS || VideoAsset.Status != MediaStatus.UPLOADED ||
            step is null || step.Id != stepId || step.AttemptId != attemptId)
        {
            return GeneralErrors.Failure("Указанный шаг сейчас не выполняется");
        }

        UnitResult<Error> result = step.Fail(errorMessage, isCriticalFailure, utcNow);
        if (result.IsFailure)
            return result.Error;

        if (!step.CanRetry)
        {
            Status = ProcessingStatus.FAILED;
            ErrorMessage = errorMessage;
            CompletedAt = utcNow;
            VideoAsset.MarkFailed();
        }

        RecalculateProgress();
        Touch(utcNow);
        return UnitResult.Success<Error>();
    }

    public void RecalculateProgress()
    {
        decimal totalWeight = _steps.Sum(step => (decimal)step.Weight);
        Progress = totalWeight == 0 ? 0 :
            _steps.Where(step => step.Status == StepStatus.COMPLETED).Sum(step => (decimal)step.Weight) / totalWeight * 100;
    }

    internal void Cancel(DateTime utcNow)
    {
        if (Status != ProcessingStatus.IN_PROGRESS)
            return;

        ErrorMessage = "Видео удалено";
        if (CurrentStep?.Status == StepStatus.IN_PROGRESS)
            CurrentStep.Fail(ErrorMessage, true, utcNow);

        Status = ProcessingStatus.FAILED;
        CompletedAt = utcNow;
        Touch(utcNow);
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAt = utcNow;
        Version = Guid.NewGuid();
    }
}
