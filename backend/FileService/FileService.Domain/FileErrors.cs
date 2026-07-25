using Shared.SharedKernel;

namespace FileService.Domain;

public static class FileErrors
{
    public static Error BucketNotFound()
    {
        return Error.NotFound("no.such.bucket", $"Бакет не найден");
    }

    public static Error UploadNotFound()
    {
        return Error.NotFound(
            "upload.not.found",
            $"Сессия загрузки не найдена");
    }

    public static Error ObjectNotFound(string? objectKey = null)
    {
        string key = objectKey ?? string.Empty;
        return Error.NotFound(
            "upload.not.found",
            $"Сессия загрузки {key}не найдена");
    }

    public static Error Forbidden()
    {
        return Error.Failure("access.denied", "Недостаточно прав для выполнения операции");
    }

    public static Error ValidationFailed()
    {
        string message = "Запрос содержит некорректные данные";

        return Error.Validation("validation.failed", message);
    }

    public static Error InternalServerError()
    {
        return Error.Failure("internal.server.error", "Внутренняя ошибка хранилища");
    }

    public static Error OperationCanceled()
    {
        return Error.Failure("operation.canceled", "Операция была отменена");
    }

    public static Error NetworkIssue()
    {
        return Error.Failure(
            "network.issue",
            "Сетевая ошибка при взаимодействии с файловым хранилищем");
    }

    public static Error Unknown()
    {
        return Error.Failure("unknown.error", "Произошла неизвестная ошибка");
    }
}