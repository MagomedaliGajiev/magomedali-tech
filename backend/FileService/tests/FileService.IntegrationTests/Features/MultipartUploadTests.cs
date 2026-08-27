using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.Features;
using FileService.Domain.Assets;
using FileService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.SharedKernel;
using CompleteMultipartUploadRequest = FileService.Contracts.Dtos.CompleteMultipartUploadRequest;

namespace FileService.IntegrationTests.Features;

public class MultipartUploadTests : FileServiceTestsBase
{
    private readonly IntegrationTestsWebFactory _factory;

    public MultipartUploadTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MultipartUpload_FullCycle_PersistsMediaFile()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo fileInfo = new(Path.Combine(AppContext.BaseDirectory, "Resources", TEST_FILE_NAME));
        Guid ownerId = Guid.NewGuid();

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(
            fileInfo,
            ownerId,
            cancellationToken);

        IReadOnlyList<PartETagDto> partETags = await UploadChunks(fileInfo, startMultipartUploadResponse, cancellationToken);

        UnitResult<Error> result = await CompletMultipartUpload(startMultipartUploadResponse, partETags, cancellationToken);

        // assert
        Assert.True(result.IsSuccess);

        Result<CheckMediaAssetExistsResponse, Error> existsResult = await CheckMediaAssetExists(
            startMultipartUploadResponse.MediaAssetId,
            cancellationToken);
        Assert.True(existsResult.IsSuccess);
        Assert.True(existsResult.Value.Exists);

        await ExecuteInDb(async db =>
        {
            MediaAsset? mediaAsset = await db.MediaAssets.FirstOrDefaultAsync(
                m => m.Id == startMultipartUploadResponse.MediaAssetId, cancellationToken);

            Assert.Equal(MediaStatus.READY, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);
            Assert.Equal("lesson", mediaAsset.Owner.Context);
            Assert.Equal(ownerId, mediaAsset.Owner.EntityId);

            IAmazonS3 amazonS3client = _factory.Services.GetRequiredService<IAmazonS3>();

            GetObjectResponse objectResponse = await amazonS3client.GetObjectAsync(
                mediaAsset.Key.Location,
                mediaAsset.Key.Value,
                cancellationToken);

            Assert.Equal(objectResponse.ContentLength, fileInfo.Length);
            Assert.Equal(objectResponse.Key, mediaAsset.Key.Value);
        });
    }

    [Fact]
    public async Task DeleteMediaAsset_ActiveUpload_AbortsMultipartAndMarksAssetDeleted()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        FileInfo fileInfo = new(Path.Combine(AppContext.BaseDirectory, "Resources", TEST_FILE_NAME));
        StartMultipartUploadResponse upload = await StartMultipartUpload(
            fileInfo,
            Guid.NewGuid(),
            cancellationToken);
        string bucketName = string.Empty;
        string storageKey = string.Empty;

        await ExecuteInDb(async db =>
        {
            MediaAsset mediaAsset = await db.MediaAssets.SingleAsync(
                asset => asset.Id == upload.MediaAssetId,
                cancellationToken);
            bucketName = mediaAsset.Key.Location;
            storageKey = mediaAsset.Key.Value;
        });

        IAmazonS3 amazonS3Client = _factory.Services.GetRequiredService<IAmazonS3>();
        var listPartsRequest = new ListPartsRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            UploadId = upload.UploadId
        };
        await amazonS3Client.ListPartsAsync(listPartsRequest, cancellationToken);

        HttpResponseMessage deleteResponse = await AppHttpClient.DeleteAsync(
            $"/api/files/{upload.MediaAssetId}?uploadId={Uri.EscapeDataString(upload.UploadId)}",
            cancellationToken);
        Result<string, Error> deleteResult = await deleteResponse.HandleResponseAsync<string>(cancellationToken);

        Assert.True(deleteResult.IsSuccess);
        Assert.Equal(upload.MediaAssetId.ToString(), deleteResult.Value);

        Result<CheckMediaAssetExistsResponse, Error> existsResult = await CheckMediaAssetExists(
            upload.MediaAssetId,
            cancellationToken);
        Assert.True(existsResult.IsSuccess);
        Assert.False(existsResult.Value.Exists);

        await ExecuteInDb(async db =>
        {
            MediaAsset mediaAsset = await db.MediaAssets.SingleAsync(
                asset => asset.Id == upload.MediaAssetId,
                cancellationToken);
            Assert.Equal(MediaStatus.DELETED, mediaAsset.Status);
        });

        AmazonS3Exception exception = await Assert.ThrowsAsync<AmazonS3Exception>(() =>
            amazonS3Client.ListPartsAsync(listPartsRequest, cancellationToken));
        Assert.Equal("NoSuchUpload", exception.ErrorCode);
    }

    [Fact]
    public async Task DeleteMediaAsset_RemovesObjectAndMarksAssetDeleted()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        FileInfo fileInfo = new(Path.Combine(AppContext.BaseDirectory, "Resources", TEST_FILE_NAME));
        StartMultipartUploadResponse upload = await StartMultipartUpload(
            fileInfo,
            Guid.NewGuid(),
            cancellationToken);
        IReadOnlyList<PartETagDto> partETags = await UploadChunks(fileInfo, upload, cancellationToken);
        UnitResult<Error> completeResult = await CompletMultipartUpload(upload, partETags, cancellationToken);
        Assert.True(completeResult.IsSuccess);

        HttpResponseMessage deleteResponse = await AppHttpClient.DeleteAsync(
            $"/api/files/{upload.MediaAssetId}",
            cancellationToken);
        Result<string, Error> deleteResult = await deleteResponse.HandleResponseAsync<string>(cancellationToken);

        Assert.True(deleteResult.IsSuccess);
        Assert.Equal(upload.MediaAssetId.ToString(), deleteResult.Value);

        await ExecuteInDb(async db =>
        {
            MediaAsset mediaAsset = await db.MediaAssets.SingleAsync(
                asset => asset.Id == upload.MediaAssetId,
                cancellationToken);
            Assert.Equal(MediaStatus.DELETED, mediaAsset.Status);

            IAmazonS3 amazonS3Client = _factory.Services.GetRequiredService<IAmazonS3>();
            AmazonS3Exception exception = await Assert.ThrowsAnyAsync<AmazonS3Exception>(() =>
                amazonS3Client.GetObjectAsync(
                    mediaAsset.Key.Location,
                    mediaAsset.Key.Value,
                    cancellationToken));
            Assert.Equal(System.Net.HttpStatusCode.NotFound, exception.StatusCode);
        });
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(
        FileInfo fileInfo,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var request = new StartMultipartUploadRequest(
            fileInfo.Name,
            "video",
            "video/mp4",
            fileInfo.Length,
            "lesson",
            ownerId);

        HttpResponseMessage startMultipartResponse = await AppHttpClient.PostAsJsonAsync(
            "/api/files/multipart-upload",
            request,
            cancellationToken);

        Result<StartMultipartUploadResponse, Error> startMultipartResult = await startMultipartResponse
            .HandleResponseAsync<StartMultipartUploadResponse>(cancellationToken);

        Assert.True(startMultipartResult.IsSuccess);
        Assert.NotNull(startMultipartResult.Value.UploadId);
        Assert.True(startMultipartResult.Value.ChunkUploadUrls.Count > 1);

        await ExecuteInDb(async db =>
        {
            MediaAsset? mediaAsset = await db.MediaAssets.FirstOrDefaultAsync(
                m => m.Id == startMultipartResult.Value.MediaAssetId, cancellationToken);

            Assert.Equal(MediaStatus.UPLOADING, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);
        });

        return startMultipartResult.Value;
    }

    private async Task<Result<CheckMediaAssetExistsResponse, Error>> CheckMediaAssetExists(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await AppHttpClient.GetAsync(
            $"/api/files/{mediaAssetId}/exists",
            cancellationToken);

        return await response.HandleResponseAsync<CheckMediaAssetExistsResponse>(cancellationToken);
    }

    private async Task<IReadOnlyList<PartETagDto>> UploadChunks(
        FileInfo fileInfo,
        StartMultipartUploadResponse startMultipartUploadResponse,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = fileInfo.OpenRead();

        var parts = new List<PartETagDto>();

        foreach (ChunkUploadUrl chunkUploadUrl in startMultipartUploadResponse.ChunkUploadUrls.OrderBy(c => c.PartNumber))
        {
            long remainingBytes = stream.Length - stream.Position;
            if (remainingBytes == 0)
                break;

            int bytesToRead = checked((int)Math.Min(startMultipartUploadResponse.ChunkSize, remainingBytes));
            byte[] chunk = new byte[bytesToRead];
            await stream.ReadExactlyAsync(chunk, cancellationToken);

            using var content = new ByteArrayContent(chunk);
            using HttpResponseMessage response = await HttpClient
                .PutAsync(chunkUploadUrl.UploadUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            Assert.NotNull(response.Headers.ETag);
            string etag = response.Headers.ETag.Tag.Trim('"');

            parts.Add(new PartETagDto(chunkUploadUrl.PartNumber, etag));
        }

        Assert.Equal(startMultipartUploadResponse.ChunkUploadUrls.Count, parts.Count);

        return parts;
    }

    private async Task<UnitResult<Error>> CompletMultipartUpload(
        StartMultipartUploadResponse startMultipartUploadResponse,
        IEnumerable<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        var completeRequest = new CompleteMultipartUploadRequest(
            startMultipartUploadResponse.MediaAssetId,
            startMultipartUploadResponse.UploadId,
            partETags.ToList());

        HttpResponseMessage completeResponse = await AppHttpClient
            .PostAsJsonAsync("/api/files/complete-upload", completeRequest, cancellationToken);

        UnitResult<Error> completeMultipart = await completeResponse
            .HandleResponseAsync(cancellationToken);

        return completeMultipart;
    }
}
