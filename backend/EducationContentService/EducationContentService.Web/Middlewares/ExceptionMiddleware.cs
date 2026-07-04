using System.Security.Authentication;
using EducationContentService.Core.Endpoints;
using EducationContentService.Domain.Exceptions;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Web.Middlewares;

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
            _logger.LogError(ex, "Exception was thrown in education service");

            // Если ответ уже начал писаться, менять статус/заголовки нельзя — пробрасываем дальше.
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Error error = exception switch
        {
            NotFoundException ex => ex.Error,

            ValidationException ex => ex.Error,

            ConflictException ex => ex.Error,

            FailureException ex => ex.Error,

            AuthenticationException => Error.Authentication("authentication.failed", exception.Message),

            _ => Error.Failure("server.internal", exception.Message)
        };

        var envelope = Envelope.Fail(error);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = error.Type.ToHttpStatusCode();

        await context.Response.WriteAsJsonAsync(envelope);
    }
}