using EducationContentService.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace EducationContentService.Core.Endpoints;

public static class ErrorTypeExtensions
{
    public static int ToHttpStatusCode(this ErrorType errorType) =>
        errorType switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            ErrorType.FAILURE => StatusCodes.Status500InternalServerError,
            ErrorType.AUTHENTICATION => StatusCodes.Status401Unauthorized,
            ErrorType.AUTHORIZATION => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
}