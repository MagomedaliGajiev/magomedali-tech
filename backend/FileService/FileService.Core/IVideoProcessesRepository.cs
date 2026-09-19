using CSharpFunctionalExtensions;
using FileService.Domain.Processing;
using Shared.SharedKernel;

namespace FileService.Core;

public interface IVideoProcessesRepository
{
    Task<Result<VideoProcess, Error>> GetById(Guid id, CancellationToken cancellationToken);

    Task<UnitResult<Error>> SaveAsync(CancellationToken cancellationToken);
}
