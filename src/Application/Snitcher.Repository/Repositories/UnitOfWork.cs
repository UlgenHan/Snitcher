using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;

namespace Snitcher.Repository.Repositories;

/// <summary>
/// Unit of Work implementation for coordinating repository operations.
/// Manages transactions and provides access to all repositories.
/// Simplified architecture with only project repository.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SnitcherDbContext _context;
    private readonly IProjectRepository _projectRepository;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="projectRepository">The project repository</param>
    public UnitOfWork(
        SnitcherDbContext context,
        IProjectRepository projectRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    /// <summary>
    /// Gets the project repository.
    /// </summary>
    public IProjectRepository Projects => _projectRepository;

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction, saving all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _context.RollbackAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all pending changes to the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected entities</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the Unit of Work and underlying context.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
