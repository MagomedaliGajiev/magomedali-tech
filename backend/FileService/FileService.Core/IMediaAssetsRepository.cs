using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.Assets;
using Shared.SharedKernel;

namespace FileService.Core;

public interface IMediaAssetsRepository
{
    Task<Result<Guid, Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(CancellationToken cancellationToken = default);
}