using CSharpFunctionalExtensions;
using EducationContentService.Core.Endpoints;
using EducationContentService.Core.Validation;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using EducationContentService.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace EducationContentService.Core.Features.Lessons;

public record CreateLessonRequest(string Title, string Description);

public class CreateLessonsRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonsRequestValidator()
    {
        RuleFor(r => r.Title)
            .MustBeValueObject(Title.Create);

        RuleFor(r => r.Description)
            .MustBeValueObject(Description.Create);
    }
}

public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons", async Task<EndpointResult<Guid>> (
            [FromBody] CreateLessonRequest request,
            [FromServices] CreateHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class CreateHandler
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly ILessonsRepository _lessonsRepository;
    private readonly IValidator<CreateLessonRequest> _validator;

    public CreateHandler(
        ILogger<CreateHandler> logger,
        ILessonsRepository lessonsRepository,
        IValidator<CreateLessonRequest> validator)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
        _validator = validator;
    }

    public async Task<Result<Guid, Error>> Handle(CreateLessonRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Title titleResult = Title.Create(request.Title).Value;
        Description descriptionResult = Description.Create(request.Description).Value;

        var lesson = new Lesson(Guid.NewGuid(), titleResult, descriptionResult);

        Result<Guid, Error> result = await _lessonsRepository.AddAsync(lesson, cancellationToken);

        if (result.IsFailure)
            return result.Error;

        _logger.LogInformation("Created lesson {Id}", lesson.Id);

        return lesson.Id;
    }
}