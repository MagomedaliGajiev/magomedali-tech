using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.FilesStorage;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class CompleteMultipartUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files/complete-upload", async Task<EndpointResult> (
            [FromBody] CompleteMultipartUploadRequest request,
            [FromServices] CompleteMultipartUploadHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class CompleteMultipartUploadHandler
{
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IMediaAssetsRepository _mediaAssetsRepository;

    public CompleteMultipartUploadHandler(
        ILogger<CompleteMultipartUploadHandler> logger,
        IFileStorageProvider fileStorageProvider,
        IMediaAssetsRepository mediaAssetsRepository)
    {
        _logger = logger;
        _fileStorageProvider = fileStorageProvider;
        _mediaAssetsRepository = mediaAssetsRepository;
    }

    public async Task<UnitResult<Error>> Handle(
        CompleteMultipartUploadRequest request,
        CancellationToken cancellationToken)
    {
        (_, bool isFailure, MediaAsset mediaAsset, Error error) = await _mediaAssetsRepository
            .GetBy(m => m.Id == request.MediaAssetId, cancellationToken);
        if (isFailure)
            return error;

        if (mediaAsset.Status != MediaStatus.UPLOADING)
            return GeneralErrors.Failure($"Медиафайл нельзя завершить в статусе {mediaAsset.Status}");

        if (string.IsNullOrWhiteSpace(request.UploadId))
            return GeneralErrors.ValueIsInvalid(nameof(request.UploadId));

        int expectedChunksCount = mediaAsset.MediaData.ExpectedChunksCount;
        if (request.PartETags is null ||
            request.PartETags.Count != expectedChunksCount ||
            request.PartETags.Any(part => part.PartNumber < 1 ||
                                         part.PartNumber > expectedChunksCount ||
                                         string.IsNullOrWhiteSpace(part.ETag)) ||
            request.PartETags.Select(part => part.PartNumber).Distinct().Count() != expectedChunksCount)
        {
            return GeneralErrors.ValueIsInvalid(nameof(request.PartETags));
        }

        Result<string, Error> completeResult = await _fileStorageProvider.CompleteMultipartUploadAsync(
            mediaAsset.Key,
            request.UploadId,
            request.PartETags,
            cancellationToken);

        if (completeResult.IsFailure)
        {
            mediaAsset.MarkFailed();
            await _mediaAssetsRepository.SaveAsync(cancellationToken);

            return completeResult.Error;
        }

        UnitResult<Error> markUploadedResult = mediaAsset.MarkUploaded();
        if (markUploadedResult.IsFailure)
            return markUploadedResult.Error;

        await _mediaAssetsRepository.SaveAsync(cancellationToken);

        _logger.LogInformation("File uploaded successfully. MediaAssetId: {MediaAssetId}", mediaAsset.Id);

        return Result.Success<Error>();
    }
}