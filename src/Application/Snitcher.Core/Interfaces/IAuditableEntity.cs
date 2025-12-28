namespace Snitcher.Core.Interfaces;

/// <summary>
/// Interface for entities that support audit trails.
/// Provides tracking for creation and modification timestamps.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets the timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Gets the timestamp when the entity was last updated.
    /// </summary>
    DateTime UpdatedAt { get; }
}

/// <summary>
/// Interface for entities that support soft deletion.
/// Allows entities to be marked as deleted without physically removing them.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }
}
