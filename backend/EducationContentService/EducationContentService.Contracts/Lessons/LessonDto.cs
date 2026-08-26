namespace EducationContentService.Contracts.Lessons;

public record LessonDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public MediaDto? Video { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public record MediaDto
{
    public Guid Id { get; init; }

    public string? Url { get; init; }

    public string Status { get; init; } = string.Empty;
}
