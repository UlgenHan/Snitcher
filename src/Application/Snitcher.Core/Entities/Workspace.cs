namespace Snitcher.Core.Entities;

/// <summary>
/// Represents a workspace in the Snitcher application.
/// Workspaces are the top-level organizational unit that contains projects.
/// </summary>
public class Workspace : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the workspace.
    /// Must be unique within the application.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the workspace.
    /// Optional field for workspace documentation.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the file system path to the workspace root.
    /// Used for locating workspace files and directories.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the version of the workspace.
    /// Optional field for workspace versioning.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets whether this is the default workspace.
    /// The application always has one default workspace.
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the collection of projects belonging to this workspace.
    /// Navigation property for one-to-many relationship.
    /// </summary>
    public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
    
    /// <summary>
    /// Validates the workspace entity.
    /// </summary>
    /// <returns>True if the workspace is valid, otherwise false</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(Path);
    }
}
