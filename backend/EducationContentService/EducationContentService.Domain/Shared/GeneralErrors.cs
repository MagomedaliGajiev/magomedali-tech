namespace EducationContentService.Domain.Shared;

public static class GeneralErrors
{
    public static Error ValueIsInvalid(string? name = null)
    {
        string label = name ?? "значение";
        return Error.Validation(new ErrorMessage("value.is.invalid", $"{label} недействительно", name));
    }

    public static Error NotFound(Guid? id = null, string? name = null)
    {
        string forId = id == null ? string.Empty : $" по Id '{id}'";
        return Error.NotFound(new ErrorMessage("record.not.found", $"{name ?? "запись"} не найдена{forId}", null));
    }

    public static Error ValueIsRequired(string? name = null)
    {
        string label = name ?? "значение";
        return Error.Validation(new ErrorMessage("value.is.required", $"{label} обязательно", name));
    }

    public static Error AlreadyExist()
    {
        return Error.Validation(new ErrorMessage("record.already.exist", "Запись уже существует", null));
    }

    public static Error Failure(string? message = null)
    {
        return Error.Failure(new ErrorMessage("server.failure", message ?? "Серверная ошибка", null));
    }
}