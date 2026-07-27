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

        long minimumChunkSize = DivideRoundingUp(fileSize, _options.MaxChunks);
        long chunkSize = Math.Max(_options.RecommendedChunkSizeBytes, minimumChunkSize);
        long totalChunks = DivideRoundingUp(fileSize, chunkSize);

        return (chunkSize, checked((int)totalChunks));
    }

    private static long DivideRoundingUp(long dividend, long divisor)
    {
        return (dividend / divisor) + (dividend % divisor == 0 ? 0 : 1);
    }
}