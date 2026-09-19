namespace FileService.Contracts.Dtos;

public record GetVideoProcessDto(
    Guid Id,
    Guid VideoAssetId,
    string Status,
    decimal Progress,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ProcessingStepDto> Steps);

public record ProcessingStepDto(
    Guid Id,
    Guid? AttemptId,
    string Type,
    string Status,
    int Order,
    int Weight,
    string? ResultData,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int RetryCount,
    DateTime? NextRetryAt,
    bool IsCriticalFailure);
