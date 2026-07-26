namespace FileService.Contracts;

public record GetMediaAssetsRequest(IReadOnlyList<Guid> MediaAssetIds);