using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;

namespace FileService.IntegrationTests.Processing;

internal static class VideoProcessTestData
{
    public static VideoAsset CreateVideo(bool uploaded = true, bool directUpload = false)
    {
        var data = MediaData.Create(
            FileName.Create("video.mp4").Value,
            ContentType.Create("video/mp4").Value,
            1024,
            1).Value;
        var video = VideoAsset.CreateForUpload(
            Guid.NewGuid(), data, MediaOwner.ForLesson(Guid.NewGuid()).Value, directUpload).Value;
        if (uploaded)
            video.MarkUploaded();
        return video;
    }

    public static VideoProcess CreateProcess(DateTime utcNow) => VideoProcess.Create(CreateVideo(), utcNow).Value;

    public static StorageKey CreateKey(string prefix) => StorageKey.Create("videos", prefix, Guid.NewGuid().ToString()).Value;
}
