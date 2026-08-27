using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Contracts.HttpCommunication;

internal sealed class FileHttpClient : IFileCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileHttpClient> _logger;

    public FileHttpClient(HttpClient httpClient, ILogger<FileHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<CheckMediaAssetExistsResponse, Error>> CheckMediaAssetExists(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                $"api/files/{mediaAssetId}/exists",
                cancellationToken);

            return await response.HandleResponseAsync<CheckMediaAssetExistsResponse>(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking media asset {MediaAssetId}", mediaAssetId);

            return Error.Failure("server.internal", "Failed to check media asset existence");
        }
    }

    public async Task<Result<GetMediaAssetsResponse, Error>> GetMediaAssets(
        GetMediaAssetsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await System.Net.Http.Json.HttpClientJsonExtensions
                .PostAsJsonAsync(_httpClient, "api/files/batch", request, cancellationToken);

            return await response.HandleResponseAsync<GetMediaAssetsResponse>(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media assets for {MediaAssetsIds}", request.MediaAssetIds);

            return Error.Failure("server.internal", "Failed to request media assets info");
        }
    }
}
