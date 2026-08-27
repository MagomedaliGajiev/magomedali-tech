using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Features.Lessons;
using EducationContentService.Domain.Lessons;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.SharedKernel;

namespace EducationContentService.UnitTests.Features.Lessons;

public class CreateHandlerTests
{
    [Fact]
    public async Task Validator_RejectsEmptyLessonId()
    {
        var validator = new CreateLessonRequestValidator();
        var request = new CreateLessonRequest(Guid.Empty, "Lesson", "Description", Guid.NewGuid());

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateLessonRequest.Id));
    }

    [Fact]
    public async Task Validator_RejectsEmptyVideoId()
    {
        var validator = new CreateLessonRequestValidator();
        var request = new CreateLessonRequest(Guid.NewGuid(), "Lesson", "Description", Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateLessonRequest.VideoId));
    }

    [Fact]
    public async Task Validator_AllowsMissingVideoId()
    {
        var validator = new CreateLessonRequestValidator();
        var request = new CreateLessonRequest(Guid.NewGuid(), "Lesson", "Description", null);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenVideoDoesNotExist()
    {
        Guid videoId = Guid.NewGuid();
        var repository = new RecordingLessonsRepository();
        CreateHandler sut = CreateHandler(repository, []);

        Result<Guid, Error> result = await sut.Handle(
            new CreateLessonRequest(Guid.NewGuid(), "Lesson", "Description", videoId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
        Assert.Null(repository.AddedLesson);
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenMediaAssetIsNotVideo()
    {
        Guid mediaAssetId = Guid.NewGuid();
        Guid lessonId = Guid.NewGuid();
        var mediaAsset = new GetMediaAssetsDto(mediaAssetId, "ready", "preview", null);
        var repository = new RecordingLessonsRepository();
        CreateHandler sut = CreateHandler(repository, [mediaAsset]);

        Result<Guid, Error> result = await sut.Handle(
            new CreateLessonRequest(lessonId, "Lesson", "Description", mediaAssetId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Null(repository.AddedLesson);
    }

    [Fact]
    public async Task Handle_CreatesLesson_WhenVideoExists()
    {
        Guid videoId = Guid.NewGuid();
        Guid lessonId = Guid.NewGuid();
        var video = new GetMediaAssetsDto(videoId, "ready", "video", null);
        var repository = new RecordingLessonsRepository();
        CreateHandler sut = CreateHandler(repository, [video]);

        Result<Guid, Error> result = await sut.Handle(
            new CreateLessonRequest(lessonId, "Lesson", "Description", videoId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedLesson);
        Assert.Equal(lessonId, repository.AddedLesson.Id);
        Assert.Equal(videoId, repository.AddedLesson.VideoId);
    }

    [Fact]
    public async Task Handle_CreatesLesson_WithoutVideo()
    {
        Guid lessonId = Guid.NewGuid();
        var repository = new RecordingLessonsRepository();
        CreateHandler sut = CreateHandler(repository, []);

        Result<Guid, Error> result = await sut.Handle(
            new CreateLessonRequest(lessonId, "Lesson", "Description", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedLesson);
        Assert.Equal(lessonId, repository.AddedLesson.Id);
        Assert.Null(repository.AddedLesson.VideoId);
    }

    private static CreateHandler CreateHandler(
        RecordingLessonsRepository repository,
        IReadOnlyList<GetMediaAssetsDto> mediaAssets)
    {
        return new CreateHandler(
            NullLogger<CreateHandler>.Instance,
            repository,
            new StubFileCommunicationService(mediaAssets),
            new CreateLessonRequestValidator());
    }

    private sealed class StubFileCommunicationService : IFileCommunicationService
    {
        private readonly IReadOnlyList<GetMediaAssetsDto> _mediaAssets;

        public StubFileCommunicationService(IReadOnlyList<GetMediaAssetsDto> mediaAssets)
        {
            _mediaAssets = mediaAssets;
        }

        public Task<Result<GetMediaAssetsResponse, Error>> GetMediaAssets(
            GetMediaAssetsRequest request,
            CancellationToken cancellationToken)
        {
            Result<GetMediaAssetsResponse, Error> result = new GetMediaAssetsResponse(_mediaAssets);
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingLessonsRepository : ILessonsRepository
    {
        public Lesson? AddedLesson { get; private set; }

        public Task<Result<Guid, Error>> AddAsync(
            Lesson lesson,
            CancellationToken cancellationToken = default)
        {
            AddedLesson = lesson;
            Result<Guid, Error> result = lesson.Id;
            return Task.FromResult(result);
        }

        public Task<Result<Guid, Error>> UpdateAsync(
            Lesson lesson,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<Lesson, Error>> GetBy(
            Expression<Func<Lesson, bool>> predicate,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
