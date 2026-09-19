using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Core.Features.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.SharedKernel;

namespace EducationContentService.UnitTests.Features.Lessons;

public class UpdateVideoHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesLessonVideo_WhenMediaAssetIsVideo()
    {
        Guid videoId = Guid.NewGuid();
        Lesson lesson = CreateLesson();
        var repository = new RecordingLessonsRepository(lesson);
        UpdateVideoHandler sut = CreateHandler(
            repository,
            [new GetMediaAssetsDto(videoId, "uploaded", "video", null)]);

        Result<Guid, Error> result = await sut.Handle(
            lesson.Id,
            new UpdateLessonVideoRequest(videoId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(videoId, lesson.VideoId);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenMediaAssetDoesNotExist()
    {
        Lesson lesson = CreateLesson();
        var repository = new RecordingLessonsRepository(lesson);
        UpdateVideoHandler sut = CreateHandler(repository, []);

        Result<Guid, Error> result = await sut.Handle(
            lesson.Id,
            new UpdateLessonVideoRequest(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenMediaAssetIsNotVideo()
    {
        Guid mediaAssetId = Guid.NewGuid();
        Lesson lesson = CreateLesson();
        var repository = new RecordingLessonsRepository(lesson);
        UpdateVideoHandler sut = CreateHandler(
            repository,
            [new GetMediaAssetsDto(mediaAssetId, "ready", "preview", null)]);

        Result<Guid, Error> result = await sut.Handle(
            lesson.Id,
            new UpdateLessonVideoRequest(mediaAssetId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task Handle_ClearsLessonVideo_WithoutRequestingFileService()
    {
        Lesson lesson = CreateLesson();
        var repository = new RecordingLessonsRepository(lesson);
        UpdateVideoHandler sut = CreateHandler(repository, []);

        Result<Guid, Error> result = await sut.Handle(
            lesson.Id,
            new UpdateLessonVideoRequest(null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(lesson.VideoId);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task Validator_RejectsEmptyVideoId()
    {
        var validator = new UpdateLessonVideoRequestValidator();

        var result = await validator.ValidateAsync(new UpdateLessonVideoRequest(Guid.Empty));

        Assert.False(result.IsValid);
    }

    private static Lesson CreateLesson() => new(
        Guid.NewGuid(),
        Title.Create("Lesson").Value,
        Description.Create("Description").Value,
        Guid.NewGuid());

    private static UpdateVideoHandler CreateHandler(
        RecordingLessonsRepository repository,
        IReadOnlyList<GetMediaAssetsDto> mediaAssets)
    {
        return new UpdateVideoHandler(
            NullLogger<UpdateVideoHandler>.Instance,
            repository,
            repository,
            new StubFileCommunicationService(mediaAssets),
            new UpdateLessonVideoRequestValidator());
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

    private sealed class RecordingLessonsRepository : ILessonsRepository, ITransactionManager
    {
        private readonly Lesson _lesson;

        public RecordingLessonsRepository(Lesson lesson)
        {
            _lesson = lesson;
        }

        public bool WasUpdated { get; private set; }

        public Task<Result<Guid, Error>> AddAsync(
            Lesson lesson,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<Guid, Error>> UpdateAsync(
            Lesson lesson,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Result<Lesson, Error>> GetBy(
            Expression<Func<Lesson, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            Result<Lesson, Error> result = _lesson;
            return Task.FromResult(result);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            WasUpdated = true;
            return Task.FromResult(1);
        }
    }
}
