using System.Security.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;
using Shared.SharedKernel.Exceptions;

namespace Framework.Middlewares;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception was thrown while processing the request");

            // Если ответ уже начал писаться, менять статус/заголовки нельзя — пробрасываем дальше.
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (int statusCode, Error error) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Error),

            ValidationException ex => (StatusCodes.Status400BadRequest, ex.Error),

            ConflictException ex => (StatusCodes.Status409Conflict, ex.Error),

            FailureException ex => (StatusCodes.Status500InternalServerError, ex.Error),

            AuthenticationException => (StatusCodes.Status401Unauthorized, Error.Authentication("authentication.failed", exception.Message)),

            _ => (StatusCodes.Status500InternalServerError, Error.Failure("server.internal", exception.Message))
        };

        var envelope = Envelope.Fail(error);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(envelope);
    }
}