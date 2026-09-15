using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.Processing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public sealed class VideoProcessesRepository : IVideoProcessesRepository
{
    private readonly FileServiceDbContext _dbContext;

    public VideoProcessesRepository(FileServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<VideoProcess, Error>> GetById(Guid id, CancellationToken cancellationToken)
    {
        VideoProcess? process = await _dbContext.VideoProcesses
            .Include(item => item.VideoAsset)
            .Include(item => item.Steps)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (process is null)
            return GeneralErrors.NotFound(id, "процесс обработки видео");

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
