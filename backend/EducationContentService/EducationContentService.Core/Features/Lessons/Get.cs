using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Core.Endpoints;
using EducationContentService.Core.Validation;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EducationContentService.Core.Features.Lessons;

public class GetLessonRequestValidator : AbstractValidator<GetLessonRequest>
{
    public GetLessonRequestValidator()
    {
        RuleFor(r => r.Search)
            .MaximumLength(1000)
            .WithError(GeneralErrors.ValueIsInvalid("search"));

        RuleFor(r => r.Page)
            .GreaterThan(0)
            .WithError(GeneralErrors.ValueIsInvalid("page"));

        RuleFor(r => r.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(GetLessonRequest.MaxPageSize)
            .WithError(GeneralErrors.ValueIsInvalid("pageSize"));
    }
}

public sealed class GetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessons", async Task<EndpointResult<PaginationLessonResponse>> (
            [AsParameters] GetLessonRequest request,
            [FromServices] GetHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class GetHandler
{
    private readonly IEducationReadDbContext _readDbContext;
    private readonly IValidator<GetLessonRequest> _validator;

    public GetHandler(
        IEducationReadDbContext readDbContext,
        IValidator<GetLessonRequest> validator)
    {
        _readDbContext = readDbContext;
        _validator = validator;
    }

    public async Task<Result<PaginationLessonResponse, Error>> Handle(GetLessonRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        IQueryable<Lesson> query = _readDbContext.LessonsQuery;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // EF Core translates ToLower()/Contains into a SQL LOWER(...) LIKE expression.
            // The culture/comparison analyzer overloads can't be translated, so they don't apply here.
#pragma warning disable CA1304, CA1311, CA1862
            string search = request.Search.ToLower();
            query = query.Where(l => l.Title.Value.ToLower().Contains(search));
#pragma warning restore CA1304, CA1311, CA1862
        }

        int lessonsCount = await query.CountAsync(cancellationToken);

        int skip = (int)Math.Min((long)(request.Page - 1) * request.PageSize, int.MaxValue);

        List<LessonDto> lessons = await query
            .OrderBy(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(l => new LessonDto
            {
                Id = l.Id,
                Title = l.Title.Value,
                Description = l.Description.Value,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PaginationLessonResponse(lessons, lessonsCount);
    }
}