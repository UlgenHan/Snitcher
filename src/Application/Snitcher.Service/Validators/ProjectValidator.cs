using Snitcher.Service.DTOs;

namespace Snitcher.Service.Validators;

/// <summary>
/// Validator for project-related operations.
/// Provides validation rules and error messages for project data.
/// </summary>
public static class ProjectValidator
{
    /// <summary>
    /// Validates a project creation DTO.
    /// </summary>
    /// <param name="dto">The project creation DTO</param>
    /// <returns>Validation result with success status and error messages</returns>
    public static ValidationResult ValidateCreateProject(CreateProjectDto dto)
    {
        var result = new ValidationResult();

        if (dto == null)
        {
            result.AddError("Project data cannot be null.");
            return result;
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.AddError("Project name is required.");
        }
        else if (dto.Name.Length > 200)
        {
            result.AddError("Project name cannot exceed 200 characters.");
        }
        else if (!IsValidProjectName(dto.Name))
        {
            result.AddError("Project name contains invalid characters.");
        }

        // Validate Path
        if (string.IsNullOrWhiteSpace(dto.Path))
        {
            result.AddError("Project path is required.");
        }
        else if (dto.Path.Length > 500)
        {
            result.AddError("Project path cannot exceed 500 characters.");
        }
        else if (!IsValidPath(dto.Path))
        {
            result.AddError("Project path is not a valid file system path.");
        }

        // Validate Description
        if (dto.Description != null && dto.Description.Length > 1000)
        {
            result.AddError("Project description cannot exceed 1000 characters.");
        }

        // Validate Version
        if (dto.Version != null && dto.Version.Length > 50)
        {
            result.AddError("Project version cannot exceed 50 characters.");
        }

        return result;
    }

    /// <summary>
    /// Validates a project update DTO.
    /// </summary>
    /// <param name="dto">The project update DTO</param>
    /// <returns>Validation result with success status and error messages</returns>
    public static ValidationResult ValidateUpdateProject(UpdateProjectDto dto)
    {
        var result = new ValidationResult();

        if (dto == null)
        {
            result.AddError("Project data cannot be null.");
            return result;
        }

        // Validate Name (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            if (dto.Name.Length > 200)
            {
                result.AddError("Project name cannot exceed 200 characters.");
            }
            else if (!IsValidProjectName(dto.Name))
            {
                result.AddError("Project name contains invalid characters.");
            }
        }

        // Validate Path (if provided)
        if (!string.IsNullOrWhiteSpace(dto.Path))
        {
            if (dto.Path.Length > 500)
            {
                result.AddError("Project path cannot exceed 500 characters.");
            }
            else if (!IsValidPath(dto.Path))
            {
                result.AddError("Project path is not a valid file system path.");
            }
        }

        // Validate Description (if provided)
        if (dto.Description != null && dto.Description.Length > 1000)
        {
            result.AddError("Project description cannot exceed 1000 characters.");
        }

        // Validate Version (if provided)
        if (dto.Version != null && dto.Version.Length > 50)
        {
            result.AddError("Project version cannot exceed 50 characters.");
        }

        return result;
    }

    /// <summary>
    /// Checks if a project name is valid.
    /// </summary>
    /// <param name="name">The project name</param>
    /// <returns>True if the name is valid, otherwise false</returns>
    private static bool IsValidProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Project names should not contain invalid characters
        var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', '/', '\\' };
        return !name.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Checks if a path is valid.
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <returns>True if the path is valid, otherwise false</returns>
    private static bool IsValidPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return !string.IsNullOrWhiteSpace(fullPath);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public List<string> Errors { get; } = new List<string>();

    /// <summary>
    /// Adds an error message to the validation result.
    /// </summary>
    /// <param name="error">The error message</param>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Adds multiple error messages to the validation result.
    /// </summary>
    /// <param name="errors">The error messages</param>
    public void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            AddError(error);
        }
    }
}
