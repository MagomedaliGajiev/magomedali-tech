using CSharpFunctionalExtensions;
using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public sealed class SoftDeleteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessons/{lessonId:guid}", async Task<EndpointResult<Guid>> (
            [FromRoute] Guid lessonId,
            [FromServices] SoftDeleteHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(lessonId, cancellationToken));
    }
}

public sealed class SoftDeleteHandler
{
    private readonly ILogger<SoftDeleteHandler> _logger;
    private readonly ITransactionManager _transactionManager;
    private readonly ILessonsRepository _lessonsRepository;

    public SoftDeleteHandler(
        ILogger<SoftDeleteHandler> logger,
        ITransactionManager transactionManager,
        ILessonsRepository lessonsRepository)
    {
        _logger = logger;
        _transactionManager = transactionManager;
        _lessonsRepository = lessonsRepository;
    }

    public async Task<Result<Guid, Error>> Handle(Guid lessonId, CancellationToken cancellationToken)
    {
        Result<Lesson, Error> lessonResult = await _lessonsRepository.GetBy(l => l.Id == lessonId, cancellationToken);
        if (lessonResult.IsFailure)
            return lessonResult.Error;

        lessonResult.Value.SoftDelete();

        try
        {
            await _transactionManager.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was cancelled while soft deleting lesson {Id}", lessonId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while soft deleting lesson {Id}", lessonId);
            return EducationErrors.DatabaseError();
        }

        _logger.LogInformation("Soft deleted lesson {Id}", lessonId);

        return lessonResult.Value.Id;
    }
}