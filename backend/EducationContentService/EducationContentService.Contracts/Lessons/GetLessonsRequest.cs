namespace EducationContentService.Contracts.Lessons;

public record GetLessonsRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = GetLessonsRequest.DEFAULT_PAGE_SIZE,
    bool IsDeleted = false)
{
    public const int DEFAULT_PAGE_SIZE = 20;
    public const int MAX_PAGE_SIZE = 100;
}