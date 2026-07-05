namespace EducationContentService.Contracts.Lessons;

public record GetLessonRequest(string? Search = null, int Page = 1, int PageSize = GetLessonRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}