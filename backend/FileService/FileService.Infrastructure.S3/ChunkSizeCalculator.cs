using CSharpFunctionalExtensions;
using FileService.Core.FilesStorage;
using Microsoft.Extensions.Options;
using Shared.SharedKernel;

namespace FileService.Infrastructure.S3;

public class ChunkSizeCalculator : IChunkSizeCalculator
{
    private readonly S3Options _options;

    public ChunkSizeCalculator(IOptions<S3Options> options)
    {
        _options = options.Value;
    }

    public Result<(long ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize)
    {
        if (fileSize <= 0)
            return GeneralErrors.ValueIsInvalid(nameof(fileSize));

        if (_options.RecommendedChunkSizeBytes <= 0 || _options.MaxChunks <= 0)
            return GeneralErrors.ValueIsInvalid("настройки чанков");

        if (fileSize <= _options.RecommendedChunkSizeBytes)
            return (fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / _options.RecommendedChunkSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, _options.MaxChunks);

        long chunkSize = (fileSize + actualChunks - 1) / actualChunks;

        return (chunkSize, actualChunks);
    }
}