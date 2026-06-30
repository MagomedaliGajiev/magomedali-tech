using CSharpFunctionalExtensions;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Core.Features.Lessons;

public interface ILessonsRepository
{
    Task<Result<Guid, Error>> AddAsync(Lesson lesson);
}