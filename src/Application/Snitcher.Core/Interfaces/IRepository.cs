namespace Snitcher.Core.Interfaces;

/// <summary>
/// Generic repository interface for data access operations.
/// Provides a contract for CRUD operations and common queries.
/// This abstraction allows for different storage implementations (EF Core, Dapper, etc.).
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The entity's identifier type</typeparam>
public interface IRepository<TEntity, TId> where TEntity : IEntity<TId>
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all entities</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entity</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entity was deleted, otherwise false</returns>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an entity with the specified identifier exists.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entity exists, otherwise false</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Simplified repository interface for entities with Guid identifiers.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IEntity
{
}
