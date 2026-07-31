using CSharpFunctionalExtensions;
using FileService.Core.Models;
using FileService.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.SharedKernel;

namespace FileService.Core.FilesStorage;

public sealed class PresignedUrlCache
{
    private const string CacheKeyPrefix = "file-service:presigned-url:";

    private static readonly TimeSpan ExpirationSafetyMargin = TimeSpan.FromHours(1);
    private static readonly TimeSpan LocalCacheExpiration = TimeSpan.FromHours(1);
    private static readonly HybridCacheEntryOptions CacheReadOptions = new()
    {
        Flags = HybridCacheEntryFlags.DisableUnderlyingData,
        LocalCacheExpiration = LocalCacheExpiration
    };

    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly HybridCache _cache;
    private readonly TimeProvider _timeProvider;

    public PresignedUrlCache(
        IFileStorageProvider fileStorageProvider,
        HybridCache cache,
        TimeProvider timeProvider)
    {
        _fileStorageProvider = fileStorageProvider;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyDictionary<StorageKey, string>, Error>> GetAsync(
        IEnumerable<StorageKey> storageKeys,
        CancellationToken cancellationToken)
    {
        List<StorageKey> keys = storageKeys.Distinct().ToList();

        if (keys.Count == 0)
            return new Dictionary<StorageKey, string>();

        Task<(StorageKey Key, string? Url)>[] cacheReadTasks = keys
            .Select(key => ReadFromCacheAsync(key, cancellationToken))
            .ToArray();

        (StorageKey Key, string? Url)[] cachedUrls = await Task.WhenAll(cacheReadTasks);

        var result = new Dictionary<StorageKey, string>();
        var keysToGenerate = new List<StorageKey>();

        foreach ((StorageKey key, string? url) in cachedUrls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                keysToGenerate.Add(key);
            }
            else
            {
                result[key] = url;
            }
        }

        if (keysToGenerate.Count == 0)
            return result;

        Result<IReadOnlyList<MediaUrl>, Error> mediaUrlsResult = await _fileStorageProvider
            .GenerateDownloadUrlsAsync(keysToGenerate, cancellationToken);

        if (mediaUrlsResult.IsFailure)
            return mediaUrlsResult.Error;

        var cacheWriteTasks = new List<Task>(mediaUrlsResult.Value.Count);

        foreach (MediaUrl mediaUrl in mediaUrlsResult.Value)
        {
            result[mediaUrl.StorageKey] = mediaUrl.PresignedUrl;

            TimeSpan cacheExpiration = mediaUrl.ExpiresAtUtc
                .Subtract(_timeProvider.GetUtcNow())
                .Subtract(ExpirationSafetyMargin);

            if (cacheExpiration <= TimeSpan.Zero)
                continue;

            TimeSpan localExpiration = cacheExpiration < LocalCacheExpiration
                ? cacheExpiration
                : LocalCacheExpiration;

            cacheWriteTasks.Add(_cache.SetAsync(
                    key: GetCacheKey(mediaUrl.StorageKey),
                    value: mediaUrl.PresignedUrl,
                    options: new HybridCacheEntryOptions
                    {
                        Expiration = cacheExpiration,
                        LocalCacheExpiration = localExpiration
                    },
                    cancellationToken: cancellationToken)
                .AsTask());
        }

        await Task.WhenAll(cacheWriteTasks);

        return result;
    }

    private static string GetCacheKey(StorageKey storageKey) =>
        $"{CacheKeyPrefix}{storageKey.FullPath}";

    private async Task<(StorageKey Key, string? Url)> ReadFromCacheAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        string? url = await _cache.GetOrCreateAsync<string?>(
            key: GetCacheKey(storageKey),
            factory: static _ => ValueTask.FromResult<string?>(null),
            options: CacheReadOptions,
            cancellationToken: cancellationToken);

        return (storageKey, url);
    }
}