using CSharpFunctionalExtensions;
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

public sealed class DeleteMediaAsset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/files/{mediaAssetId:guid}", async Task<EndpointResult<string>> (
            [FromRoute] Guid mediaAssetId,
            [FromQuery] string? uploadId,
            [FromServices] DeleteMediaAssetHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(mediaAssetId, uploadId, cancellationToken));
    }
}

public sealed class DeleteMediaAssetHandler
{
    private readonly ILogger<DeleteMediaAssetHandler> _logger;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IMediaAssetsRepository _mediaAssetsRepository;

    public DeleteMediaAssetHandler(
        ILogger<DeleteMediaAssetHandler> logger,
        IFileStorageProvider fileStorageProvider,
        IMediaAssetsRepository mediaAssetsRepository)
    {
        _logger = logger;
        _fileStorageProvider = fileStorageProvider;
        _mediaAssetsRepository = mediaAssetsRepository;
    }

    public async Task<Result<string, Error>> Handle(
        Guid mediaAssetId,
        string? uploadId,
        CancellationToken cancellationToken)
    {
        Result<MediaAsset, Error> mediaAssetResult = await _mediaAssetsRepository
            .GetBy(asset => asset.Id == mediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        MediaAsset mediaAsset = mediaAssetResult.Value;
        if (mediaAsset.Status == MediaStatus.DELETED)
            return mediaAsset.Id.ToString();

        UnitResult<Error> storageResult;
        if (mediaAsset.Status == MediaStatus.UPLOADING)
        {
            if (string.IsNullOrWhiteSpace(uploadId))
                return GeneralErrors.ValueIsRequired(nameof(uploadId));

            storageResult = await _fileStorageProvider.AbortMultipartUploadAsync(
                mediaAsset.UploadKey,
                uploadId,
                cancellationToken);
        }
        else if (mediaAsset.Status == MediaStatus.FAILED && !string.IsNullOrWhiteSpace(uploadId))
        {
            storageResult = await _fileStorageProvider.AbortMultipartUploadAsync(
                mediaAsset.UploadKey,
                uploadId,
                cancellationToken);
        }
        else
        {
            storageResult = UnitResult.Success<Error>();
        }

        if (storageResult.IsFailure)
            return storageResult.Error;

        foreach (StorageKey key in mediaAsset.GetStorageKeys())
        {
            UnitResult<Error> deleteResult = await _fileStorageProvider.DeleteAsync(key, cancellationToken);
            if (deleteResult.IsFailure)
                return deleteResult.Error;
        }

        mediaAsset.MarkDeleted();
        await _mediaAssetsRepository.SaveAsync(cancellationToken);

        _logger.LogInformation("Deleted media asset {MediaAssetId}", mediaAsset.Id);

        return mediaAsset.Id.ToString();
    }
}
