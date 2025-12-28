namespace Snitcher.Service.DTOs;

/// <summary>
/// Data Transfer Object for project information.
/// Used for read operations and API responses.
/// </summary>
public class ProjectDto
{
    /// <summary>
    /// Gets or sets the project identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the project description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the project file system path.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the project version.
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the project was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the project was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the project was last analyzed.
    /// </summary>
    public DateTime? LastAnalyzedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the number of namespaces in the project.
    /// </summary>
    public int NamespaceCount { get; set; }
}

/// <summary>
/// Data Transfer Object for creating a new project.
/// Used for write operations and API requests.
/// </summary>
public class CreateProjectDto
{
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the project description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the project file system path.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the project version.
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// Data Transfer Object for updating an existing project.
/// Used for write operations and API requests.
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the project description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the project file system path.
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// Gets or sets the project version.
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// Data Transfer Object for project with namespaces included.
/// Used for detailed project information.
/// </summary>
public class ProjectWithNamespacesDto : ProjectDto
{
    /// <summary>
    /// Gets or sets the collection of namespaces in the project.
    /// </summary>
    public ICollection<ProjectNamespaceDto> Namespaces { get; set; } = new List<ProjectNamespaceDto>();
}

/// <summary>
/// Data Transfer Object for project namespace information.
/// </summary>
public class ProjectNamespaceDto
{
    /// <summary>
    /// Gets or sets the namespace identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the full namespace name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the simple namespace name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the parent project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    
    /// <summary>
    /// Gets or sets the parent namespace identifier.
    /// </summary>
    public Guid? ParentNamespaceId { get; set; }
    
    /// <summary>
    /// Gets or sets the depth of the namespace in the hierarchy.
    /// </summary>
    public int Depth { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the namespace was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the namespace was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the namespace was last analyzed.
    /// </summary>
    public DateTime? LastAnalyzedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the number of child namespaces.
    /// </summary>
    public int ChildNamespaceCount { get; set; }
}

/// <summary>
/// Data Transfer Object for project path validation result.
/// </summary>
public class ProjectPathValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the path is valid.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the path is already in use.
    /// </summary>
    public bool IsAlreadyInUse { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the project that uses this path (if already in use).
    /// </summary>
    public Guid? ExistingProjectId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the project that uses this path (if already in use).
    /// </summary>
    public string? ExistingProjectName { get; set; }
}
