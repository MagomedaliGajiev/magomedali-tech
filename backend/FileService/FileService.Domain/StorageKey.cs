using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record StorageKey
{
    public string Key { get; private set; } = string.Empty;

    public string Prefix { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public string Value => string.IsNullOrEmpty(Prefix) ? Key : $"{Prefix}/{Key}";

    public string FullPath => $"{Location}/{Value}";

    private StorageKey()
    {
    }

    private StorageKey(string location, string prefix, string key)
    {
        Location = location;
        Prefix = prefix;
        Key = key;
    }

    public static Result<StorageKey, Error> Create(string location, string? prefix, string key)
    {
        if (string.IsNullOrEmpty(location))
            return GeneralErrors.ValueIsInvalid("location");

        Result<string, Error> normalizedKeyResult = NormalizedSegment(key);
        if (normalizedKeyResult.IsFailure)
            return normalizedKeyResult.Error;

        Result<string, Error> normalizedPrefixResult = NormalizedPrefix(prefix);
        if (normalizedPrefixResult.IsFailure)
            return GeneralErrors.ValueIsInvalid("prefix");

        return new StorageKey(location.Trim(), normalizedPrefixResult.Value, normalizedKeyResult.Value);
    }

    private static Result<string, Error> NormalizedPrefix(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return string.Empty;

        string[] parts = prefix.Trim().Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> normalizedParts = [];
        foreach (string part in parts)
        {
            Result<string, Error> normalizedPart = NormalizedSegment(part);
            if (normalizedPart.IsFailure)
                return normalizedPart;

            normalizedParts.Add(normalizedPart.Value);
        }

        return string.Join("/", normalizedParts);
    }

    private static Result<string, Error> NormalizedSegment(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return GeneralErrors.ValueIsInvalid("key");

        string trimmed = value.Trim();

        if (trimmed.Contains('/', StringComparison.Ordinal) || trimmed.Contains('\\', StringComparison.Ordinal))
            return GeneralErrors.ValueIsInvalid("key");

        return trimmed;
    }
}