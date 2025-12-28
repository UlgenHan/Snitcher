using Snitcher.Core.Enums;

namespace Snitcher.Service.DTOs;

/// <summary>
/// Data Transfer Object for metadata information.
/// Used for read operations and API responses.
/// </summary>
public class MetadataDto
{
    /// <summary>
    /// Gets or sets the metadata identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the metadata value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the metadata scope.
    /// </summary>
    public MetadataScope Scope { get; set; }
    
    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public string ValueType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the related entity identifier.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Gets or sets the related entity type.
    /// </summary>
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the metadata is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata category.
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the metadata was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the metadata was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the metadata is entity-scoped.
    /// </summary>
    public bool IsEntityScoped => RelatedEntityId.HasValue && !string.IsNullOrWhiteSpace(RelatedEntityType);
    
    /// <summary>
    /// Gets or sets a value indicating whether the metadata is global.
    /// </summary>
    public bool IsGlobal => Scope == MetadataScope.Global && !IsEntityScoped;
}

/// <summary>
/// Data Transfer Object for creating a new metadata entry.
/// Used for write operations and API requests.
/// </summary>
public class CreateMetadataDto
{
    /// <summary>
    /// Gets or sets the metadata key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the metadata value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the metadata scope.
    /// </summary>
    public MetadataScope Scope { get; set; }
    
    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public string ValueType { get; set; } = "string";
    
    /// <summary>
    /// Gets or sets the related entity identifier.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Gets or sets the related entity type.
    /// </summary>
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the metadata is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata category.
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Data Transfer Object for updating an existing metadata entry.
/// Used for write operations and API requests.
/// </summary>
public class UpdateMetadataDto
{
    /// <summary>
    /// Gets or sets the metadata value.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public string? ValueType { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata category.
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Data Transfer Object for typed metadata operations.
/// </summary>
/// <typeparam name="T">The value type</typeparam>
public class TypedMetadataDto<T>
{
    /// <summary>
    /// Gets or sets the metadata key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the typed metadata value.
    /// </summary>
    public T? Value { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata scope.
    /// </summary>
    public MetadataScope Scope { get; set; }
    
    /// <summary>
    /// Gets or sets the related entity identifier.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }
    
    /// <summary>
    /// Gets or sets the related entity type.
    /// </summary>
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata category.
    /// </summary>
    public string? Category { get; set; }
}
