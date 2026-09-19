using System.Data;
using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.Core;

public interface ITransactionManager
{
    Task<Result<int, Error>> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
