using FileService.Domain;

namespace FileService.Core.Models;

public sealed record MediaUrl(
    StorageKey StorageKey,
    string PresignedUrl,
    DateTimeOffset ExpiresAtUtc);