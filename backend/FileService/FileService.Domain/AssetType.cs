using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public enum AssetType
{
    VIDEO,
    PREVIEW,
    AVATAR
}

public static class AssetTypeExtensions
{
    public static Result<AssetType, Error> ToAssetType(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GeneralErrors.ValueIsInvalid(nameof(value));

        return value.Trim().ToLowerInvariant() switch
        {
            "video" => AssetType.VIDEO,
            "preview" => AssetType.PREVIEW,
            "avatar" => AssetType.AVATAR,
            _ => GeneralErrors.ValueIsInvalid(nameof(value))
        };
    }
}