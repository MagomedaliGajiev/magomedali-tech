using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.FilesStorage;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class GetMediaAsset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{mediaAssetId:guid}", async Task<EndpointResult<GetMediaAssetDto?>> (
            Guid mediaAssetId,
            [FromServices] GetMediaAssetUploadHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(mediaAssetId, cancellationToken));
    }

    public sealed class GetMediaAssetUploadHandler
    {
        private readonly IReadDbContext _readDbContext;
        private readonly IFileStorageProvider _fileStorageProvider;

        public GetMediaAssetUploadHandler(
            IReadDbContext readDbContext,
            IFileStorageProvider fileStorageProvider)
        {
            _readDbContext = readDbContext;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task<Result<GetMediaAssetDto?, Error>> Handle(
            Guid mediaAssetId,
            CancellationToken cancellationToken)
        {
            MediaAsset? mediaAsset = await _readDbContext.MediaAssetsQuery
                .FirstOrDefaultAsync(
                    m => m.Id == mediaAssetId && m.Status != MediaStatus.DELETED,
                    cancellationToken);

            if (mediaAsset == null)
                return Result.Success<GetMediaAssetDto?, Error>(null);

            string? url = null;

            if (mediaAsset.Status == MediaStatus.READY)
            {
                (_, bool isFailure, string presignedUrl, Error? error) =
                    await _fileStorageProvider.GenerateDownloadUrlAsync(mediaAsset.Key);

                if (isFailure)
                    return error;

                url = presignedUrl;
            }

            var mediaAssetDto = new GetMediaAssetDto(
                mediaAsset.Id,
                mediaAsset.Status.ToString().ToLowerInvariant(),
                mediaAsset.AssetType.ToString().ToLowerInvariant(),
                url,
                mediaAsset.MediaData.Size,
                mediaAsset.MediaData.FileName.Value,
                mediaAsset.MediaData.ContentType.Value);

            return mediaAssetDto;
        }
    }
}