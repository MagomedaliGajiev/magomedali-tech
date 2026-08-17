namespace EducationContentService.Contracts.Lessons;

public record PaginationLessonResponse(
    IReadOnlyList<LessonDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);