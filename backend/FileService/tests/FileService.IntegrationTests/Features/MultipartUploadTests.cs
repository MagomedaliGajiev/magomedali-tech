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

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(fileInfo, cancellationToken);

        IReadOnlyList<PartETagDto> partETags = await UploadChunks(fileInfo, startMultipartUploadResponse, cancellationToken);

        UnitResult<Error> result = await CompletMultipartUpload(startMultipartUploadResponse, partETags, cancellationToken);

        // assert
        Assert.True(result.IsSuccess);

        await ExecuteInDb(async db =>
        {
            MediaAsset? mediaAsset = await db.MediaAssets.FirstOrDefaultAsync(
                m => m.Id == startMultipartUploadResponse.MediaAssetId, cancellationToken);

            Assert.Equal(MediaStatus.UPLOADED, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);

            IAmazonS3 amazonS3client = _factory.Services.GetRequiredService<IAmazonS3>();

            GetObjectResponse objectResponse = await amazonS3client.GetObjectAsync(
                mediaAsset.Key.Location,
                mediaAsset.Key.Value,
                cancellationToken);

            Assert.Equal(objectResponse.ContentLength, fileInfo.Length);
            Assert.Equal(objectResponse.Key, mediaAsset.Key.Value);
        });
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(FileInfo fileInfo, CancellationToken cancellationToken)
    {
        var request = new StartMultipartUploadRequest(
            fileInfo.Name,
            "video",
            "video/mp4",
            fileInfo.Length);

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