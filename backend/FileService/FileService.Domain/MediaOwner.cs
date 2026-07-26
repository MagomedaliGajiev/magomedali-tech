using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record MediaOwner
{
    private static readonly HashSet<string> AllowedContext =
    [
        "lesson",
        "module",
        "user",
    ];
    public string Context { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    private MediaOwner()
    {
    }

    private MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

    public static Result<MediaOwner, Error> Create(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context) || context.Length > 50)
            return GeneralErrors.ValueIsInvalid(nameof(context));

        string normalizedContext = context.Trim().ToLowerInvariant();
        if (!AllowedContext.Contains(normalizedContext))
            return GeneralErrors.ValueIsInvalid(nameof(context));

        if (entityId == Guid.Empty)
            return GeneralErrors.ValueIsInvalid(nameof(entityId));

        return new MediaOwner(normalizedContext, entityId);
    }

    public static Result<MediaOwner, Error> ForLesson(Guid lessonId) => Create("lesson", lessonId);

    public static Result<MediaOwner, Error> ForModule(Guid courseId) => Create("module", courseId);

    public static Result<MediaOwner, Error> ForUser(Guid userId) => Create("user", userId);
}