using System.Net;
using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core;
using FileService.Core.Features;
using FileService.Core.FilesStorage;
using FileService.Core.Processing;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;
using FileService.Infrastructure.Postgres;
using FileService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Shared.SharedKernel;

namespace FileService.IntegrationTests.Processing;

[Collection("File service")]
public sealed class VideoProcessingIntegrationTests : FileServiceTestsBase
{
    public VideoProcessingIntegrationTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Processing_RoundTripsEachStepAndPublishesOnlyFinalObject()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(DateTime.UtcNow);
        VideoAsset video = process.VideoAsset;
        StorageKey finalKey = VideoProcessTestData.CreateKey("processed");
        StorageKey previewKey = VideoProcessTestData.CreateKey("previews");
        IAmazonS3 storage = Services.GetRequiredService<IAmazonS3>();

        foreach (StorageKey key in new[] { video.UploadKey, finalKey, previewKey })
        {
            await storage.PutObjectAsync(new PutObjectRequest
            {
                BucketName = key.Location,
                Key = key.Value,
                ContentBody = "test media content",
            });
        }

        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(video);
            await db.SaveChangesAsync();
        });

        var before = await GetMedia(video.Id);
        Assert.Equal("uploaded", before.Status);
        Assert.Null(before.Url);
        var pendingBatch = await GetBatch(video.Id);
        Assert.Null(Assert.Single(pendingBatch.MediaAssets).Url);

        for (int index = 0; index < 4; index++)
        {
            Guid stepId;
            Guid attemptId;
            await using (AsyncServiceScope scope = Services.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<VideoProcessingService>();
                var start = await service.ProcessNextStep(process.Id);
                Assert.True(start.IsSuccess);
                stepId = start.Value.Id;
                attemptId = start.Value.AttemptId!.Value;
                Assert.Equal(index, start.Value.Order);
            }

            await using (AsyncServiceScope scope = Services.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<VideoProcessingService>();
                var complete = await service.CompleteCurrentStep(
                    process.Id,
                    stepId,
                    attemptId,
                    "{\"saved\":true}",
                    index == 3 ? finalKey : null,
                    index == 3 ? previewKey : null);
                Assert.True(complete.IsSuccess);
            }

            HttpResponseMessage statusResponse = await AppHttpClient.GetAsync($"/api/files/{video.Id}/processing");
            var status = await statusResponse.HandleResponseAsync<GetVideoProcessDto>();
            Assert.True(status.IsSuccess);
            Assert.Equal(index + 1, status.Value.Steps.Count(step => step.Status == "completed"));
            Assert.Equal("{\"saved\":true}", status.Value.Steps[index].ResultData);
        }

        var ready = await GetMedia(video.Id);
        Assert.Equal("ready", ready.Status);
        Assert.NotNull(ready.Url);
        Assert.Contains(finalKey.Value, ready.Url, StringComparison.Ordinal);
        var readyBatch = await GetBatch(video.Id);
        Assert.NotNull(Assert.Single(readyBatch.MediaAssets).Url);

        await ExecuteInDb(async db =>
        {
            VideoAsset saved = await db.MediaAssets.OfType<VideoAsset>().Include(item => item.Process)
                .SingleAsync(item => item.Id == video.Id);
            Assert.Equal(finalKey, saved.Key);
            Assert.Equal(previewKey, saved.PreviewKey);
            Assert.Equal(video.RawKey, saved.RawKey);
            Assert.Equal(video.UploadKey, saved.UploadKey);
            Assert.Equal(100, saved.Process!.Progress);
            Assert.Equal(ProcessingStatus.COMPLETED, saved.Process.Status);
        });

        HttpResponseMessage delete = await AppHttpClient.DeleteAsync($"/api/files/{video.Id}");
        Assert.True((await delete.HandleResponseAsync<string>()).IsSuccess);
        foreach (StorageKey key in new[] { video.UploadKey, finalKey, previewKey })
        {
            AmazonS3Exception exception = await Assert.ThrowsAnyAsync<AmazonS3Exception>(() =>
                storage.GetObjectAsync(key.Location, key.Value));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        HttpResponseMessage deletedStatusResponse = await AppHttpClient.GetAsync($"/api/files/{video.Id}/processing");
        deletedStatusResponse.EnsureSuccessStatusCode();
        var deletedStatus = await deletedStatusResponse.Content.ReadFromJsonAsync<Envelope<GetVideoProcessDto>>();
        Assert.NotNull(deletedStatus);
        Assert.Null(deletedStatus.Error);
        Assert.Null(deletedStatus.Result);
    }

    [Fact]
    public async Task Retry_ReloadsFailureScheduleAndAttemptCount()
    {
        DateTime now = DateTime.UtcNow;
        VideoProcess process = VideoProcessTestData.CreateProcess(now);
        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(process.VideoAsset);
            await db.SaveChangesAsync();
        });

        Guid oldAttempt;
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<VideoProcessingService>();
            var start = await service.ProcessNextStep(process.Id);
            oldAttempt = start.Value.AttemptId!.Value;
            Assert.True((await service.FailCurrentStep(
                process.Id, start.Value.Id, oldAttempt, "storage unavailable", false)).IsSuccess);
        }

        await ExecuteInDb(async db =>
        {
            VideoProcess saved = await db.VideoProcesses.Include(item => item.Steps).Include(item => item.VideoAsset)
                .SingleAsync(item => item.Id == process.Id);
            ProcessingStep step = saved.CurrentStep!;
            Assert.Equal(StepStatus.FAILED, step.Status);
            Assert.Equal("storage unavailable", step.ErrorMessage);
            Assert.NotNull(step.StartedAt);
            Assert.NotNull(step.CompletedAt);
            Assert.NotNull(step.NextRetryAt);
            Assert.Equal(0, step.RetryCount);
            Assert.True(saved.ProcessNextStep(step.NextRetryAt.Value.AddTicks(-1)).IsFailure);
            Assert.True(saved.ProcessNextStep(step.NextRetryAt.Value).IsSuccess);
            Assert.NotEqual(oldAttempt, step.AttemptId);
            await db.SaveChangesAsync();
        });

        await ExecuteInDb(async db =>
        {
            VideoProcess saved = await db.VideoProcesses.Include(item => item.Steps).Include(item => item.VideoAsset)
                .SingleAsync(item => item.Id == process.Id);
            Assert.Equal(1, saved.CurrentStep!.RetryCount);
            Assert.Equal(StepStatus.IN_PROGRESS, saved.CurrentStep.Status);
            Assert.Null(saved.CurrentStep.ErrorMessage);
            Assert.Null(saved.CurrentStep.NextRetryAt);
            Assert.Equal(ProcessingStatus.IN_PROGRESS, saved.Status);
        });
    }

    [Fact]
    public async Task ConcurrentWorkers_OnlyOneCanClaimTheStep()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(DateTime.UtcNow);
        await ExecuteInDb(async db =>
        {
            db.MediaAssets.Add(process.VideoAsset);
            await db.SaveChangesAsync();
        });

        await using AsyncServiceScope firstScope = Services.CreateAsyncScope();
        await using AsyncServiceScope secondScope = Services.CreateAsyncScope();
        var firstRepository = firstScope.ServiceProvider.GetRequiredService<IVideoProcessingRepository>();
        var secondRepository = secondScope.ServiceProvider.GetRequiredService<IVideoProcessingRepository>();
        VideoProcess first = (await firstRepository.GetById(process.Id, CancellationToken.None)).Value;
        VideoProcess second = (await secondRepository.GetById(process.Id, CancellationToken.None)).Value;
        first.ProcessNextStep(DateTime.UtcNow);
        second.ProcessNextStep(DateTime.UtcNow);

        Assert.True((await firstRepository.SaveAsync(CancellationToken.None)).IsSuccess);
        var conflict = await secondRepository.SaveAsync(CancellationToken.None);
        Assert.True(conflict.IsFailure);
        Assert.Equal("video.processing.conflict", Assert.Single(conflict.Error.Messages).Code);

        await ExecuteInDb(async db =>
        {
            VideoProcess saved = await db.VideoProcesses.Include(item => item.Steps)
                .SingleAsync(item => item.Id == process.Id);
            Assert.Equal(first.CurrentStep!.AttemptId, saved.CurrentStep!.AttemptId);
            Assert.Single(saved.Steps, step => step.Status == StepStatus.IN_PROGRESS);
        });
    }

    [Theory]
    [InlineData("preview", "preview.png", "image/png", false)]
    [InlineData("video", "video.mp4", "video/mp4", true)]
    public async Task UploadWithoutProcessing_PersistsDirectUploadAndIsImmediatelyReady(
        string assetType,
        string fileName,
        string contentType,
        bool directUpload)
    {
        StartMultipartUploadResponse upload;
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            var handler = new StartMultipartUploadHandler(
                NullLogger<StartMultipartUploadHandler>.Instance,
                scope.ServiceProvider.GetRequiredService<IFileStorageProvider>(),
                scope.ServiceProvider.GetRequiredService<IChunkSizeCalculator>(),
                scope.ServiceProvider.GetRequiredService<IMediaAssetsRepository>(),
                Options.Create(new MediaUploadOptions { DirectUpload = directUpload }));
            var result = await handler.Handle(
                new StartMultipartUploadRequest(fileName, assetType, contentType, 4, "lesson", Guid.NewGuid()),
                CancellationToken.None);
            Assert.True(result.IsSuccess);
            upload = result.Value;
        }

        using var content = new ByteArrayContent([1, 2, 3, 4]);
        using HttpResponseMessage part = await HttpClient.PutAsync(Assert.Single(upload.ChunkUploadUrls).UploadUrl, content);
        part.EnsureSuccessStatusCode();
        Assert.NotNull(part.Headers.ETag);

        var request = new FileService.Contracts.Dtos.CompleteMultipartUploadRequest(
            upload.MediaAssetId,
            upload.UploadId,
            [new PartETagDto(1, part.Headers.ETag.Tag.Trim('"'))]);
        HttpResponseMessage complete = await AppHttpClient.PostAsJsonAsync("/api/files/complete-upload", request);
        Assert.True((await complete.HandleResponseAsync()).IsSuccess);

        await ExecuteInDb(async db =>
        {
            MediaAsset saved = await db.MediaAssets.SingleAsync(item => item.Id == upload.MediaAssetId);
            Assert.True(saved.DirectUpload);
            Assert.False(saved.RequiresProcessing());
            Assert.Equal(MediaStatus.READY, saved.Status);
            Assert.NotNull(saved.Key);
            Assert.Null(saved.RawKey);
            Assert.False(await db.VideoProcesses.AnyAsync(item => item.VideoAssetId == saved.Id));
        });
    }

    [Fact]
    public async Task Migration_PreservesLegacyKeysAndCanRollBackNewRawUploads()
    {
        string connectionString;
        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
            var builder = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString())
            {
                Database = "file_service_migration_" + Guid.NewGuid().ToString("N"),
            };
            connectionString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<FileServiceDbContext>().UseNpgsql(connectionString).Options;
        await using var migrationDb = new FileServiceDbContext(options);
        IMigrator migrator = migrationDb.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20260725221502_InitialMediaAssets");
            Guid legacyId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;
            await migrationDb.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO media_assets (id, file_name, file_extension, content_type, media_type,
                    size, expected_chunks_count, asset_type, created_at, updated_at,
                    storage_key, storage_prefix, storage_location, owner_context, owner_entity_id, status)
                VALUES ({legacyId}, 'legacy', 'mp4', 'video/mp4', 'VIDEO', 1024, 1, 'VIDEO',
                    {now}, {now}, 'legacy-key', 'raw', 'videos', 'lesson', {ownerId}, 'READY');
                """);

            await migrator.MigrateAsync();
            Assert.False(migrationDb.Database.HasPendingModelChanges());
            VideoAsset legacy = await migrationDb.MediaAssets.OfType<VideoAsset>().SingleAsync(item => item.Id == legacyId);
            Assert.True(legacy.DirectUpload);
            Assert.False(legacy.RequiresProcessing());
            Assert.Equal("raw/legacy-key", legacy.UploadKey.Value);
            Assert.Equal(MediaStatus.READY, legacy.Status);

            VideoAsset rawVideo = VideoProcessTestData.CreateVideo(uploaded: false);
            migrationDb.MediaAssets.Add(rawVideo);
            await migrationDb.SaveChangesAsync();
            migrationDb.ChangeTracker.Clear();

            await migrator.MigrateAsync("20260725221502_InitialMediaAssets");
            string key = await migrationDb.Database.SqlQuery<string>($"""
                SELECT storage_key AS "Value" FROM media_assets WHERE id = {rawVideo.Id}
                """).SingleAsync();
            Assert.Equal(rawVideo.UploadKey.Key, key);
        }
        finally
        {
            await migrationDb.Database.EnsureDeletedAsync();
        }
    }

    private async Task<GetMediaAssetDto> GetMedia(Guid id)
    {
        HttpResponseMessage response = await AppHttpClient.GetAsync($"/api/files/{id}");
        var result = await response.HandleResponseAsync<GetMediaAssetDto>();
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private async Task<GetMediaAssetsResponse> GetBatch(Guid id)
    {
        HttpResponseMessage response = await AppHttpClient.PostAsJsonAsync("/api/files/batch", new GetMediaAssetsRequest([id]));
        var result = await response.HandleResponseAsync<GetMediaAssetsResponse>();
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
