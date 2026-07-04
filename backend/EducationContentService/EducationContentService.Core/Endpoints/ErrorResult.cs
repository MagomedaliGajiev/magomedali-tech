using EducationContentService.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace EducationContentService.Core.Endpoints;

public sealed class ErrorResult : IResult
{
    private readonly Error _error;

    public ErrorResult(Error error)
    {
        _error = error;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = _error.Type.ToHttpStatusCode();

        var envelope = Envelope.Fail(_error);

        return httpContext.Response.WriteAsJsonAsync(envelope);
    }
}