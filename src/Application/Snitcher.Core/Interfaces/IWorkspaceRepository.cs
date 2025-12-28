using Snitcher.Core.Entities;

namespace Snitcher.Core.Interfaces;

/// <summary>
/// Defines the contract for workspace data access operations.
/// </summary>
public interface IWorkspaceRepository : IRepository<Workspace>
{
    /// <summary>
    /// Gets a workspace by name.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    Task<Workspace?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default workspace if found, otherwise null</returns>
    Task<Workspace?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a workspace as the default workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    Task<bool> SetDefaultAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workspaces with their projects and namespaces included.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of workspaces with related data</returns>
    Task<IEnumerable<Workspace>> GetWithRelatedDataAsync(CancellationToken cancellationToken = default);
}
