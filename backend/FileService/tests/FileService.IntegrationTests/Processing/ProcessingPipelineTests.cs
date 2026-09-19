using System.Data;
using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;
using FileService.VideoProcessing.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.SharedKernel;

namespace FileService.IntegrationTests.Processing;

public sealed class ProcessingPipelineTests
{
    [Fact]
    public async Task Pipeline_CompletesInDomainOrderAndDoesNotRepeatCompletedProcess()
    {
        var harness = new Harness();
        VideoProcess originalProcess = harness.Video.Process!;
        Guid[] originalSteps = originalProcess.Steps.Select(step => step.Id).ToArray();
        VideoMetadata metadata = VideoMetadata.Create(TimeSpan.FromMinutes(2), 1920, 1080).Value;
        harness.Handle = (step, context, _) =>
        {
            Assert.False(harness.Transactions.IsActive);
            if (step == StepType.VALIDATE)
                context.Metadata = metadata;
            return Task.FromResult(harness.Success(step, context));
        };

        UnitResult<Error> result = await harness.Run();

        Assert.True(result.IsSuccess);
        Assert.Equal(Enum.GetValues<StepType>(), harness.Executed);
        Assert.Same(originalProcess, harness.Video.Process);
        Assert.Equal(originalSteps, originalProcess.Steps.Select(step => step.Id));
        Assert.All(originalProcess.Steps, step => Assert.Equal(StepStatus.COMPLETED, step.Status));
        Assert.Equal(100, originalProcess.Progress);
        Assert.Equal(MediaStatus.READY, harness.Video.Status);
        Assert.Equal(metadata, harness.Video.Metadata);
        Assert.NotNull(harness.Video.Key);
        Assert.Equal(8, harness.Transactions.Commits);
        Assert.Equal(0, harness.Transactions.Rollbacks);

        Assert.True((await harness.Run()).IsSuccess);
        Assert.Equal(4, harness.Executed.Count);
        Assert.Equal(8, harness.Transactions.Commits);
    }

    [Fact]
    public async Task Pipeline_CreatesAndPersistsMissingProcess()
    {
        var harness = new Harness(createProcess: false);

        Assert.True((await harness.Run()).IsSuccess);

        Assert.NotNull(harness.Video.Process);
        Assert.Equal(1, harness.Repository.AddCount);
        Assert.Equal(9, harness.Transactions.Commits);
    }

    [Fact]
    public async Task NonCriticalFailure_SkipsStepAndContinuesToUpload()
    {
        var harness = new Harness();
        harness.Handle = (step, context, _) => Task.FromResult(step == StepType.GENERATE_PREVIEW
            ? ProcessingResult.Failure(GeneralErrors.Failure("Preview unavailable"), isCritical: false)
            : harness.Success(step, context));

        Assert.True((await harness.Run()).IsSuccess);

        ProcessingStep skipped = harness.Video.Process!.Steps.Single(step => step.Type == StepType.GENERATE_PREVIEW);
        Assert.Equal(StepStatus.SKIPPED, skipped.Status);
        Assert.Equal("Preview unavailable", skipped.ErrorMessage);
        Assert.False(skipped.CanRetry);
        Assert.Null(skipped.NextRetryAt);
        Assert.Equal(4, harness.Executed.Count);
        Assert.Equal(ProcessingStatus.COMPLETED, harness.Video.Process.Status);
        Assert.Equal(100, harness.Video.Process.Progress);
        Assert.Equal(MediaStatus.READY, harness.Video.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CriticalFailureOrException_StopsAndPersistsFailure(bool throwException)
    {
        var harness = new Harness();
        harness.Handle = (step, context, _) =>
        {
            if (step != StepType.TRANSCODE)
                return Task.FromResult(harness.Success(step, context));
            if (throwException)
                throw new InvalidOperationException("Encoder crashed");
            return Task.FromResult(ProcessingResult.Failure(GeneralErrors.Failure("Invalid video")));
        };

        Assert.True((await harness.Run()).IsFailure);

        Assert.Equal(new[] { StepType.VALIDATE, StepType.TRANSCODE }, harness.Executed);
        Assert.Equal(ProcessingStatus.FAILED, harness.Video.Process!.Status);
        Assert.Equal(MediaStatus.FAILED, harness.Video.Status);
        Assert.Equal(10, harness.Video.Process.Progress);
        Assert.True(harness.Video.Process.Steps[1].IsCriticalFailure);
        Assert.Equal(4, harness.Transactions.Commits);
    }

    [Fact]
    public async Task NonCriticalUploadFailure_CannotPublishVideoWithoutFinalFile()
    {
        var harness = new Harness();
        harness.Handle = (step, context, _) => Task.FromResult(step == StepType.UPLOAD_RESULT
            ? ProcessingResult.Failure(GeneralErrors.Failure("Upload failed"), isCritical: false)
            : harness.Success(step, context));

        Assert.True((await harness.Run()).IsFailure);

        Assert.Equal(ProcessingStatus.FAILED, harness.Video.Process!.Status);
        Assert.Equal(MediaStatus.FAILED, harness.Video.Status);
        Assert.Null(harness.Video.Key);
    }

    [Fact]
    public async Task SuccessfulHandlerWithoutFinalKey_DoesNotMarkVideoReady()
    {
        var harness = new Harness
        {
            Handle = (_, context, _) => Task.FromResult(ProcessingResult.Success(context)),
        };

        Assert.True((await harness.Run()).IsFailure);

        Assert.Equal(ProcessingStatus.FAILED, harness.Video.Process!.Status);
        Assert.Null(harness.Video.Key);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task MissingOrDuplicateHandler_DoesNotStartStep(int handlerCount)
    {
        var harness = new Harness();
        IProcessingStepHandler[] handlers = Enumerable.Range(0, handlerCount)
            .Select(_ => new Handler(StepType.VALIDATE, harness)).ToArray();

        Assert.True((await harness.CreatePipeline(handlers).ProcessAllStepsAsync(harness.Video.Id)).IsFailure);

        Assert.Empty(harness.Executed);
        Assert.Equal(0, harness.Transactions.SaveCount);
        Assert.Equal(StepStatus.PENDING, harness.Video.Process!.CurrentStep!.Status);
    }

    [Fact]
    public async Task Cancellation_ResetsActiveStepAndAllowsResume()
    {
        var harness = new Harness();
        using var cancellation = new CancellationTokenSource();
        harness.Handle = async (step, context, token) =>
        {
            if (step == StepType.TRANSCODE)
            {
                await cancellation.CancelAsync();
                token.ThrowIfCancellationRequested();
            }

            return harness.Success(step, context);
        };

        UnitResult<Error> result = await harness.Run(cancellation.Token);

        Assert.Equal("operation.canceled", Assert.Single(result.Error.Messages).Code);
        Assert.Equal(ProcessingStatus.IN_PROGRESS, harness.Video.Process!.Status);
        Assert.Equal(StepStatus.COMPLETED, harness.Video.Process.Steps[0].Status);
        Assert.Equal(StepStatus.PENDING, harness.Video.Process.Steps[1].Status);
        Assert.Null(harness.Video.Process.Steps[1].AttemptId);
        harness.Handle = (step, context, _) => Task.FromResult(harness.Success(step, context));

        Assert.True((await harness.Run()).IsSuccess);
        Assert.Equal(1, harness.Executed.Count(step => step == StepType.VALIDATE));
        Assert.Equal(2, harness.Executed.Count(step => step == StepType.TRANSCODE));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    public async Task SaveFailure_RollsBackAndDoesNotStartNextHandler(int failOnSave, int expectedExecutions)
    {
        var harness = new Harness();
        harness.Transactions.FailOnSave = failOnSave;

        UnitResult<Error> result = await harness.Run();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.CONFLICT, result.Error.Type);
        Assert.Equal(expectedExecutions, harness.Executed.Count);
        Assert.Equal(1, harness.Transactions.Rollbacks);
        Assert.False(harness.Transactions.IsActive);
    }

    [Fact]
    public async Task MissingOrDeletedAsset_DoesNotCreateProcess()
    {
        var harness = new Harness(createProcess: false);

        Assert.True((await harness.CreatePipeline().ProcessAllStepsAsync(Guid.NewGuid())).IsFailure);
        harness.Video.MarkDeleted();
        Assert.True((await harness.Run()).IsFailure);

        Assert.Equal(0, harness.Repository.AddCount);
        Assert.Equal(0, harness.Transactions.SaveCount);
    }

    private sealed class Harness
    {
        public Harness(bool createProcess = true)
        {
            Video = VideoProcessTestData.CreateVideo();
            if (createProcess)
                VideoProcess.Create(Video, DateTime.UtcNow);
            Repository = new ProcessRepository(Video);
            Handle = (step, context, _) => Task.FromResult(Success(step, context));
        }

        public VideoAsset Video { get; }

        public ProcessRepository Repository { get; }

        public Transactions Transactions { get; } = new();

        public List<StepType> Executed { get; } = [];

        public Func<StepType, ProcessingContext, CancellationToken, Task<ProcessingResult>> Handle { get; set; }

        public ProcessingResult Success(StepType step, ProcessingContext context)
        {
            if (step == StepType.UPLOAD_RESULT)
                context.FinalKey = VideoProcessTestData.CreateKey("processed");
            return ProcessingResult.Success(context, step.ToString());
        }

        public ProcessingPipeline CreatePipeline(IEnumerable<IProcessingStepHandler>? handlers = null) => new(
            NullLogger<ProcessingPipeline>.Instance, Repository, new AssetsRepository(Video), Transactions,
            handlers ?? Enum.GetValues<StepType>().Reverse().Select(step => new Handler(step, this)), TimeProvider.System);

        public Task<UnitResult<Error>> Run(CancellationToken cancellationToken = default) =>
            CreatePipeline().ProcessAllStepsAsync(Video.Id, cancellationToken);
    }

    private sealed class Handler(StepType stepType, Harness harness) : IProcessingStepHandler
    {
        public StepType StepType => stepType;

        public Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            harness.Executed.Add(StepType);
            return harness.Handle(StepType, context, cancellationToken);
        }
    }

    private sealed class AssetsRepository(VideoAsset video) : IMediaAssetsRepository
    {
        public Task<Result<MediaAsset, Error>> GetBy(Expression<Func<MediaAsset, bool>> predicate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Result<MediaAsset, Error>>(predicate.Compile()(video) ? video : GeneralErrors.NotFound());
        }

        public Task<Result<Guid, Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> SaveAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ProcessRepository(VideoAsset video) : IVideoProcessingRepository
    {
        public int AddCount { get; private set; }

        public void Add(VideoProcess process)
        {
            Assert.Same(video.Process, process);
            AddCount++;
        }

        public Task<Result<VideoProcess, Error>> GetBy(Expression<Func<VideoProcess, bool>> predicate, CancellationToken cancellationToken = default) =>
            Task.FromResult<Result<VideoProcess, Error>>(video.Process is { } process && predicate.Compile()(process)
                ? process : GeneralErrors.NotFound());

        public Task<Result<VideoProcess, Error>> GetById(Guid id, CancellationToken cancellationToken) =>
            GetBy(process => process.Id == id, cancellationToken);

        public Task<UnitResult<Error>> SaveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Transactions : ITransactionManager
    {
        public int SaveCount { get; private set; }

        public int Commits { get; private set; }

        public int Rollbacks { get; private set; }

        public int? FailOnSave { get; set; }

        public bool IsActive { get; private set; }

        public Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.False(IsActive);
            IsActive = true;
            return Task.FromResult<IDbTransaction>(new Transaction(this));
        }

        public Task<Result<int, Error>> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.True(IsActive);
            SaveCount++;
            return Task.FromResult<Result<int, Error>>(SaveCount == FailOnSave ? GeneralErrors.ConcurrencyConflict() : 1);
        }

        private sealed class Transaction(Transactions owner) : IDbTransaction
        {
            public IDbConnection? Connection => null;

            public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

            public void Commit() => owner.Commits++;

            public void Rollback() => owner.Rollbacks++;

            public void Dispose() => owner.IsActive = false;
        }
    }
}
