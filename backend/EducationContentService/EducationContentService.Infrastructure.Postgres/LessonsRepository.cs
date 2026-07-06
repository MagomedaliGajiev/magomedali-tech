using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using EducationContentService.Core.Features.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Infrastructure.Postgres.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.SharedKernel;
using EducationErrors = EducationContentService.Domain.Shared.EducationErrors;

namespace EducationContentService.Infrastructure.Postgres;

public class LessonsRepository : ILessonsRepository
{
    private readonly EducationDbContext _dbContext;
    private readonly ILogger<LessonsRepository> _logger;

    public LessonsRepository(EducationDbContext dbContext, ILogger<LessonsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _dbContext.Lessons.Add(lesson);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return lesson.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            if (pgEx is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: not null } &&
                pgEx.ConstraintName.Contains(LessonIndexes.TITLE, StringComparison.InvariantCultureIgnoreCase))
            {
                return EducationErrors.TitleConflict(lesson.Title.Value);
            }

            _logger.LogError(ex, "Database update error while creating lesson with title {Title}", lesson.Title.Value);

            return EducationErrors.DatabaseError();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was cancelled while creating lesson with title {Title}", lesson.Title.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating lesson with title {Title}",
                lesson.Title.Value);
            return EducationErrors.DatabaseError();
        }
    }

    public async Task<Result<Guid, Error>> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return lesson.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            if (pgEx is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: not null } &&
                pgEx.ConstraintName.Contains(LessonIndexes.TITLE, StringComparison.InvariantCultureIgnoreCase))
            {
                return EducationErrors.TitleConflict(lesson.Title.Value);
            }

            _logger.LogError(ex, "Database update error while updating lesson {Id}", lesson.Id);

            return EducationErrors.DatabaseError();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was cancelled while updating lesson {Id}", lesson.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating lesson {Id}", lesson.Id);
            return EducationErrors.DatabaseError();
        }
    }

    public async Task<Result<Lesson, Error>> GetBy(
        Expression<Func<Lesson, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        Lesson? lesson = await _dbContext.Lessons.FirstOrDefaultAsync(predicate, cancellationToken);

        if (lesson is null)
            return GeneralErrors.NotFound(null, "lesson");

        return lesson;
    }
}