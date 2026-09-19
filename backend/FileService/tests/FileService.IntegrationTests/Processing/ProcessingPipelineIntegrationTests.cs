using System.Data;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;
using FileService.Infrastructure.Postgres;
using FileService.IntegrationTests.Infrastructure;
using FileService.VideoProcessing.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.SharedKernel;

namespace FileService.IntegrationTests.Processing;

[Collection("File service")]
public sealed class ProcessingPipelineIntegrationTests : FileServiceTestsBase
{
    public ProcessingPipelineIntegrationTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Pipeline_PersistsNewProcessMetadataAndSkippedStep()
    {
        VideoAsset video = VideoProcessTestData.CreateVideo();
        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(video);
            await db.SaveChangesAsync();
        });

        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            var pipeline = CreatePipeline(scope.ServiceProvider, skipPreview: true);
            Assert.True((await pipeline.ProcessAllStepsAsync(video.Id)).IsSuccess);
            var db = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
            Assert.Null(db.Database.CurrentTransaction);
        }

        await ExecuteInDb(async db =>
        {
            VideoAsset saved = await db.MediaAssets.OfType<VideoAsset>()
                .Include(asset => asset.Process!).ThenInclude(process => process.Steps)
                .SingleAsync(asset => asset.Id == video.Id);
            Assert.Equal(MediaStatus.READY, saved.Status);
            Assert.NotNull(saved.Metadata);
            Assert.Equal(1920, saved.Metadata.Width);
            Assert.Equal(1080, saved.Metadata.Height);
            Assert.Equal(TimeSpan.FromMinutes(2), saved.Metadata.Duration);
            Assert.Equal(ProcessingStatus.COMPLETED, saved.Process!.Status);
            Assert.Equal(100, saved.Process.Progress);
            Assert.Equal(4, saved.Process.Steps.Count);
            Assert.Equal(StepStatus.SKIPPED, saved.Process.Steps.Single(step => step.Type == StepType.GENERATE_PREVIEW).Status);
            Assert.Equal("preview unavailable", saved.Process.Steps.Single(step => step.Type == StepType.GENERATE_PREVIEW).ErrorMessage);
            Assert.NotNull(saved.Key);
            Assert.Null(saved.PreviewKey);
        });
    }

    [Fact]
    public async Task Transaction_RollsBackMetadataAndReleasesEfTransaction()
    {
        VideoAsset video = VideoProcessTestData.CreateVideo();
        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(video);
            await db.SaveChangesAsync();
        });

        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
            VideoAsset tracked = await db.MediaAssets.OfType<VideoAsset>().SingleAsync(asset => asset.Id == video.Id);
            using (IDbTransaction transaction = await manager.BeginTransactionAsync())
            {
                tracked.SetMetadata(VideoMetadata.Create(TimeSpan.FromSeconds(5), 640, 360).Value);
                Assert.True((await manager.SaveChangesAsync()).IsSuccess);
                transaction.Rollback();
            }

            Assert.Null(db.Database.CurrentTransaction);
            using IDbTransaction next = await manager.BeginTransactionAsync();
            next.Commit();
        }

        await ExecuteInDb(async db =>
        {
            VideoAsset saved = await db.MediaAssets.OfType<VideoAsset>().SingleAsync(asset => asset.Id == video.Id);
            Assert.Null(saved.Metadata);
            Assert.Equal(MediaStatus.UPLOADED, saved.Status);
        });
    }

    [Fact]
    public async Task ConcurrentPipeline_DoesNotExecuteHandlerAfterLosingClaim()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(DateTime.UtcNow);
        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(process.VideoAsset);
            await db.SaveChangesAsync();
        });

        await using AsyncServiceScope firstScope = Services.CreateAsyncScope();
        await using AsyncServiceScope secondScope = Services.CreateAsyncScope();
        await firstScope.ServiceProvider.GetRequiredService<IMediaAssetsRepository>().GetBy(asset => asset.Id == process.VideoAssetId);
        await secondScope.ServiceProvider.GetRequiredService<IMediaAssetsRepository>().GetBy(asset => asset.Id == process.VideoAssetId);

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int secondExecutions = 0;
        var first = CreatePipeline(firstScope.ServiceProvider, beforeStep: async step =>
        {
            if (step == StepType.VALIDATE)
            {
                started.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
        });
        var second = CreatePipeline(secondScope.ServiceProvider, beforeStep: _ =>
        {
            secondExecutions++;
            return Task.CompletedTask;
        });

        Task<UnitResult<Error>> firstRun = first.ProcessAllStepsAsync(process.VideoAssetId);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            UnitResult<Error> secondResult = await second.ProcessAllStepsAsync(process.VideoAssetId);
            Assert.True(secondResult.IsFailure);
            Assert.Equal(ErrorType.CONFLICT, secondResult.Error.Type);
            Assert.Equal(0, secondExecutions);
        }
        finally
        {
            release.TrySetResult();
            Assert.True((await firstRun).IsSuccess);
        }
    }

    private static ProcessingPipeline CreatePipeline(
        IServiceProvider services,
        bool skipPreview = false,
        Func<StepType, Task>? beforeStep = null) => new(
        NullLogger<ProcessingPipeline>.Instance,
        services.GetRequiredService<IVideoProcessingRepository>(),
        services.GetRequiredService<IMediaAssetsRepository>(),
        services.GetRequiredService<ITransactionManager>(),
        Enum.GetValues<StepType>().Select(step => new Handler(step, skipPreview, beforeStep)),
        TimeProvider.System);

    private sealed class Handler(StepType stepType, bool skipPreview, Func<StepType, Task>? beforeStep) : IProcessingStepHandler
    {
        public StepType StepType => stepType;

        public async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (beforeStep is not null)
                await beforeStep(StepType);
            if (StepType == StepType.VALIDATE)
                context.Metadata = VideoMetadata.Create(TimeSpan.FromMinutes(2), 1920, 1080).Value;
            if (StepType == StepType.GENERATE_PREVIEW && skipPreview)
                return ProcessingResult.Failure(GeneralErrors.Failure("preview unavailable"), isCritical: false);
            if (StepType == StepType.UPLOAD_RESULT)
                context.FinalKey = VideoProcessTestData.CreateKey("processed");

            return ProcessingResult.Success(context, "{\"success\":true}");
        }
    }
}
