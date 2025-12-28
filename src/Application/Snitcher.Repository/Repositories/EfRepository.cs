using Microsoft.EntityFrameworkCore;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;

namespace Snitcher.Repository.Repositories;

/// <summary>
/// Generic Entity Framework Core repository implementation.
/// Provides standard CRUD operations for any entity type.
/// This class serves as the base for all repository implementations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The entity's identifier type</typeparam>
public class EfRepository<TEntity, TId> : IRepository<TEntity, TId> 
    where TEntity : class, IEntity<TId>
    where TId : IEquatable<TId>
{
    protected readonly SnitcherDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the EfRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    public EfRepository(SnitcherDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Gets all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entity</returns>
    public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var entry = _dbSet.Update(entity);
        return Task.FromResult(entry.Entity);
    }

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entity was deleted, otherwise false</returns>
    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        return true;
    }

    /// <summary>
    /// Checks if an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entity exists, otherwise false</returns>
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Gets the count of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities</returns>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Finds entities based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds the first entity matching the predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity, or null if not found</returns>
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }
}

/// <summary>
/// Simplified generic repository for entities with Guid identifiers.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class EfRepository<TEntity> : EfRepository<TEntity, Guid>, IRepository<TEntity> 
    where TEntity : class, IEntity
{
    /// <summary>
    /// Initializes a new instance of the EfRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    public EfRepository(SnitcherDbContext context) : base(context)
    {
    }
}
