using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Domain.Assets;
using FileService.Domain.Processing;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Core.Features;

public sealed class GetVideoProcess : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{mediaAssetId:guid}/processing", async Task<EndpointResult<GetVideoProcessDto?>> (
            Guid mediaAssetId,
            [FromServices] GetVideoProcessHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(mediaAssetId, cancellationToken));
    }
}

public sealed class GetVideoProcessHandler
{
    private readonly IReadDbContext _readDbContext;

    public GetVideoProcessHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public async Task<Result<GetVideoProcessDto?, Error>> Handle(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        VideoProcess? process = await _readDbContext.VideoProcessesQuery
            .Include(item => item.Steps)
            .SingleOrDefaultAsync(
                item => item.VideoAssetId == mediaAssetId && item.VideoAsset.Status != MediaStatus.DELETED,
                cancellationToken);
        if (process is null)
            return Result.Success<GetVideoProcessDto?, Error>(null);

        return new GetVideoProcessDto(
            process.Id,
            process.VideoAssetId,
            process.Status.ToString().ToLowerInvariant(),
            process.Progress,
            process.ErrorMessage,
            process.CreatedAt,
            process.CompletedAt,
            process.Steps.OrderBy(step => step.Order).Select(step => new ProcessingStepDto(
                step.Id,
                step.AttemptId,
                step.Type.ToString().ToLowerInvariant(),
                step.Status.ToString().ToLowerInvariant(),
                step.Order,
                step.Weight,
                step.ResultData,
                step.ErrorMessage,
                step.StartedAt,
                step.CompletedAt,
                step.RetryCount,
                step.NextRetryAt,
                step.IsCriticalFailure)).ToList());
    }
}
