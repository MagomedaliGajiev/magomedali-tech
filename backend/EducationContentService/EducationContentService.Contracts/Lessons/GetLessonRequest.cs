namespace EducationContentService.Contracts.Lessons;

public record GetLessonRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = GetLessonRequest.DEFAULT_PAGE_SIZE)
{
    public const int DEFAULT_PAGE_SIZE = 20;
    public const int MAX_PAGE_SIZE = 100;
}