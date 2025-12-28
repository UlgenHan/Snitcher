using Snitcher.Core.Entities;

namespace Snitcher.Core.Interfaces;

/// <summary>
/// Repository interface for ProjectEntity entities.
/// Provides project-specific data access operations for the new database architecture.
/// </summary>
public interface IProjectRepository : IRepository<ProjectEntity, Guid>
{
    /// <summary>
    /// Gets a project by its name within a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    Task<ProjectEntity?> GetByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by its file path within a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    Task<ProjectEntity?> GetByPathAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects belonging to a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects in the workspace</returns>
    Task<IEnumerable<ProjectEntity>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project with the specified name exists within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project exists, otherwise false</returns>
    Task<bool> ExistsByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project with the specified path exists within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project exists, otherwise false</returns>
    Task<bool> ExistsByPathAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for projects by name or description within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching projects</returns>
    Task<IEnumerable<ProjectEntity>> SearchAsync(Guid workspaceId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects that have been analyzed since the specified date within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="since">The date to search from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of analyzed projects</returns>
    Task<IEnumerable<ProjectEntity>> GetAnalyzedSinceAsync(Guid workspaceId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects that haven't been analyzed recently within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="beforeDate">Projects analyzed before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects needing analysis</returns>
    Task<IEnumerable<ProjectEntity>> GetProjectsNeedingAnalysisAsync(Guid workspaceId, DateTime beforeDate, CancellationToken cancellationToken = default);
}
