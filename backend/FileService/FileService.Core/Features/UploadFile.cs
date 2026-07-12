using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FileService.Core.Features;

public sealed class UploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/files", async Task<IResult> (
            IFormFile formFile,
            [FromForm] string bucket,
            [FromServices] IS3Provider s3Provider,
            CancellationToken cancellationToken) =>
        {
            string key = $"raw/{Guid.NewGuid()}";

            await s3Provider.UploadFileAsync(formFile.OpenReadStream(), bucket, key, formFile.ContentType, cancellationToken);

            return Results.Ok(key);
        })
        .DisableAntiforgery();
    }
}

public sealed class GetDownloadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/url", async Task<IResult> (
                string bucket,
                string key,
                [FromServices] IS3Provider s3Provider,
                CancellationToken cancellationToken) =>
            {
                string result = await s3Provider.GenerateDownloadUrlAsync(bucket, key);

                return Results.Ok(result);
            })
            .DisableAntiforgery();
    }
}