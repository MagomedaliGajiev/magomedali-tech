namespace FileService.Core.Features;

public interface IS3Provider
{
    Task UploadFileAsync(
        Stream stream,
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken);

    Task<string> GenerateDownloadUrlAsync(string bucketName, string key);
}