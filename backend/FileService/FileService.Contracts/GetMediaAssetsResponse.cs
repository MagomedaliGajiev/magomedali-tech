namespace FileService.Contracts;

public record GetMediaAssetsResponse(IReadOnlyList<GetMediaAssetsDto> MediaAssets);