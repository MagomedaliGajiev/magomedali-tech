using System.Data;
using CSharpFunctionalExtensions;
using FileService.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace FileService.Infrastructure.Postgres;

public class TransactionManager : ITransactionManager
{
    private readonly FileServiceDbContext _dbContext;

    private readonly ILogger<TransactionManager> _logger;

    public TransactionManager(FileServiceDbContext dbContext, ILogger<TransactionManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        return new ManagedTransaction(transaction);
    }

    public async Task<Result<int, Error>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _dbContext.ChangeTracker.Clear();
            _logger.LogError(ex, "Concurrency conflict during save");
            return GeneralErrors.ConcurrencyConflict();
        }
        catch (OperationCanceledException ex)
        {
            _dbContext.ChangeTracker.Clear();
            _logger.LogInformation(ex, "Operation cancelled during save");
            return GeneralErrors.OperationCancelled();
        }
        catch (Exception ex)
        {
            _dbContext.ChangeTracker.Clear();
            _logger.LogError(ex, "Unexpected error during save");
            return GeneralErrors.DatabaseError();
        }
    }

    private sealed class ManagedTransaction : IDbTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public ManagedTransaction(IDbContextTransaction transaction) => _transaction = transaction;

        public IDbConnection? Connection => _transaction.GetDbTransaction().Connection;

        public IsolationLevel IsolationLevel => _transaction.GetDbTransaction().IsolationLevel;

        public void Commit() => _transaction.Commit();

        public void Rollback() => _transaction.Rollback();

        public void Dispose() => _transaction.Dispose();
    }
}
