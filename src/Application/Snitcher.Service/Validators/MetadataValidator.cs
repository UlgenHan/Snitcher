using Snitcher.Core.Enums;
using Snitcher.Service.DTOs;

namespace Snitcher.Service.Validators;

/// <summary>
/// Validator for metadata-related operations.
/// Provides validation rules and error messages for metadata data.
/// </summary>
public static class MetadataValidator
{
    /// <summary>
    /// Validates a metadata creation DTO.
    /// </summary>
    /// <param name="dto">The metadata creation DTO</param>
    /// <returns>Validation result with success status and error messages</returns>
    public static ValidationResult ValidateCreateMetadata(CreateMetadataDto dto)
    {
        var result = new ValidationResult();

        if (dto == null)
        {
            result.AddError("Metadata data cannot be null.");
            return result;
        }

        // Validate Key
        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            result.AddError("Metadata key is required.");
        }
        else if (dto.Key.Length > 200)
        {
            result.AddError("Metadata key cannot exceed 200 characters.");
        }
        else if (!IsValidMetadataKey(dto.Key))
        {
            result.AddError("Metadata key contains invalid characters.");
        }

        // Validate Value
        if (string.IsNullOrWhiteSpace(dto.Value))
        {
            result.AddError("Metadata value is required.");
        }
        else if (dto.Value.Length > 2000)
        {
            result.AddError("Metadata value cannot exceed 2000 characters.");
        }

        // Validate Scope
        if (!Enum.IsDefined(typeof(MetadataScope), dto.Scope))
        {
            result.AddError("Invalid metadata scope.");
        }

        // Validate ValueType
        if (string.IsNullOrWhiteSpace(dto.ValueType))
        {
            result.AddError("Value type is required.");
        }
        else if (dto.ValueType.Length > 100)
        {
            result.AddError("Value type cannot exceed 100 characters.");
        }
        else if (!IsValidValueType(dto.ValueType))
        {
            result.AddError("Invalid value type.");
        }

        // Validate RelatedEntityType (if provided)
        if (!string.IsNullOrWhiteSpace(dto.RelatedEntityType))
        {
            if (dto.RelatedEntityType.Length > 100)
            {
                result.AddError("Related entity type cannot exceed 100 characters.");
            }
        }

        // Validate Description (if provided)
        if (dto.Description != null && dto.Description.Length > 500)
        {
            result.AddError("Metadata description cannot exceed 500 characters.");
        }

        // Validate Category (if provided)
        if (dto.Category != null && dto.Category.Length > 100)
        {
            result.AddError("Metadata category cannot exceed 100 characters.");
        }

        // Validate entity relationship consistency
        if (dto.RelatedEntityId.HasValue && string.IsNullOrWhiteSpace(dto.RelatedEntityType))
        {
            result.AddError("Related entity type is required when related entity ID is provided.");
        }

        if (!dto.RelatedEntityId.HasValue && !string.IsNullOrWhiteSpace(dto.RelatedEntityType))
        {
            result.AddError("Related entity ID is required when related entity type is provided.");
        }

        // Validate global scope constraints
        if (dto.Scope == MetadataScope.Global && dto.RelatedEntityId.HasValue)
        {
            result.AddError("Global metadata cannot be associated with a specific entity.");
        }

        return result;
    }

    /// <summary>
    /// Validates a metadata update DTO.
    /// </summary>
    /// <param name="dto">The metadata update DTO</param>
    /// <returns>Validation result with success status and error messages</returns>
    public static ValidationResult ValidateUpdateMetadata(UpdateMetadataDto dto)
    {
        var result = new ValidationResult();

        if (dto == null)
        {
            result.AddError("Metadata data cannot be null.");
            return result;
        }

        // Validate Value (if provided)
        if (dto.Value != null && dto.Value.Length > 2000)
        {
            result.AddError("Metadata value cannot exceed 2000 characters.");
        }

        // Validate ValueType (if provided)
        if (dto.ValueType != null)
        {
            if (string.IsNullOrWhiteSpace(dto.ValueType))
            {
                result.AddError("Value type cannot be empty.");
            }
            else if (dto.ValueType.Length > 100)
            {
                result.AddError("Value type cannot exceed 100 characters.");
            }
            else if (!IsValidValueType(dto.ValueType))
            {
                result.AddError("Invalid value type.");
            }
        }

        // Validate Description (if provided)
        if (dto.Description != null && dto.Description.Length > 500)
        {
            result.AddError("Metadata description cannot exceed 500 characters.");
        }

        // Validate Category (if provided)
        if (dto.Category != null && dto.Category.Length > 100)
        {
            result.AddError("Metadata category cannot exceed 100 characters.");
        }

        return result;
    }

    /// <summary>
    /// Checks if a metadata key is valid.
    /// </summary>
    /// <param name="key">The metadata key</param>
    /// <returns>True if the key is valid, otherwise false</returns>
    private static bool IsValidMetadataKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        // Metadata keys should follow a consistent pattern
        // Allow alphanumeric characters, dots, underscores, and hyphens
        var pattern = @"^[a-zA-Z0-9._-]+$";
        return System.Text.RegularExpressions.Regex.IsMatch(key, pattern);
    }

    /// <summary>
    /// Checks if a value type is valid.
    /// </summary>
    /// <param name="valueType">The value type</param>
    /// <returns>True if the value type is valid, otherwise false</returns>
    private static bool IsValidValueType(string valueType)
    {
        if (string.IsNullOrWhiteSpace(valueType))
            return false;

        // Common value types that are supported
        var validTypes = new[]
        {
            "string", "int", "integer", "long", "float", "double", "decimal",
            "bool", "boolean", "datetime", "date", "time", "guid",
            "json", "xml", "base64", "url", "email", "phone"
        };

        return validTypes.Contains(valueType.ToLowerInvariant());
    }

    /// <summary>
    /// Validates metadata scope and entity relationship.
    /// </summary>
    /// <param name="scope">The metadata scope</param>
    /// <param name="entityId">The entity identifier</param>
    /// <param name="entityType">The entity type</param>
    /// <returns>Validation result with success status and error messages</returns>
    public static ValidationResult ValidateScopeAndEntity(MetadataScope scope, Guid? entityId, string? entityType)
    {
        var result = new ValidationResult();

        // Validate global scope constraints
        if (scope == MetadataScope.Global)
        {
            if (entityId.HasValue)
            {
                result.AddError("Global metadata cannot be associated with a specific entity.");
            }
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                result.AddError("Global metadata cannot specify an entity type.");
            }
        }
        else
        {
            // For non-global scopes, entity association is optional but must be consistent
            if (entityId.HasValue && string.IsNullOrWhiteSpace(entityType))
            {
                result.AddError("Entity type is required when entity ID is provided.");
            }
            if (!entityId.HasValue && !string.IsNullOrWhiteSpace(entityType))
            {
                result.AddError("Entity ID is required when entity type is provided.");
            }
        }

        return result;
    }
}
