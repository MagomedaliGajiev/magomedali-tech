using EducationContentService.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace EducationContentService.Core.Extensions;

public static class ResponseExtensions
{
    public static IResult ToResponse(this Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.VALIDATION => StatusCodes.Status400BadRequest,
            ErrorType.NOT_FOUND => StatusCodes.Status404NotFound,
            ErrorType.CONFLICT => StatusCodes.Status409Conflict,
            ErrorType.AUTHENTICATION => StatusCodes.Status401Unauthorized,
            ErrorType.AUTHORIZATION => StatusCodes.Status403Forbidden,
            ErrorType.FAILURE => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(error, statusCode: statusCode);
    }
}