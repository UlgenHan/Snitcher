using Snitcher.Core.Entities;

namespace Snitcher.Service.Interfaces;

/// <summary>
/// Defines the contract for workspace service operations.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Creates a new workspace.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="description">The workspace description</param>
    /// <param name="path">The workspace path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workspace</returns>
    Task<Workspace> CreateWorkspaceAsync(string name, string description = "", string path = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of workspaces</returns>
    Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by ID.
    /// </summary>
    /// <param name="id">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    Task<Workspace?> GetWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by name.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    Task<Workspace?> GetWorkspaceByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workspace.
    /// </summary>
    /// <param name="workspace">The workspace to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated workspace</returns>
    Task<Workspace> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workspace.
    /// </summary>
    /// <param name="id">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    Task<bool> DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default workspace if found, otherwise null</returns>
    Task<Workspace?> GetDefaultWorkspaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a workspace as the default workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    Task<bool> SetDefaultWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for workspaces by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching workspaces</returns>
    Task<IEnumerable<Workspace>> SearchWorkspacesAsync(string searchTerm, CancellationToken cancellationToken = default);
}
