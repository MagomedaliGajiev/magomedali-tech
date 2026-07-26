using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record FileName
{
    public string Value => Name;

    public string Name { get; private set; } = string.Empty;

    public string Extension { get; private set; } = string.Empty;

    private FileName()
    {
    }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
    }

    public static Result<FileName, Error> Create(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GeneralErrors.ValueIsInvalid(nameof(fileName));

        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
            return GeneralErrors.ValueIsInvalid("File must have extension");

        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();
        return new FileName(fileName, extension);
    }
}