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

    public Guid Version { get; protected set; } = Guid.NewGuid();

    public StorageKey? Key { get; protected set; }

    public StorageKey? RawKey { get; protected set; }

    public bool DirectUpload { get; protected set; }

    public StorageKey UploadKey => (DirectUpload ? Key : RawKey)
        ?? throw new InvalidOperationException("Media asset has no upload key.");

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
        StorageKey uploadKey,
        bool directUpload = true)
    {
        Id = id;
        MediaData = mediaData;
        Owner = owner;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        AssetType = assetType;
        DirectUpload = directUpload;
        Key = directUpload ? uploadKey : null;
        RawKey = directUpload ? null : uploadKey;
    }

    public static Result<MediaAsset, Error> CreateForUpload(
        MediaData mediaData,
        MediaOwner owner,
        AssetType assetType,
        bool directUpload = false)
    {
        var assetId = Guid.NewGuid();

        switch (assetType)
        {
            case AssetType.VIDEO:
                Result<VideoAsset, Error> videoResult = VideoAsset.CreateForUpload(assetId, mediaData, owner, directUpload);
                return videoResult.IsFailure ? videoResult.Error : videoResult.Value;
            case AssetType.PREVIEW:
                Result<PreviewAsset, Error> previewResult = PreviewAsset.CreateForUpload(assetId, mediaData, owner);
                return previewResult.IsFailure ? previewResult.Error : previewResult.Value;
            case AssetType.AVATAR:
            default:
                return GeneralErrors.ValueIsInvalid(nameof(assetType));
        }
    }

    public virtual bool RequiresProcessing() => false;

    public virtual IReadOnlyList<StorageKey> GetStorageKeys() =>
        new[] { RawKey, Key }.OfType<StorageKey>().Distinct().ToList();

    public UnitResult<Error> MarkUploaded()
    {
        if (Status != MediaStatus.UPLOADING)
            return GeneralErrors.Failure($"Нельзя завершить загрузку медиафайла в статусе {Status}");

        Status = MediaStatus.UPLOADED;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();

        return UnitResult.Success<Error>();
    }

    public virtual UnitResult<Error> MarkReady()
    {
        if (Status != MediaStatus.UPLOADED)
            return GeneralErrors.Failure($"Нельзя пометить медиафайл готовым в статусе {Status}");

        if (Key is null)
            return GeneralErrors.Failure("Для готового медиафайла необходим финальный ключ");

        Status = MediaStatus.READY;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkFailed()
    {
        Status = MediaStatus.FAILED;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
        return UnitResult.Success<Error>();
    }

    public virtual UnitResult<Error> MarkDeleted()
    {
        if (Status == MediaStatus.DELETED)
            return UnitResult.Success<Error>();

        Status = MediaStatus.DELETED;
        UpdatedAt = DateTime.UtcNow;
        Version = Guid.NewGuid();
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
