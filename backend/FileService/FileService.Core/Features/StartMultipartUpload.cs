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
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class StartMultipartUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/multipart-upload", async Task<EndpointResult<StartMultipartUploadResponse>> (
            [FromBody] StartMultipartUploadRequest request,
            [FromServices] StartMultipartUploadHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class StartMultipartUploadHandler
{
    private readonly ILogger<StartMultipartUploadHandler> _logger;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IMediaAssetsRepository _mediaAssetsRepository;

    public StartMultipartUploadHandler(
        ILogger<StartMultipartUploadHandler> logger,
        IFileStorageProvider fileStorageProvider,
        IChunkSizeCalculator chunkSizeCalculator,
        IMediaAssetsRepository mediaAssetsRepository)
    {
        _logger = logger;
        _fileStorageProvider = fileStorageProvider;
        _chunkSizeCalculator = chunkSizeCalculator;
        _mediaAssetsRepository = mediaAssetsRepository;
    }

    public async Task<Result<StartMultipartUploadResponse, Error>> Handle(
        StartMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        Result<FileName, Error> fileName = FileName.Create(request.FileName);
        if (fileName.IsFailure)
            return fileName.Error;

        Result<ContentType, Error> contentType = ContentType.Create(request.ContentType);
        if (contentType.IsFailure)
            return contentType.Error;

        Result<AssetType, Error> assetType = request.AssetType.ToAssetType();
        if (assetType.IsFailure)
            return assetType.Error;

        // TODO: Replace the temporary owner with the actual upload context.
        Result<MediaOwner, Error> owner = MediaOwner.ForUser(Guid.NewGuid());
        if (owner.IsFailure)
            return owner.Error;

        Result<(long ChunkSize, int TotalChunks), Error> chunkCalculationResult = _chunkSizeCalculator
            .CalculateChunkSize(request.Size);
        if (chunkCalculationResult.IsFailure)
            return chunkCalculationResult.Error;

        Result<MediaData, Error> mediaDataResult = MediaData.Create(
            fileName.Value,
            contentType.Value,
            request.Size,
            chunkCalculationResult.Value.TotalChunks);
        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset
            .CreateForUpload(mediaDataResult.Value, owner.Value, assetType.Value);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;
        Result<Guid, Error> addResult = await _mediaAssetsRepository.AddAsync(mediaAsset, cancellationToken);
        if (addResult.IsFailure)
            return addResult.Error;

        Result<string, Error> startUploadResult = await _fileStorageProvider.StartMultipartUploadAsync(
            mediaAsset.Key,
            mediaAsset.MediaData,
            cancellationToken);
        if (startUploadResult.IsFailure)
        {
            await MarkFailed(mediaAsset);
            return startUploadResult.Error;
        }

        Result<IReadOnlyList<ChunkUploadUrl>, Error> chunkUploadUrlsResult = await _fileStorageProvider.GenerateAllChunksUploadUrlsAsync(
            mediaAsset.Key,
            startUploadResult.Value,
            chunkCalculationResult.Value.TotalChunks,
            cancellationToken);
        if (chunkUploadUrlsResult.IsFailure)
        {
            await _fileStorageProvider.AbortMultipartUploadAsync(
                mediaAsset.Key,
                startUploadResult.Value,
                CancellationToken.None);
            await MarkFailed(mediaAsset);

            return chunkUploadUrlsResult.Error;
        }

        _logger.LogInformation(
            "Media asset {MediaAssetId} started uploading with key {StorageKey}",
            mediaAsset.Id,
            mediaAsset.Key);

        return new StartMultipartUploadResponse(
            mediaAsset.Id,
            startUploadResult.Value,
            chunkUploadUrlsResult.Value,
            chunkCalculationResult.Value.ChunkSize);
    }

    private async Task MarkFailed(MediaAsset mediaAsset)
    {
        mediaAsset.MarkFailed();
        await _mediaAssetsRepository.SaveAsync(CancellationToken.None);
    }
}