using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.FilesStorage;
using FileService.Core.Models;
using FileService.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Shared.SharedKernel;

namespace FileService.IntegrationTests.Features;

public sealed class PresignedUrlCacheTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_WhenUrlIsCached_DoesNotCallStorageProviderAgain()
    {
        StorageKey storageKey = CreateStorageKey("videos", "raw", "asset-id");
        var storageProvider = new StubFileStorageProvider(keys =>
            keys.Select(key => new MediaUrl(
                    key,
                    $"https://cdn.test/{key.FullPath}",
                    UtcNow.AddHours(2)))
                .ToList());

        await using ServiceProvider services = CreateServices();
        var sut = new PresignedUrlCache(
            storageProvider,
            services.GetRequiredService<HybridCache>(),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyDictionary<StorageKey, string>, Error> firstResult =
            await sut.GetAsync([storageKey], CancellationToken.None);
        Result<IReadOnlyDictionary<StorageKey, string>, Error> secondResult =
            await sut.GetAsync([storageKey], CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal("https://cdn.test/videos/raw/asset-id", secondResult.Value[storageKey]);
        Assert.Equal(1, storageProvider.GenerateDownloadUrlsCallCount);
    }

    [Fact]
    public async Task GetAsync_WhenStorageProviderFails_ReturnsOriginalError()
    {
        StorageKey storageKey = CreateStorageKey("videos", "raw", "asset-id");
        Error storageError = Error.Failure("storage.unavailable", "Storage is unavailable.");
        var storageProvider = new StubFileStorageProvider(_ => storageError);

        await using ServiceProvider services = CreateServices();
        var sut = new PresignedUrlCache(
            storageProvider,
            services.GetRequiredService<HybridCache>(),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyDictionary<StorageKey, string>, Error> result =
            await sut.GetAsync([storageKey], CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(storageError, result.Error);
    }

    [Fact]
    public async Task GetAsync_WhenObjectKeysMatchInDifferentBuckets_CachesUrlsSeparately()
    {
        StorageKey videoKey = CreateStorageKey("videos", "raw", "shared-key");
        StorageKey previewKey = CreateStorageKey("preview", "raw", "shared-key");
        var storageProvider = new StubFileStorageProvider(keys =>
            keys.Select(key => new MediaUrl(
                    key,
                    $"https://cdn.test/{key.FullPath}",
                    UtcNow.AddHours(2)))
                .ToList());

        await using ServiceProvider services = CreateServices();
        var sut = new PresignedUrlCache(
            storageProvider,
            services.GetRequiredService<HybridCache>(),
            new FixedTimeProvider(UtcNow));

        Result<IReadOnlyDictionary<StorageKey, string>, Error> videoResult =
            await sut.GetAsync([videoKey], CancellationToken.None);
        Result<IReadOnlyDictionary<StorageKey, string>, Error> previewResult =
            await sut.GetAsync([previewKey], CancellationToken.None);

        Assert.Equal("https://cdn.test/videos/raw/shared-key", videoResult.Value[videoKey]);
        Assert.Equal("https://cdn.test/preview/raw/shared-key", previewResult.Value[previewKey]);
        Assert.Equal(2, storageProvider.GenerateDownloadUrlsCallCount);
    }

    [Fact]
    public async Task GetAsync_WhenUrlExpiresWithinSafetyMargin_DoesNotCacheUrl()
    {
        StorageKey storageKey = CreateStorageKey("videos", "raw", "short-lived");
        var storageProvider = new StubFileStorageProvider(keys =>
            keys.Select(key => new MediaUrl(
                    key,
                    $"https://cdn.test/{key.FullPath}",
                    UtcNow.AddMinutes(30)))
                .ToList());

        await using ServiceProvider services = CreateServices();
        var sut = new PresignedUrlCache(
            storageProvider,
            services.GetRequiredService<HybridCache>(),
            new FixedTimeProvider(UtcNow));

        await sut.GetAsync([storageKey], CancellationToken.None);
        await sut.GetAsync([storageKey], CancellationToken.None);

        Assert.Equal(2, storageProvider.GenerateDownloadUrlsCallCount);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();

        return services.BuildServiceProvider();
    }

    private static StorageKey CreateStorageKey(string location, string prefix, string key)
    {
        Result<StorageKey, Error> result = StorageKey.Create(location, prefix, key);
        return result.Value;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class StubFileStorageProvider : IFileStorageProvider
    {
        private readonly Func<IReadOnlyList<StorageKey>, Result<IReadOnlyList<MediaUrl>, Error>> _generateUrls;

        public StubFileStorageProvider(
            Func<IReadOnlyList<StorageKey>, Result<IReadOnlyList<MediaUrl>, Error>> generateUrls)
        {
            _generateUrls = generateUrls;
        }

        public int GenerateDownloadUrlsCallCount { get; private set; }

        public Task<Result<string, Error>> StartMultipartUploadAsync(
            StorageKey storageKey,
            MediaData mediaData,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
            StorageKey storageKey,
            string uploadId,
            int totalChunks,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<MediaUrl>, Error>> GenerateDownloadUrlsAsync(
            IEnumerable<StorageKey> storageKeys,
            CancellationToken cancellationToken)
        {
            GenerateDownloadUrlsCallCount++;

            return Task.FromResult(_generateUrls(storageKeys.ToList()));
        }

        public Task<Result<string, Error>> CompleteMultipartUploadAsync(
            StorageKey storageKey,
            string uploadId,
            IReadOnlyList<PartETagDto> partETags,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UnitResult<Error>> AbortMultipartUploadAsync(
            StorageKey storageKey,
            string uploadId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UnitResult<Error>> DeleteAsync(
            StorageKey storageKey,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
