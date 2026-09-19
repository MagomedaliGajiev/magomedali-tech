using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;

namespace FileService.IntegrationTests.Processing;

public sealed class VideoProcessTests
{
    private static readonly DateTime UtcNow = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateVideo_SeparatesUploadAndFinalKeys()
    {
        VideoAsset video = VideoProcessTestData.CreateVideo(uploaded: false);

        Assert.True(video.RequiresProcessing());
        Assert.False(video.DirectUpload);
        Assert.Null(video.Key);
        Assert.NotNull(video.RawKey);
        Assert.Equal(video.RawKey, video.UploadKey);
        Assert.Equal("raw/" + video.Id, video.UploadKey.Value);
        Assert.True(video.MarkReady().IsFailure);
        Assert.True(video.MarkUploaded().IsSuccess);
        Assert.True(video.MarkReady().IsFailure);
    }

    [Fact]
    public void DirectUploadVideo_BecomesReadyWithoutProcessing()
    {
        VideoAsset video = VideoProcessTestData.CreateVideo(directUpload: true);

        Assert.False(video.RequiresProcessing());
        Assert.True(video.DirectUpload);
        Assert.Null(video.RawKey);
        Assert.NotNull(video.Key);
        Assert.Equal(video.Key, video.UploadKey);
        Assert.True(video.MarkReady().IsSuccess);
        Assert.True(VideoProcess.Create(video, UtcNow).IsFailure);
    }

    [Fact]
    public void Preview_DoesNotRequireProcessing()
    {
        var data = MediaData.Create(
            FileName.Create("preview.png").Value,
            ContentType.Create("image/png").Value,
            1024,
            1).Value;
        PreviewAsset preview = PreviewAsset.CreateForUpload(
            Guid.NewGuid(), data, MediaOwner.ForLesson(Guid.NewGuid()).Value).Value;

        Assert.False(preview.RequiresProcessing());
        Assert.Equal(preview.Key, preview.UploadKey);
        Assert.True(preview.MarkUploaded().IsSuccess);
        Assert.True(preview.MarkReady().IsSuccess);
    }

    [Fact]
    public void Create_RequiresUploadedVideoAndRejectsDuplicateProcess()
    {
        VideoAsset video = VideoProcessTestData.CreateVideo(uploaded: false);
        Assert.True(VideoProcess.Create(video, UtcNow).IsFailure);

        video.MarkUploaded();
        VideoProcess process = VideoProcess.Create(video, UtcNow).Value;
        Guid[] stepIds = process.Steps.Select(step => step.Id).ToArray();

        Assert.Equal(video.Id, process.VideoAssetId);
        Assert.Same(process, video.Process);
        Assert.Equal(ProcessingStatus.IN_PROGRESS, process.Status);
        Assert.Equal(0, process.Progress);
        Assert.Equal(4, stepIds.Distinct().Count());
        Assert.All(process.Steps, step => Assert.Equal(StepStatus.PENDING, step.Status));
        Assert.True(VideoProcess.Create(video, UtcNow).IsFailure);
        Assert.True(process.InitializeSteps().IsFailure);
        Assert.Equal(stepIds, process.Steps.Select(step => step.Id));
    }

    [Theory]
    [InlineData((StepType)99, 0, 1)]
    [InlineData(StepType.VALIDATE, -1, 1)]
    [InlineData(StepType.VALIDATE, 0, 0)]
    [InlineData(StepType.VALIDATE, 0, -1)]
    public void CreateStep_RejectsInvalidDefinition(StepType type, int order, int weight)
    {
        Assert.True(ProcessingStep.Create(type, order, weight).IsFailure);
    }

    [Fact]
    public void CompleteSteps_UsesWeightsAndPublishesResultOnlyAfterLastStep()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        StorageKey finalKey = VideoProcessTestData.CreateKey("processed");
        StorageKey previewKey = VideoProcessTestData.CreateKey("previews");
        decimal[] progress = [10, 75, 90, 100];

        for (int index = 0; index < 4; index++)
        {
            ProcessingStep step = process.ProcessNextStep(UtcNow.AddMinutes(index)).Value;
            Assert.Equal(index, step.Order);
            Assert.Equal(StepStatus.IN_PROGRESS, step.Status);
            Assert.NotNull(step.AttemptId);
            Assert.True(process.ProcessNextStep(UtcNow.AddMinutes(index)).IsFailure);

            var result = process.CompleteCurrentStep(
                step.Id,
                step.AttemptId.Value,
                "{\"success\":true}",
                UtcNow.AddMinutes(index).AddSeconds(1),
                index == 3 ? finalKey : null,
                index == 3 ? previewKey : null);

            Assert.True(result.IsSuccess);
            Assert.Equal(progress[index], process.Progress);
            Assert.Equal("{\"success\":true}", step.ResultData);
            Assert.NotNull(step.CompletedAt);
            if (index != 3)
            {
                Assert.Null(process.VideoAsset.Key);
                Assert.Equal(MediaStatus.UPLOADED, process.VideoAsset.Status);
            }
        }

        Assert.Equal(ProcessingStatus.COMPLETED, process.Status);
        Assert.Equal(MediaStatus.READY, process.VideoAsset.Status);
        Assert.Equal(finalKey, process.VideoAsset.Key);
        Assert.Equal(previewKey, process.VideoAsset.PreviewKey);
        Assert.NotNull(process.VideoAsset.RawKey);
        Assert.Equal(3, process.VideoAsset.GetStorageKeys().Count);
        Assert.Null(process.CurrentStep);
        Assert.True(process.ProcessNextStep(UtcNow.AddHours(1)).IsFailure);
    }

    [Fact]
    public void Complete_RejectsPendingWrongAndRepeatedCallbacks()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        ProcessingStep first = process.Steps[0];
        Assert.True(process.CompleteCurrentStep(first.Id, Guid.NewGuid(), null, UtcNow).IsFailure);

        process.ProcessNextStep(UtcNow);
        Assert.True(process.CompleteCurrentStep(process.Steps[1].Id, first.AttemptId!.Value, null, UtcNow).IsFailure);
        Assert.True(process.CompleteCurrentStep(first.Id, Guid.NewGuid(), null, UtcNow).IsFailure);
        Assert.True(process.CompleteCurrentStep(first.Id, first.AttemptId.Value, null, UtcNow).IsSuccess);
        Assert.True(process.CompleteCurrentStep(first.Id, first.AttemptId.Value, null, UtcNow).IsFailure);
        Assert.Equal(10, process.Progress);
    }

    [Fact]
    public void CompleteLastStep_RequiresDistinctFinalKeyWithoutMutatingOnFailure()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        for (int index = 0; index < 3; index++)
        {
            ProcessingStep step = process.ProcessNextStep(UtcNow).Value;
            process.CompleteCurrentStep(step.Id, step.AttemptId!.Value, null, UtcNow);
        }

        ProcessingStep last = process.ProcessNextStep(UtcNow).Value;
        Assert.True(process.CompleteCurrentStep(last.Id, last.AttemptId!.Value, null, UtcNow).IsFailure);
        Assert.True(process.CompleteCurrentStep(
            last.Id, last.AttemptId.Value, null, UtcNow, process.VideoAsset.RawKey).IsFailure);
        Assert.Equal(StepStatus.IN_PROGRESS, last.Status);
        Assert.Equal(90, process.Progress);
        Assert.Equal(ProcessingStatus.IN_PROGRESS, process.Status);
        Assert.Null(process.VideoAsset.Key);
    }

    [Fact]
    public void Retry_WaitsForDueTimeAndRejectsPreviousAttemptResult()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        ProcessingStep step = process.ProcessNextStep(UtcNow).Value;
        Guid originalAttempt = step.AttemptId!.Value;
        Assert.True(process.FailCurrentStep(step.Id, originalAttempt, "temporary error", false, UtcNow).IsSuccess);
        Assert.Equal("temporary error", step.ErrorMessage);
        Assert.Equal(UtcNow.AddSeconds(30), step.NextRetryAt);

        Assert.True(process.ProcessNextStep(UtcNow.AddSeconds(29)).IsFailure);
        Assert.Equal(0, step.RetryCount);
        Assert.Equal(StepStatus.FAILED, step.Status);

        Assert.True(process.ProcessNextStep(UtcNow.AddSeconds(30)).IsSuccess);
        Assert.Equal(1, step.RetryCount);
        Assert.NotEqual(originalAttempt, step.AttemptId);
        Assert.Null(step.ErrorMessage);
        Assert.Null(step.CompletedAt);
        Assert.Null(step.NextRetryAt);
        Assert.True(process.CompleteCurrentStep(step.Id, originalAttempt, "stale", UtcNow.AddSeconds(31)).IsFailure);
        Assert.True(process.FailCurrentStep(step.Id, originalAttempt, "stale", true, UtcNow.AddSeconds(31)).IsFailure);
        Assert.Equal(ProcessingStatus.IN_PROGRESS, process.Status);
    }

    [Fact]
    public void Retry_AllowsThreeRetriesAndFailsAfterFourthFailedAttempt()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        DateTime now = UtcNow;

        for (int retryCount = 0; retryCount <= ProcessingStep.MAX_RETRIES; retryCount++)
        {
            ProcessingStep step = process.ProcessNextStep(now).Value;
            Assert.Equal(retryCount, step.RetryCount);
            Assert.True(process.FailCurrentStep(step.Id, step.AttemptId!.Value, "temporary error", false, now).IsSuccess);
            if (retryCount < ProcessingStep.MAX_RETRIES)
            {
                Assert.Equal(ProcessingStatus.IN_PROGRESS, process.Status);
                Assert.Equal(MediaStatus.UPLOADED, process.VideoAsset.Status);
                Assert.NotNull(step.NextRetryAt);
                now = step.NextRetryAt.Value;
            }
        }

        Assert.Equal(ProcessingStatus.FAILED, process.Status);
        Assert.Equal(MediaStatus.FAILED, process.VideoAsset.Status);
        Assert.Equal("temporary error", process.ErrorMessage);
        Assert.Equal(3, process.CurrentStep!.RetryCount);
        Assert.Null(process.CurrentStep.NextRetryAt);
        Assert.True(process.CurrentStep.Reset(now.AddHours(1)).IsFailure);
        Assert.True(process.ProcessNextStep(now.AddHours(1)).IsFailure);
    }

    [Fact]
    public void CriticalFailure_StopsImmediatelyAndPreservesCompletedProgress()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        ProcessingStep first = process.ProcessNextStep(UtcNow).Value;
        process.CompleteCurrentStep(first.Id, first.AttemptId!.Value, "validated", UtcNow);
        ProcessingStep second = process.ProcessNextStep(UtcNow).Value;

        Assert.True(process.FailCurrentStep(second.Id, second.AttemptId!.Value, "invalid video", true, UtcNow).IsSuccess);
        Assert.Equal(10, process.Progress);
        Assert.Equal(ProcessingStatus.FAILED, process.Status);
        Assert.True(second.IsCriticalFailure);
        Assert.False(second.CanRetry);
        Assert.Null(second.NextRetryAt);
        Assert.Equal(0, second.RetryCount);
        Assert.True(process.ProcessNextStep(UtcNow.AddDays(1)).IsFailure);
    }

    [Fact]
    public void DeletedVideo_CannotContinueProcessing()
    {
        VideoProcess process = VideoProcessTestData.CreateProcess(UtcNow);
        ProcessingStep step = process.ProcessNextStep(UtcNow).Value;

        process.VideoAsset.MarkDeleted();

        Assert.Equal(ProcessingStatus.FAILED, process.Status);
        Assert.Equal(MediaStatus.DELETED, process.VideoAsset.Status);
        Assert.True(process.CompleteCurrentStep(step.Id, step.AttemptId!.Value, null, UtcNow).IsFailure);
        Assert.True(process.ProcessNextStep(UtcNow).IsFailure);
    }

    [Fact]
    public void Step_RejectsInvalidTransitionsAndKeepsFailureDetails()
    {
        ProcessingStep step = ProcessingStep.Create(StepType.TRANSCODE, 0, 65).Value;
        Assert.True(step.Complete(null, UtcNow).IsFailure);
        Assert.True(step.Fail("error", false, UtcNow).IsFailure);
        Assert.True(step.Reset(UtcNow).IsFailure);
        Assert.True(step.Start(UtcNow).IsSuccess);
        Assert.True(step.Start(UtcNow).IsFailure);
        Assert.True(step.Fail(" ", false, UtcNow).IsFailure);
        Assert.True(step.Complete(null, UtcNow.AddSeconds(-1)).IsFailure);
        Assert.True(step.Fail("timeout", false, UtcNow.AddSeconds(1)).IsSuccess);
        Assert.Equal(UtcNow, step.StartedAt);
        Assert.Equal(UtcNow.AddSeconds(1), step.CompletedAt);
        Assert.Equal("timeout", step.ErrorMessage);
        Assert.Null(step.ResultData);
    }
}
