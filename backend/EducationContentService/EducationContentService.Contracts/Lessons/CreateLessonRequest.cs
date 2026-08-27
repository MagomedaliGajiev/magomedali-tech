namespace EducationContentService.Contracts.Lessons;

public record CreateLessonRequest(Guid Id, string Title, string Description, Guid? VideoId);
