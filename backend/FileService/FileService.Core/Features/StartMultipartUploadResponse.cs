using FileService.Contracts;
using FileService.Contracts.Dtos;

namespace FileService.Core.Features;

public record StartMultipartUploadResponse(
    Guid MediaAssetId,
    string UploadId,
    IReadOnlyList<ChunkUploadUrl> ChunkUploadUrls,
    long ChunkSize);