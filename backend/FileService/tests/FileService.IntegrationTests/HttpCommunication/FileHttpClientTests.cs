using System.Net;
using System.Net.Http.Json;
using FileService.Contracts.Dtos;
using FileService.Contracts.HttpCommunication;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.SharedKernel;

namespace FileService.IntegrationTests.HttpCommunication;

public class FileHttpClientTests
{
    [Fact]
    public async Task GetMediaAssets_SendsPostRequestWithMediaAssetIds()
    {
        var messageHandler = new RecordingMessageHandler();
        using var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://file-service/"),
        };
        var sut = new FileHttpClient(httpClient, NullLogger<FileHttpClient>.Instance);
        Guid[] mediaAssetIds = [Guid.NewGuid(), Guid.NewGuid()];

        var result = await sut.GetMediaAssets(
            new GetMediaAssetsRequest(mediaAssetIds),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, messageHandler.Method);
        Assert.Equal("/api/files/batch", messageHandler.RequestUri?.AbsolutePath);
        Assert.Equal(mediaAssetIds, messageHandler.Request?.MediaAssetIds);
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public GetMediaAssetsRequest? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Request = await request.Content!.ReadFromJsonAsync<GetMediaAssetsRequest>(cancellationToken);

            var response = new GetMediaAssetsResponse([]);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Envelope<GetMediaAssetsResponse>.Ok(response)),
            };
        }
    }
}
