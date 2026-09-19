using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Processing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public sealed class VideoProcessingRepository : IVideoProcessingRepository
{
    private readonly FileServiceDbContext _dbContext;

    public VideoProcessingRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(VideoProcess process) => _dbContext.VideoProcesses.Add(process);

    public Task<Result<VideoProcess, Error>> GetById(Guid id, CancellationToken cancellationToken) =>
        GetBy(process => process.Id == id, cancellationToken);

    public async Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        VideoProcess? process = await _dbContext.VideoProcesses
            .Include(item => item.VideoAsset)
            .Include(item => item.Steps)
            .SingleOrDefaultAsync(predicate, cancellationToken);

        if (process is null)
            return GeneralErrors.NotFound(null, "процесс обработки видео");

        return process;
    }

    public async Task<UnitResult<Error>> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return Error.Failure("video.processing.conflict", "Процесс уже изменён другим обработчиком. Загрузите актуальное состояние.");
        }
    }
}
