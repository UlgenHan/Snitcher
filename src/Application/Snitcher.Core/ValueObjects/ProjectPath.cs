namespace Snitcher.Core.ValueObjects;

/// <summary>
/// Value object representing a project file system path.
/// Provides validation and normalization for project paths.
/// </summary>
public class ProjectPath
{
    /// <summary>
    /// Gets the normalized absolute path.
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Gets the directory name of the path.
    /// </summary>
    public string DirectoryName => Path.GetDirectoryName(Value) ?? string.Empty;
    
    /// <summary>
    /// Gets the file name of the path.
    /// </summary>
    public string FileName => Path.GetFileName(Value);
    
    /// <summary>
    /// Initializes a new instance of the ProjectPath class.
    /// </summary>
    /// <param name="path">The file system path</param>
    /// <exception cref="ArgumentException">Thrown when the path is invalid</exception>
    public ProjectPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            
        var normalizedPath = Path.GetFullPath(path);
        
        if (!Directory.Exists(normalizedPath))
            throw new DirectoryNotFoundException($"Directory not found: {normalizedPath}");
            
        Value = normalizedPath;
    }
    
    /// <summary>
    /// Determines whether the specified path is equal to the current path.
    /// </summary>
    /// <param name="obj">The object to compare with the current path</param>
    /// <returns>True if the objects are equal, otherwise false</returns>
    public override bool Equals(object? obj)
    {
        return obj is ProjectPath other && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Returns the hash code for this path.
    /// </summary>
    /// <returns>The hash code based on the normalized path</returns>
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }
    
    /// <summary>
    /// Returns the string representation of the path.
    /// </summary>
    /// <returns>The normalized path</returns>
    public override string ToString()
    {
        return Value;
    }
    
    /// <summary>
    /// Implicit conversion from string to ProjectPath.
    /// </summary>
    /// <param name="path">The string path</param>
    /// <returns>A new ProjectPath instance</returns>
    public static implicit operator ProjectPath(string path) => new ProjectPath(path);
    
    /// <summary>
    /// Implicit conversion from ProjectPath to string.
    /// </summary>
    /// <param name="path">The ProjectPath instance</param>
    /// <returns>The string representation of the path</returns>
    public static implicit operator string(ProjectPath path) => path.Value;
    
    /// <summary>
    /// Determines whether two ProjectPath instances are equal.
    /// </summary>
    /// <param name="left">The first path</param>
    /// <param name="right">The second path</param>
    /// <returns>True if the paths are equal, otherwise false</returns>
    public static bool operator ==(ProjectPath? left, ProjectPath? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;
            
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;
            
        return left.Equals(right);
    }
    
    /// <summary>
    /// Determines whether two ProjectPath instances are not equal.
    /// </summary>
    /// <param name="left">The first path</param>
    /// <param name="right">The second path</param>
    /// <returns>True if the paths are not equal, otherwise false</returns>
    public static bool operator !=(ProjectPath? left, ProjectPath? right)
    {
        return !(left == right);
    }
}
