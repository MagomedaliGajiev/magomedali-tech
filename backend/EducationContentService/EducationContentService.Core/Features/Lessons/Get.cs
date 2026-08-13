using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FluentValidation;
using FluentValidation.Results;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public class GetLessonRequestValidator : AbstractValidator<GetLessonsRequest>
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
            .LessThanOrEqualTo(GetLessonsRequest.MAX_PAGE_SIZE)
            .WithError(GeneralErrors.ValueIsInvalid("pageSize"));
    }
}

public sealed class GetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessons", async Task<EndpointResult<PaginationLessonResponse>> (
            [AsParameters] GetLessonsRequest request,
            [FromServices] GetHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class GetHandler
{
    private readonly IEducationReadDbContext _readDbContext;
    private readonly IFileCommunicationService _fileCommunicationService;
    private readonly IValidator<GetLessonsRequest> _validator;

    public GetHandler(
        IEducationReadDbContext readDbContext,
        IFileCommunicationService fileCommunicationService,
        IValidator<GetLessonsRequest> validator)
    {
        _readDbContext = readDbContext;
        _fileCommunicationService = fileCommunicationService;
        _validator = validator;
    }

    public async Task<Result<PaginationLessonResponse, Error>> Handle(
        GetLessonsRequest request,
        CancellationToken cancellationToken)
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

        var lessonRows = await query
            .OrderBy(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(l => new
            {
                l.Id,
                Title = l.Title.Value,
                Description = l.Description.Value,
                l.VideoId,
                l.CreatedAt,
                l.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        Guid[] videoIds = lessonRows
            .Select(l => l.VideoId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        Dictionary<Guid, GetMediaAssetsDto> mediaAssetsById = [];

        if (videoIds.Length > 0)
        {
            Result<GetMediaAssetsResponse, Error> mediaAssetsResult = await _fileCommunicationService
                .GetMediaAssets(new GetMediaAssetsRequest(videoIds), cancellationToken);

            if (mediaAssetsResult.IsFailure)
                return mediaAssetsResult.Error;

            mediaAssetsById = mediaAssetsResult.Value.MediaAssets.ToDictionary(media => media.Id);
        }

        List<LessonDto> lessons = lessonRows
            .Select(lesson => new LessonDto
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                Video = MapVideo(lesson.VideoId, mediaAssetsById),
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt,
            })
            .ToList();

        int totalPages = lessonsCount == 0
            ? 0
            : ((lessonsCount - 1) / request.PageSize) + 1;

        return new PaginationLessonResponse(
            lessons,
            lessonsCount,
            request.Page,
            request.PageSize,
            totalPages);
    }

    private static MediaDto? MapVideo(
        Guid? videoId,
        Dictionary<Guid, GetMediaAssetsDto> mediaAssetsById)
    {
        if (!videoId.HasValue ||
            !mediaAssetsById.TryGetValue(videoId.Value, out GetMediaAssetsDto? mediaAsset))
            return null;

        return new MediaDto
        {
            Id = mediaAsset.Id,
            Url = mediaAsset.Url,
            Status = mediaAsset.Status,
        };
    }
}