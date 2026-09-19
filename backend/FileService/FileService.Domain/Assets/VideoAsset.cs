using CSharpFunctionalExtensions;
using FileService.Domain.Processing;
using Shared.SharedKernel;

namespace FileService.Domain.Assets;

public class VideoAsset : MediaAsset
{
    public const long MAX_SIZE = 5_368_709_120;

    public const string LOCATION = "videos";
    public const string RAW_PREFIX = "raw";
    public const string ALLOWED_CONTENT_TYPE = "video";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public StorageKey? PreviewKey { get; private set; }

    public VideoProcess? Process { get; private set; }

    private VideoAsset()
    {
    }

    private VideoAsset(
        Guid id,
        MediaData mediaData,
        MediaOwner owner,
        MediaStatus status,
        StorageKey key,
        bool directUpload)
        : base(id, mediaData, owner, status, AssetType.VIDEO, key, directUpload)
    {
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return Error.Validation("video.invalid.extension", $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (mediaData.ContentType.Category != MediaType.VIDEO)
        {
            return Error.Validation("video.invalid.content-type", $"File content type must be {ALLOWED_CONTENT_TYPE}");
        }

        if (mediaData.Size > MAX_SIZE)
        {
            return Error.Validation("video.invalid.size", $"File size must be less than {MAX_SIZE} bytes");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<VideoAsset, Error> CreateForUpload(
        Guid id,
        MediaData mediaData,
        MediaOwner owner,
        bool directUpload = false)
    {
        UnitResult<Error> validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> key = StorageKey.Create(LOCATION, RAW_PREFIX, id.ToString());
        if (key.IsFailure)
            return key.Error;

        return new VideoAsset(
            id,
            mediaData,
            owner,
            MediaStatus.UPLOADING,
            key.Value,
            directUpload);
    }

    public override bool RequiresProcessing() => !DirectUpload;

    public override IReadOnlyList<StorageKey> GetStorageKeys() =>
        base.GetStorageKeys().Concat(new[] { PreviewKey }.OfType<StorageKey>()).Distinct().ToList();

    public override UnitResult<Error> MarkReady()
    {
        if (RequiresProcessing() && Process?.Status != ProcessingStatus.COMPLETED)
            return GeneralErrors.Failure("Обработка видео ещё не завершена");

        return base.MarkReady();
    }

    public override UnitResult<Error> MarkDeleted()
    {
        Process?.Cancel(DateTime.UtcNow);
        return base.MarkDeleted();
    }

    internal void AttachProcess(VideoProcess process) => Process = process;

    internal void SetProcessedKeys(StorageKey key, StorageKey? previewKey)
    {
        Key = key;
        PreviewKey = previewKey;
    }
}
