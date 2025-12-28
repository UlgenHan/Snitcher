using Snitcher.Core.Entities;

namespace Snitcher.Service.Interfaces;

/// <summary>
/// Service interface for ProjectEntity business operations.
/// Provides high-level project management functionality for the new database architecture.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="description">The project description</param>
    /// <param name="path">The project file path</param>
    /// <param name="version">The project version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created project</returns>
    Task<ProjectEntity> CreateProjectAsync(Guid workspaceId, string name, string description = "", string path = "", string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by its identifier.
    /// </summary>
    /// <param name="id">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    Task<ProjectEntity?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by name within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    Task<ProjectEntity?> GetProjectByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects in the workspace</returns>
    Task<IEnumerable<ProjectEntity>> GetProjectsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="project">The project to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated project</returns>
    Task<ProjectEntity> UpdateProjectAsync(ProjectEntity project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project (soft delete).
    /// </summary>
    /// <param name="id">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project was deleted, otherwise false</returns>
    Task<bool> DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for projects within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching projects</returns>
    Task<IEnumerable<ProjectEntity>> SearchProjectsAsync(Guid workspaceId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last analyzed timestamp for a project.
    /// </summary>
    /// <param name="projectId">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the update was successful, otherwise false</returns>
    Task<bool> UpdateLastAnalyzedAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects that need analysis within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="beforeDate">Projects analyzed before this date need re-analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects needing analysis</returns>
    Task<IEnumerable<ProjectEntity>> GetProjectsNeedingAnalysisAsync(Guid workspaceId, DateTime beforeDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project name is unique within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="excludeProjectId">Optional project ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name is unique, otherwise false</returns>
    Task<bool> IsProjectNameUniqueAsync(Guid workspaceId, string name, Guid? excludeProjectId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a project path is valid and not already in use within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project path</param>
    /// <param name="excludeProjectId">Optional project ID to exclude from validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the path is valid, otherwise false</returns>
    Task<bool> ValidateProjectPathAsync(Guid workspaceId, string path, Guid? excludeProjectId = null, CancellationToken cancellationToken = default);
}
