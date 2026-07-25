using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core;
using FileService.Core.Features;
using FileService.Core.FilesStorage;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Infrastructure.S3;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.SharedKernel;
using Xunit;

namespace FileService.Tests;

public sealed class StartMultipartUploadHandlerTests
{
    [Fact]
    public async Task HandleReturnsValidationErrorForZeroSize()
    {
        var repository = new FakeMediaAssetsRepository();
        var storage = new FakeFileStorageProvider();
        var calculator = new ChunkSizeCalculator(Options.Create(new S3Options()));
        var handler = CreateHandler(storage, calculator, repository);
        var request = CreateRequest(size: 0);

        Result<StartMultipartUploadResponse, Error> result = await handler.Handle(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(storage.StartCalled);
        Assert.Null(repository.AddedAsset);
    }

    [Fact]
    public async Task HandleStopsWhenRepositoryAddFails()
    {
        var repository = new FakeMediaAssetsRepository
        {
            AddError = GeneralErrors.Failure("database unavailable")
        };
        var storage = new FakeFileStorageProvider();
        var calculator = new ChunkSizeCalculator(Options.Create(new S3Options()));
        var handler = CreateHandler(storage, calculator, repository);

        Result<StartMultipartUploadResponse, Error> result =
            await handler.Handle(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(storage.StartCalled);
    }

    [Fact]
    public async Task HandleMarksAssetFailedWhenStorageStartFails()
    {
        var repository = new FakeMediaAssetsRepository();
        var storage = new FakeFileStorageProvider
        {
            StartError = FileErrors.NetworkIssue()
        };
        var calculator = new ChunkSizeCalculator(Options.Create(new S3Options()));
        var handler = CreateHandler(storage, calculator, repository);

        Result<StartMultipartUploadResponse, Error> result =
            await handler.Handle(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(repository.AddedAsset);
        Assert.Equal(MediaStatus.FAILED, repository.AddedAsset.Status);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task HandlePersistsOwnerAndReturnsUploadData()
    {
        var repository = new FakeMediaAssetsRepository();
        var storage = new FakeFileStorageProvider();
        var calculator = new ChunkSizeCalculator(Options.Create(new S3Options()));
        var handler = CreateHandler(storage, calculator, repository);
        Guid contextId = Guid.NewGuid();

        Result<StartMultipartUploadResponse, Error> result =
            await handler.Handle(CreateRequest(contextId: contextId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedAsset);
        Assert.Equal("lesson", repository.AddedAsset.Owner.Context);
        Assert.Equal(contextId, repository.AddedAsset.Owner.EntityId);
        Assert.Equal("raw", repository.AddedAsset.Key.Prefix);
        Assert.Equal("upload-id", result.Value.UploadId);
    }

    private static StartMultipartUploadHandler CreateHandler(
        IFileStorageProvider storage,
        IChunkSizeCalculator calculator,
        IMediaAssetsRepository repository)
    {
        return new StartMultipartUploadHandler(
            NullLogger<StartMultipartUploadHandler>.Instance,
            storage,
            calculator,
            repository);
    }

    private static StartMultipartUploadRequest CreateRequest(long size = 1024, Guid? contextId = null)
    {
        return new StartMultipartUploadRequest(
            "video.mp4",
            "video",
            "video/mp4",
            size,
            "lesson",
            contextId ?? Guid.NewGuid());
    }

    private sealed class FakeMediaAssetsRepository : IMediaAssetsRepository
    {
        public Error? AddError { get; init; }

        public MediaAsset? AddedAsset { get; private set; }

        public int SaveCalls { get; private set; }

        public Task<Result<Guid, Error>> AddAsync(
            MediaAsset mediaAsset,
            CancellationToken cancellationToken = default)
        {
            AddedAsset = mediaAsset;

            Result<Guid, Error> result = AddError is null ? mediaAsset.Id : AddError;
            return Task.FromResult(result);
        }

        public Task<Result<MediaAsset, Error>> GetBy(
            Expression<Func<MediaAsset, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            Result<MediaAsset, Error> result = AddedAsset is null
                ? GeneralErrors.NotFound(null, "media asset")
                : AddedAsset;
            return Task.FromResult(result);
        }

        public Task<int> SaveAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeFileStorageProvider : IFileStorageProvider
    {
        public Error? StartError { get; init; }

        public bool StartCalled { get; private set; }

        public Task<Result<string, Error>> StartMultipartUploadAsync(
            StorageKey storageKey,
            MediaData mediaData,
            CancellationToken cancellationToken)
        {
            StartCalled = true;

            Result<string, Error> result = StartError is null ? "upload-id" : StartError;
            return Task.FromResult(result);
        }

        public Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
            StorageKey storageKey,
            string uploadId,
            int totalChunks,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ChunkUploadUrl> urls =
                Enumerable.Range(1, totalChunks)
                    .Select(number => new ChunkUploadUrl(number, $"https://upload/{number}"))
                    .ToArray();
            Result<IReadOnlyList<ChunkUploadUrl>, Error> result =
                Result.Success<IReadOnlyList<ChunkUploadUrl>, Error>(urls);
            return Task.FromResult(result);
        }

        public Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey)
        {
            Result<string, Error> result = "https://download";
            return Task.FromResult(result);
        }

        public Task<Result<string, Error>> CompleteMultipartUploadAsync(
            StorageKey storageKey,
            string uploadId,
            IReadOnlyList<PartETagDto> partETags,
            CancellationToken cancellationToken)
        {
            Result<string, Error> result = storageKey.Value;
            return Task.FromResult(result);
        }

        public Task<UnitResult<Error>> AbortMultipartUploadAsync(
            StorageKey storageKey,
            string uploadId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UnitResult.Success<Error>());
        }
    }
}