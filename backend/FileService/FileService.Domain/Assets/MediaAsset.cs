using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain.Assets;

public abstract class MediaAsset
{
    public Guid Id { get; protected set; }

    public MediaData MediaData { get; protected set; } = null!;

    public AssetType AssetType { get; protected set; }

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public StorageKey Key { get; protected set; } = null!;

    public MediaOwner Owner { get; protected set; } = null!;

    public MediaStatus Status { get; protected set; }

    protected MediaAsset()
    {
    }

    protected MediaAsset(
        Guid id,
        MediaData mediaData,
        MediaOwner owner,
        MediaStatus status,
        AssetType assetType,
        StorageKey key)
    {
        Id = id;
        MediaData = mediaData;
        Owner = owner;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        AssetType = assetType;
        Key = key;
    }

    public static Result<MediaAsset, Error> CreateForUpload(
        MediaData mediaData,
        MediaOwner owner,
        AssetType assetType)
    {
        var assetId = Guid.NewGuid();

        switch (assetType)
        {
            case AssetType.VIDEO:
                Result<VideoAsset, Error> videoResult = VideoAsset.CreateForUpload(assetId, mediaData, owner);
                return videoResult.IsFailure ? videoResult.Error : videoResult.Value;
            case AssetType.PREVIEW:
                Result<PreviewAsset, Error> previewResult = PreviewAsset.CreateForUpload(assetId, mediaData, owner);
                return previewResult.IsFailure ? previewResult.Error : previewResult.Value;
            case AssetType.AVATAR:
            default:
                return GeneralErrors.ValueIsInvalid(nameof(assetType));
        }
    }

    public UnitResult<Error> MarkUploaded()
    {
        if (Status != MediaStatus.UPLOADING)
            return GeneralErrors.Failure($"Нельзя завершить загрузку медиафайла в статусе {Status}");

        Status = MediaStatus.UPLOADED;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkReady()
    {
        if (Status != MediaStatus.UPLOADED)
            return GeneralErrors.Failure($"Нельзя пометить медиафайл готовым в статусе {Status}");

        Status = MediaStatus.READY;
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkFailed()
    {
        Status = MediaStatus.FAILED;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }
}

public enum MediaStatus
{
    UPLOADING,
    UPLOADED,
    READY,
    FAILED,
    DELETED,
}
