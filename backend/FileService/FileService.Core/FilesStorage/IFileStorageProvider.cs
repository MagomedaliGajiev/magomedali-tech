using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Models;
using FileService.Domain;
using Shared.SharedKernel;

namespace FileService.Core.FilesStorage;

public interface IFileStorageProvider
{
    Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey);

    Task<Result<IReadOnlyList<MediaUrl>, Error>> GenerateDownloadUrlsAsync(
        IEnumerable<StorageKey> storageKeys,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken);
}