using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.FilesStorage;
using FileService.Core.Models;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class GetMediaAssets : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/batch", async Task<EndpointResult<GetMediaAssetsResponse>> (
            [FromBody] GetMediaAssetsRequest request,
            [FromServices] GetMediaAssetsUploadHandler handler,
            CancellationToken token) => await handler.Handle(request, token));
    }
}

public sealed class GetMediaAssetsUploadHandler
{
    private readonly IReadDbContext _readDbContext;
    private readonly IFileStorageProvider _fileStorageProvider;

    public GetMediaAssetsUploadHandler(
        ILogger<GetMediaAssetsUploadHandler> logger,
        IReadDbContext readDbContext,
        IFileStorageProvider fileStorageProvider)
    {
        _readDbContext = readDbContext;
        _fileStorageProvider = fileStorageProvider;
    }

    public async Task<Result<GetMediaAssetsResponse, Error>> Handle(
        GetMediaAssetsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.MediaAssetIds.Any())
            return new GetMediaAssetsResponse([]);

        List<MediaAsset> mediaAssets = await _readDbContext.MediaAssetsQuery
            .Where(m => request.MediaAssetIds.Contains(m.Id) && m.Status != MediaStatus.DELETED)
            .ToListAsync(cancellationToken);

        var readyMediaAssets = mediaAssets
            .Where(m => m.Status == MediaStatus.READY)
            .ToList();

        var keys = readyMediaAssets.Select(m => m.Key).ToList();

        (_, bool isFailure, IReadOnlyList<MediaUrl> urls, Error error) = await _fileStorageProvider
            .GenerateDownloadUrlsAsync(keys, cancellationToken);

        if (isFailure)
            return error;

        var urlsDict = urls.ToDictionary(url => url.StorageKey, url => url.PresignedUrl);

        var results = new List<GetMediaAssetsDto>();
        foreach (MediaAsset mediaAsset in mediaAssets)
        {
            string? downloadUrl = null;

            if (urlsDict.TryGetValue(mediaAsset.Key, out string? url))
            {
                downloadUrl = url;
            }

            var mediaAssetDto = new GetMediaAssetsDto(
                mediaAsset.Id,
                mediaAsset.Status.ToString().ToLowerInvariant(),
                mediaAsset.AssetType.ToString().ToLowerInvariant(),
                downloadUrl);

            results.Add(mediaAssetDto);
        }

        return new GetMediaAssetsResponse(results);
    }
}