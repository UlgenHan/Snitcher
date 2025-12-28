namespace Snitcher.Core.Entities;

/// <summary>
/// Represents a namespace within a project.
/// Namespaces provide hierarchical organization for code elements.
/// </summary>
public class ProjectNamespace : BaseEntity
{
    /// <summary>
    /// Gets or sets the full name of the namespace.
    /// Includes the complete namespace hierarchy (e.g., "Snitcher.Core.Entities").
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the simple name of the namespace.
    /// The final segment of the full name (e.g., "Entities").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the foreign key reference to the parent project.
    /// Required for establishing the relationship with the project.
    /// </summary>
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Gets or sets the parent project.
    /// Navigation property for many-to-one relationship.
    /// </summary>
    public virtual Project Project { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the foreign key reference to the parent namespace.
    /// Optional for nested namespaces.
    /// </summary>
    public Guid? ParentNamespaceId { get; set; }
    
    /// <summary>
    /// Gets or sets the parent namespace.
    /// Navigation property for self-referencing hierarchy.
    /// </summary>
    public virtual ProjectNamespace? ParentNamespace { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of child namespaces.
    /// Navigation property for one-to-many relationship.
    /// </summary>
    public virtual ICollection<ProjectNamespace> ChildNamespaces { get; set; } = new List<ProjectNamespace>();
    
    /// <summary>
    /// Gets or sets the depth of the namespace in the hierarchy.
    /// Root namespaces have depth 0.
    /// </summary>
    public int Depth { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the namespace was last analyzed.
    /// Used for tracking analysis history.
    /// </summary>
    public DateTime? LastAnalyzedAt { get; set; }
    
    /// <summary>
    /// Updates the last analyzed timestamp to the current time.
    /// </summary>
    public void UpdateLastAnalyzed()
    {
        LastAnalyzedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
    
    /// <summary>
    /// Validates the namespace entity.
    /// </summary>
    /// <returns>True if the namespace is valid, otherwise false</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FullName) && 
               !string.IsNullOrWhiteSpace(Name) && 
               ProjectId != Guid.Empty;
    }
    
    /// <summary>
    /// Determines if this namespace is a root namespace.
    /// </summary>
    /// <returns>True if this is a root namespace, otherwise false</returns>
    public bool IsRootNamespace()
    {
        return ParentNamespaceId == null;
    }
}
