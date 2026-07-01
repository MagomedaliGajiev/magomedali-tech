using CSharpFunctionalExtensions;
using EducationContentService.Core.Features.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Infrastructure.Postgres;

public class LessonsRepository : ILessonsRepository
{
    public Task<Result<Guid, Error>> AddAsync(Lesson lesson) => throw new NotImplementedException();
}