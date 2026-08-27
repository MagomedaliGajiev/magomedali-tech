using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Domain.Assets;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class CheckMediaAssetExists : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{mediaAssetId:guid}/exists", async Task<EndpointResult<CheckMediaAssetExistsResponse>> (
            [FromRoute] Guid mediaAssetId,
            [FromServices] CheckMediaAssetExistsHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(mediaAssetId, cancellationToken));
    }
}

public sealed class CheckMediaAssetExistsHandler
{
    private readonly IReadDbContext _readDbContext;

    public CheckMediaAssetExistsHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public async Task<Result<CheckMediaAssetExistsResponse, Error>> Handle(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        bool exists = await _readDbContext.MediaAssetsQuery.AnyAsync(
            mediaAsset => mediaAsset.Id == mediaAssetId && mediaAsset.Status != MediaStatus.DELETED,
            cancellationToken);

        return new CheckMediaAssetExistsResponse(exists);
    }
}
