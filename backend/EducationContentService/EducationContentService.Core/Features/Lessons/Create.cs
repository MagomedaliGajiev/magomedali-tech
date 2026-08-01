using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FluentValidation;
using FluentValidation.Results;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(r => r.Title)
            .MustBeValueObject(Title.Create);

        RuleFor(r => r.Description)
            .MustBeValueObject(Description.Create);

        RuleFor(r => r.VideoId)
            .NotEmpty()
            .WithError(GeneralErrors.ValueIsRequired("videoId"));
    }
}

public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons", async Task<EndpointResult<Guid>> (
            [FromBody] CreateLessonRequest request,
            [FromServices] CreateHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class CreateHandler
{
    private const string VIDEO_ASSET_TYPE = "video";

    private readonly ILogger<CreateHandler> _logger;
    private readonly ILessonsRepository _lessonsRepository;
    private readonly IFileCommunicationService _fileCommunicationService;
    private readonly IValidator<CreateLessonRequest> _validator;

    public CreateHandler(
        ILogger<CreateHandler> logger,
        ILessonsRepository lessonsRepository,
        IFileCommunicationService fileCommunicationService,
        IValidator<CreateLessonRequest> validator)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
        _fileCommunicationService = fileCommunicationService;
        _validator = validator;
    }

    public async Task<Result<Guid, Error>> Handle(CreateLessonRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Result<GetMediaAssetsResponse, Error> mediaAssetsResult = await _fileCommunicationService
            .GetMediaAssets(new GetMediaAssetsRequest([request.VideoId]), cancellationToken);

        if (mediaAssetsResult.IsFailure)
            return mediaAssetsResult.Error;

        GetMediaAssetsDto? video = mediaAssetsResult.Value.MediaAssets
            .FirstOrDefault(mediaAsset => mediaAsset.Id == request.VideoId);

        if (video is null)
            return GeneralErrors.NotFound(request.VideoId, "video");

        if (!string.Equals(video.AssetType, VIDEO_ASSET_TYPE, StringComparison.OrdinalIgnoreCase))
            return GeneralErrors.ValueIsInvalid("videoId");

        Title title = Title.Create(request.Title).Value;
        Description description = Description.Create(request.Description).Value;

        var lesson = new Lesson(Guid.NewGuid(), title, description, request.VideoId);

        Result<Guid, Error> result = await _lessonsRepository.AddAsync(lesson, cancellationToken);

        if (result.IsFailure)
            return result.Error;

        _logger.LogInformation("Created lesson {Id}", lesson.Id);

        return lesson.Id;
    }
}