namespace Snitcher.Core.Interfaces;

/// <summary>
/// Base interface for all entities in the domain model.
/// Provides a contract for entities that have a unique identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// Non-generic version of IEntity for entities with Guid identifiers.
/// Most entities in this system use Guid as the primary key type.
/// </summary>
public interface IEntity : IEntity<Guid>
{
}
