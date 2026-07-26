using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Domain;

public sealed record MediaData
{
    public FileName FileName { get; private set; } = null!;

    public ContentType ContentType { get; private set; } = null!;

    public long Size { get; private set; }

    public int ExpectedChunksCount { get; private set; }

    private MediaData()
    {
    }

    private MediaData(FileName fileName, ContentType contentType, long size, int expectedChunksCount)
    {
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        ExpectedChunksCount = expectedChunksCount;
    }

    public static Result<MediaData, Error> Create(FileName fileName, ContentType contentType, long size, int expectedChunksCount)
    {
        if (size <= 0)
            return GeneralErrors.ValueIsInvalid("size");

        if (expectedChunksCount <= 0)
            return GeneralErrors.ValueIsInvalid("expectedChunksCount");

        return new MediaData(fileName, contentType, size, expectedChunksCount);
    }
}