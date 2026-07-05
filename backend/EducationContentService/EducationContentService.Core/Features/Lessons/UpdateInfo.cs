using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Endpoints;
using EducationContentService.Core.Validation;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;
using EducationContentService.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace EducationContentService.Core.Features.Lessons;

public class UpdateLessonInfoRequestValidator : AbstractValidator<UpdateLessonInfoRequest>
{
    public UpdateLessonInfoRequestValidator()
    {
        RuleFor(r => r.Title)
            .MustBeValueObject(Title.Create);

        RuleFor(r => r.Description)
            .MustBeValueObject(Description.Create);
    }
}

public sealed class UpdateInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/lessons/{lessonId:guid}", async Task<EndpointResult<Guid>> (
            [FromRoute] Guid lessonId,
            [FromBody] UpdateLessonInfoRequest infoRequest,
            [FromServices] UpdateInfoHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(lessonId, infoRequest, cancellationToken));
    }
}

public sealed class UpdateInfoHandler
{
    private readonly ILogger<UpdateInfoHandler> _logger;
    private readonly ILessonsRepository _lessonsRepository;
    private readonly IValidator<UpdateLessonInfoRequest> _validator;

    public UpdateInfoHandler(
        ILogger<UpdateInfoHandler> logger,
        ILessonsRepository lessonsRepository,
        IValidator<UpdateLessonInfoRequest> validator)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
        _validator = validator;
    }

    public async Task<Result<Guid, Error>> Handle(
        Guid lessonId,
        UpdateLessonInfoRequest request,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Result<Lesson, Error> lessonResult = await _lessonsRepository.GetBy(l => l.Id == lessonId, cancellationToken);
        if (lessonResult.IsFailure)
            return lessonResult.Error;

        Title title = Title.Create(request.Title).Value;
        Description description = Description.Create(request.Description).Value;

        lessonResult.Value.UpdateInfo(title, description);

        Result<Guid, Error> updateResult = await _lessonsRepository.UpdateAsync(lessonResult.Value, cancellationToken);
        if (updateResult.IsFailure)
            return updateResult.Error;

        _logger.LogInformation("Updated info lesson {Id}", lessonResult.Value.Id);

        return lessonResult.Value.Id;
    }
}