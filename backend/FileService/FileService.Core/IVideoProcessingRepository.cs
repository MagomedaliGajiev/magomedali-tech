using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.Processing;
using Shared.SharedKernel;

namespace FileService.Core;

public interface IVideoProcessingRepository
{
    void Add(VideoProcess process);

    Task<Result<VideoProcess, Error>> GetBy(
        Expression<Func<VideoProcess, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<Result<VideoProcess, Error>> GetById(Guid id, CancellationToken cancellationToken);

    Task<UnitResult<Error>> SaveAsync(CancellationToken cancellationToken);
}
