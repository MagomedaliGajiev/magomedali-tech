namespace FileService.Contracts.HttpCommunication;

public sealed record FileServiceOptions
{
    public string Url { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 7;
}
