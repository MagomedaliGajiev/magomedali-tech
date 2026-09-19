using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Processing;

namespace FileService.VideoProcessing.Pipeline;

public sealed record ProcessingContext
{
    public required VideoProcess VideoProcessing { get; init; }

    public required VideoAsset VideoAsset { get; init; }

    public string? WorkingDirectory { get; set; }

    public string? HlsOutputDirectory { get; set; }

    public string? MediaAssetUrl { get; set; }

    public VideoMetadata? Metadata { get; set; }

    public StorageKey? FinalKey { get; set; }

    public StorageKey? PreviewKey { get; set; }
}
