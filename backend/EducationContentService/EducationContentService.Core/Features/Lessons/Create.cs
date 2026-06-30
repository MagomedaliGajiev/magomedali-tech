using CSharpFunctionalExtensions;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using EducationContentService.Domain.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace EducationContentService.Core.Features.Lessons;

public record CreateLessonRequest(string Title, string Description);

public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons", async ([FromBody] CreateLessonRequest request, CreateHandler handler) =>
        {
           await handler.Handle(request);
        });
    }
}

public sealed partial class CreateHandler
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly ILessonsRepository _lessonsRepository;

    public CreateHandler(ILogger<CreateHandler> logger, ILessonsRepository lessonsRepository)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
    }

    public async Task<Result<Guid, Error>> Handle(CreateLessonRequest request)
    {
        Result<Title, Error> titleResult = Title.Create(request.Title);
        if (titleResult.IsFailure)
            return titleResult.Error;

        Result<Description, Error> descriptionResult = Description.Create(request.Description);
        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        var lesson = new Lesson(Guid.NewGuid(), titleResult.Value, descriptionResult.Value);

        Result<Guid, Error> result = await _lessonsRepository.AddAsync(lesson);

        if (result.IsFailure)
            return result.Error;

        LogLessonCreated(lesson.Id);

        return lesson.Id;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created lesson {Id}")]
    private partial void LogLessonCreated(Guid id);
}