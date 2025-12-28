namespace Snitcher.Core.Interfaces;

/// <summary>
/// Unit of Work interface for managing transactions and repository coordination.
/// Ensures that multiple repository operations are executed within a single transaction.
/// This pattern maintains data consistency across complex business operations.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current transaction, saving all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected entities</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
