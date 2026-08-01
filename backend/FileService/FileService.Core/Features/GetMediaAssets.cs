using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.FilesStorage;
using FileService.Domain;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
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
    private readonly PresignedUrlCache _presignedUrlCache;

    public GetMediaAssetsUploadHandler(
        IReadDbContext readDbContext,
        PresignedUrlCache presignedUrlCache)
    {
        _readDbContext = readDbContext;
        _presignedUrlCache = presignedUrlCache;
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

        Result<IReadOnlyDictionary<StorageKey, string>, Error> urlsResult = await _presignedUrlCache
            .GetAsync(keys, cancellationToken);

        if (urlsResult.IsFailure)
            return urlsResult.Error;

        var results = new List<GetMediaAssetsDto>();
        foreach (MediaAsset mediaAsset in mediaAssets)
        {
            string? downloadUrl = null;

            if (urlsResult.Value.TryGetValue(mediaAsset.Key, out string? url))
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