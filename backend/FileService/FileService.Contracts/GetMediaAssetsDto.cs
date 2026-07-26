namespace FileService.Contracts;

public record GetMediaAssetsDto(
    Guid Id,
    string Status,
    string AssetType,
    string? Url);