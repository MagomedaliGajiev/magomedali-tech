using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using Shared.SharedKernel;

namespace EducationContentService.IntegrationTests.Mocks;

public class FileServiceCommunicationMock : IFileCommunicationService
{
    public Task<Result<GetMediaAssetsResponse, Error>> GetMediaAssets(
        GetMediaAssetsRequest request,
        CancellationToken cancellationToken)
    {
        var mediaAssets = request.MediaAssetIds
            .Select(id => new GetMediaAssetsDto(id, "ready", "video", $"https://test.local/{id}"))
            .ToArray();

        var result = new GetMediaAssetsResponse(mediaAssets);

        return Task.FromResult(
            Result.Success<GetMediaAssetsResponse, Error>(result));
    }
}