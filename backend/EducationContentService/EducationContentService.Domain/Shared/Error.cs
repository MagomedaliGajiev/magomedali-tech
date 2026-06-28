namespace EducationContentService.Domain.Shared;

public record ErrorMessage(string Code, string Message, string? InvalidField);

public record Error
{
    public IReadOnlyList<ErrorMessage> Messages { get; } = [];

    public ErrorType Type { get; }

    public string? InvalidField { get; }

    private Error(IEnumerable<ErrorMessage> messages, ErrorType type)
    {
        Messages = messages.ToArray();
        Type = type;
    }

    public static Error Validation(params IEnumerable<ErrorMessage> messages) =>
        new(messages, ErrorType.VALIDATION);

    public static Error NotFound(params IEnumerable<ErrorMessage> messages) => new(messages, ErrorType.NOT_FOUND);

    public static Error Failure(params IEnumerable<ErrorMessage> messages) => new(messages, ErrorType.FAILURE);

    public static Error Conflict(params IEnumerable<ErrorMessage> messages) => new(messages, ErrorType.CONFLICT);

    public static Error Authentication(params IEnumerable<ErrorMessage> messages) => new(messages, ErrorType.AUTHENTICATION);

    public static Error Authorization(params IEnumerable<ErrorMessage> messages) => new(messages, ErrorType.AUTHORIZATION);
}

public enum ErrorType
{
    VALIDATION,
    NOT_FOUND,
    FAILURE,
    CONFLICT,
    AUTHENTICATION,
    AUTHORIZATION,
}