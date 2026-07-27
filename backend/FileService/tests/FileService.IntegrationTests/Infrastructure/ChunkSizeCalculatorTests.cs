using FileService.Infrastructure.S3;
using Microsoft.Extensions.Options;

namespace FileService.IntegrationTests.Infrastructure;

public class ChunkSizeCalculatorTests
{
    [Fact]
    public void CalculateChunkSize_WhenChunkExceedsIntRange_ReturnsLongChunkSize()
    {
        var options = Options.Create(new S3Options
        {
            RecommendedChunkSizeBytes = 100 * 1024 * 1024,
            MaxChunks = 100,
        });
        var calculator = new ChunkSizeCalculator(options);
        long fileSize = ((long)int.MaxValue * options.Value.MaxChunks) + 1;

        var result = calculator.CalculateChunkSize(fileSize);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.ChunkSize > int.MaxValue);
        Assert.InRange(result.Value.TotalChunks, 1, options.Value.MaxChunks);
    }

    [Fact]
    public void CalculateChunkSize_WhenFileRequiresMultipleChunks_UsesRecommendedChunkSize()
    {
        const long recommendedChunkSize = 5 * 1024 * 1024;
        var options = Options.Create(new S3Options
        {
            RecommendedChunkSizeBytes = recommendedChunkSize,
            MaxChunks = 100,
        });
        var calculator = new ChunkSizeCalculator(options);

        var result = calculator.CalculateChunkSize(recommendedChunkSize + 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(recommendedChunkSize, result.Value.ChunkSize);
        Assert.Equal(2, result.Value.TotalChunks);
    }
}