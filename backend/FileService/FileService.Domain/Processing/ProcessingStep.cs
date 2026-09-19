using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain.Processing;

public sealed class ProcessingStep
{
    public const int MAX_RETRIES = 3;

    public Guid Id { get; private set; }

    public Guid? AttemptId { get; private set; }

    public StepType Type { get; private set; }

    public StepStatus Status { get; private set; }

    public int Order { get; private set; }

    public int Weight { get; private set; }

    public string? ResultData { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public bool IsCriticalFailure { get; private set; }

    public bool CanRetry => Status == StepStatus.FAILED && !IsCriticalFailure && RetryCount < MAX_RETRIES;

    private ProcessingStep()
    {
    }

    public static Result<ProcessingStep, Error> Create(StepType type, int order, int weight)
    {
        if (!Enum.IsDefined(type))
            return GeneralErrors.ValueIsInvalid(nameof(type));
        if (order < 0)
            return GeneralErrors.ValueIsInvalid(nameof(order));
        if (weight <= 0)
            return GeneralErrors.ValueIsInvalid(nameof(weight));

        return new ProcessingStep
        {
            Id = Guid.NewGuid(),
            Type = type,
            Status = StepStatus.PENDING,
            Order = order,
            Weight = weight,
        };
    }

    public UnitResult<Error> Start(DateTime utcNow)
    {
        if (Status != StepStatus.PENDING)
            return GeneralErrors.Failure($"Нельзя запустить шаг в статусе {Status}");
        if (NextRetryAt > utcNow)
            return GeneralErrors.Failure("Время следующей попытки ещё не наступило");

        Status = StepStatus.IN_PROGRESS;
        AttemptId = Guid.NewGuid();
        StartedAt = utcNow;
        CompletedAt = null;
        NextRetryAt = null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Complete(string? resultData, DateTime utcNow)
    {
        if (Status != StepStatus.IN_PROGRESS)
            return GeneralErrors.Failure($"Нельзя завершить шаг в статусе {Status}");
        if (utcNow < StartedAt)
            return GeneralErrors.ValueIsInvalid(nameof(utcNow));

        Status = StepStatus.COMPLETED;
        ResultData = resultData;
        ErrorMessage = null;
        CompletedAt = utcNow;
        NextRetryAt = null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Fail(string errorMessage, bool isCriticalFailure, DateTime utcNow)
    {
        if (Status != StepStatus.IN_PROGRESS)
            return GeneralErrors.Failure($"Нельзя завершить с ошибкой шаг в статусе {Status}");
        if (string.IsNullOrWhiteSpace(errorMessage))
            return GeneralErrors.ValueIsRequired(nameof(errorMessage));
        if (utcNow < StartedAt)
            return GeneralErrors.ValueIsInvalid(nameof(utcNow));

        Status = StepStatus.FAILED;
        ErrorMessage = errorMessage;
        IsCriticalFailure = isCriticalFailure;
        ResultData = null;
        CompletedAt = utcNow;
        NextRetryAt = CanRetry ? utcNow.AddSeconds(30 * Math.Pow(2, RetryCount)) : null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Skip(string errorMessage, DateTime utcNow)
    {
        UnitResult<Error> result = Fail(errorMessage, false, utcNow);
        if (result.IsFailure)
            return result.Error;

        Status = StepStatus.SKIPPED;
        NextRetryAt = null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel()
    {
        if (Status != StepStatus.IN_PROGRESS)
            return GeneralErrors.Failure($"Нельзя отменить шаг в статусе {Status}");

        Status = StepStatus.PENDING;
        AttemptId = null;
        StartedAt = null;
        CompletedAt = null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reset(DateTime utcNow)
    {
        if (!CanRetry)
            return GeneralErrors.Failure("Повторная попытка для этого шага недоступна");
        if (NextRetryAt > utcNow)
            return GeneralErrors.Failure("Время следующей попытки ещё не наступило");

        RetryCount++;
        AttemptId = null;
        Status = StepStatus.PENDING;
        ResultData = null;
        ErrorMessage = null;
        StartedAt = null;
        CompletedAt = null;
        NextRetryAt = null;
        return UnitResult.Success<Error>();
    }
}
