using Snitcher.Core.Interfaces;

namespace Snitcher.Core.Entities;

/// <summary>
/// Base entity class providing common properties for all domain entities.
/// Implements standard audit fields and soft delete functionality.
/// This class serves as the foundation for all entities in the system.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// Uses GUID to ensure uniqueness across distributed systems.
    /// </summary>
    public Guid Id { get; protected set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// Automatically set by the infrastructure layer.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated.
    /// Automatically updated by the infrastructure layer.
    /// </summary>
    public DateTime UpdatedAt { get; protected set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// Allows for data recovery and audit trails.
    /// </summary>
    public bool IsDeleted { get; protected set; }
    
    /// <summary>
    /// Initializes a new instance of the BaseEntity class.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
    
    /// <summary>
    /// Marks the entity as deleted without physically removing it.
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdateTimestamp();
    }
    
    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        UpdateTimestamp();
    }
    
    /// <summary>
    /// Updates the entity's last modified timestamp.
    /// Called automatically by the infrastructure layer when the entity is modified.
    /// </summary>
    public virtual void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// Equality is based on the entity's identifier.
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns>True if the objects are equal, otherwise false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;
            
        if (ReferenceEquals(this, other))
            return true;
            
        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;
            
        return Id.Equals(other.Id);
    }
    
    /// <summary>
    /// Returns the hash code for this entity.
    /// </summary>
    /// <returns>The hash code based on the entity's identifier</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    /// <summary>
    /// Returns a string representation of the entity.
    /// </summary>
    /// <returns>String containing the entity type and identifier</returns>
    public override string ToString()
    {
        return $"{GetType().Name}[Id={Id}]";
    }
}
