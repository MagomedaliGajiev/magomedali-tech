namespace FileService.Contracts;

public record StartMultipartUploadRequest(
    string FileName,
    string AssetType,
    string ContentType,
    long Size);