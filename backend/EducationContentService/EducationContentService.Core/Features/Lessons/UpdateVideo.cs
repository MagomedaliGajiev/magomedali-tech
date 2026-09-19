using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FluentValidation;
using FluentValidation.Results;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public class UpdateLessonVideoRequestValidator : AbstractValidator<UpdateLessonVideoRequest>
{
    public UpdateLessonVideoRequestValidator()
    {
        RuleFor(r => r.VideoId)
            .Must(videoId => !videoId.HasValue || videoId.Value != Guid.Empty)
            .WithError(GeneralErrors.ValueIsInvalid("videoId"));
    }
}

public sealed class UpdateVideoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/lessons/{lessonId:guid}/video", async Task<EndpointResult<Guid>> (
            [FromRoute] Guid lessonId,
            [FromBody] UpdateLessonVideoRequest request,
            [FromServices] UpdateVideoHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(lessonId, request, cancellationToken));
    }
}

public sealed class UpdateVideoHandler
{
    private const string VIDEO_ASSET_TYPE = "video";

    private readonly ILogger<UpdateVideoHandler> _logger;
    private readonly ITransactionManager _transactionManager;
    private readonly ILessonsRepository _lessonsRepository;
    private readonly IFileCommunicationService _fileCommunicationService;
    private readonly IValidator<UpdateLessonVideoRequest> _validator;

    public UpdateVideoHandler(
        ILogger<UpdateVideoHandler> logger,
        ITransactionManager transactionManager,
        ILessonsRepository lessonsRepository,
        IFileCommunicationService fileCommunicationService,
        IValidator<UpdateLessonVideoRequest> validator)
    {
        _logger = logger;
        _transactionManager = transactionManager;
        _lessonsRepository = lessonsRepository;
        _fileCommunicationService = fileCommunicationService;
        _validator = validator;
    }

    public async Task<Result<Guid, Error>> Handle(
        Guid lessonId,
        UpdateLessonVideoRequest request,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToError();

        Result<Lesson, Error> lessonResult = await _lessonsRepository
            .GetBy(lesson => lesson.Id == lessonId, cancellationToken);
        if (lessonResult.IsFailure)
            return lessonResult.Error;

        if (request.VideoId.HasValue)
        {
            Result<GetMediaAssetsResponse, Error> mediaAssetsResult = await _fileCommunicationService
                .GetMediaAssets(new GetMediaAssetsRequest([request.VideoId.Value]), cancellationToken);
            if (mediaAssetsResult.IsFailure)
                return mediaAssetsResult.Error;

            GetMediaAssetsDto? video = mediaAssetsResult.Value.MediaAssets
                .FirstOrDefault(mediaAsset => mediaAsset.Id == request.VideoId.Value);

            if (video is null)
                return GeneralErrors.NotFound(request.VideoId.Value, "video");

            if (!string.Equals(video.AssetType, VIDEO_ASSET_TYPE, StringComparison.OrdinalIgnoreCase))
                return GeneralErrors.ValueIsInvalid("videoId");
        }

        Lesson lesson = lessonResult.Value;
        lesson.UpdateVideoId(request.VideoId);

        try
        {
            await _transactionManager.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was cancelled while updating video for lesson {LessonId}", lessonId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while updating video for lesson {LessonId}", lessonId);
            return EducationErrors.DatabaseError();
        }

        _logger.LogInformation("Updated video for lesson {LessonId} to media asset {MediaAssetId}", lessonId, request.VideoId);

        return lesson.Id;
    }
}
