namespace Snitcher.Core.Entities;

/// <summary>
/// Represents a project in the Snitcher application.
/// Projects are the top-level organizational unit for code analysis.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the project.
    /// Must be unique within the application.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the project.
    /// Optional field for project documentation.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the file system path to the project root.
    /// Used for locating project files and directories.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the version of the project.
    /// Optional field for project versioning.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the project was last analyzed.
    /// Used for tracking analysis history.
    /// </summary>
    public DateTime? LastAnalyzedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of namespaces belonging to this project.
    /// Navigation property for one-to-many relationship.
    /// </summary>
    public virtual ICollection<ProjectNamespace> Namespaces { get; set; } = new List<ProjectNamespace>();
    
    /// <summary>
    /// Updates the last analyzed timestamp to the current time.
    /// </summary>
    public void UpdateLastAnalyzed()
    {
        LastAnalyzedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
    
    /// <summary>
    /// Validates the project entity.
    /// </summary>
    /// <returns>True if the project is valid, otherwise false</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Path);
    }
}
