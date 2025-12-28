namespace Snitcher.Core.Enums;

/// <summary>
/// Defines the scope of metadata entries.
/// Determines how metadata values are categorized and accessed.
/// </summary>
public enum MetadataScope
{
    /// <summary>
    /// Global metadata applicable to the entire application.
    /// Used for application-wide settings and configuration.
    /// </summary>
    Global = 0,
    
    /// <summary>
    /// Project-specific metadata.
    /// Applied to a specific project and its operations.
    /// </summary>
    Project = 1,
    
    /// <summary>
    /// Namespace-specific metadata.
    /// Applied to a specific namespace within a project.
    /// </summary>
    Namespace = 2,
    
    /// <summary>
    /// User-specific metadata.
    /// Applied to user preferences and settings.
    /// </summary>
    User = 3,
    
    /// <summary>
    /// Session-specific metadata.
    /// Temporary metadata that exists only during the current session.
    /// </summary>
    Session = 4
}
