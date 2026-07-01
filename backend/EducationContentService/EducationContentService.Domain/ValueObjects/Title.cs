using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Domain.ValueObjects;

public partial record Title
{
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    private Title(string value)
    {
        Value = value;
    }

    public static Result<Title, Error> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsInvalid("title");
        }

        string normalized = SpaceRemoveRegex().Replace(value.Trim(), " ");

        if (normalized.Length > MAX_LENGTH)
        {
            return GeneralErrors.ValueIsInvalid("title");
        }

        return new Title(normalized);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaceRemoveRegex();
}