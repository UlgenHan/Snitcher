using Microsoft.Extensions.Logging;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Interfaces;

namespace Snitcher.Service.Services;

/// <summary>
/// Implementation of the workspace service.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkspaceService> _logger;

    /// <summary>
    /// Initializes a new instance of the WorkspaceService class.
    /// </summary>
    /// <param name="workspaceRepository">The workspace repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="logger">The logger</param>
    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IUnitOfWork unitOfWork,
        ILogger<WorkspaceService> logger)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new workspace.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="description">The workspace description</param>
    /// <param name="path">The workspace path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workspace</returns>
    public async Task<Workspace> CreateWorkspaceAsync(string name, string description = "", string path = "", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating workspace {Name}", name);

        try
        {
            // Check if workspace with same name already exists
            var existingWorkspace = await _workspaceRepository.GetByNameAsync(name, cancellationToken);
            if (existingWorkspace != null)
            {
                throw new InvalidOperationException($"A workspace with name '{name}' already exists.");
            }

            // Use default path if not provided
            if (string.IsNullOrWhiteSpace(path))
            {
                path = GetDefaultWorkspacePath(name);
            }

            var workspace = new Workspace
            {
                Name = name,
                Description = description,
                Path = path,
                IsDefault = false // Don't make new workspaces default by default
            };

            var createdWorkspace = await _workspaceRepository.AddAsync(workspace, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created workspace {Name} with ID {Id}", createdWorkspace.Name, createdWorkspace.Id);
            return createdWorkspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workspace {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of workspaces</returns>
    public async Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all workspaces");

        try
        {
            var workspaces = await _workspaceRepository.GetWithRelatedDataAsync(cancellationToken);
            return workspaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all workspaces");
            throw;
        }
    }

    /// <summary>
    /// Gets a workspace by ID.
    /// </summary>
    /// <param name="id">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting workspace by ID {Id}", id);

        try
        {
            return await _workspaceRepository.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workspace by ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets a workspace by name.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetWorkspaceByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting workspace by name {Name}", name);

        try
        {
            return await _workspaceRepository.GetByNameAsync(name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workspace by name {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing workspace.
    /// </summary>
    /// <param name="workspace">The workspace to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated workspace</returns>
    public async Task<Workspace> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating workspace {Name} with ID {Id}", workspace.Name, workspace.Id);

        try
        {
            var updatedWorkspace = await _workspaceRepository.UpdateAsync(workspace, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated workspace {Name}", updatedWorkspace.Name);
            return updatedWorkspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update workspace {Name}", workspace.Name);
            throw;
        }
    }

    /// <summary>
    /// Deletes a workspace.
    /// </summary>
    /// <param name="id">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting workspace with ID {Id}", id);

        try
        {
            var workspace = await _workspaceRepository.GetByIdAsync(id, cancellationToken);
            if (workspace == null)
            {
                _logger.LogWarning("Workspace with ID {Id} not found", id);
                return false;
            }

            // Don't allow deletion of default workspace
            if (workspace.IsDefault)
            {
                throw new InvalidOperationException("Cannot delete the default workspace.");
            }

            await _workspaceRepository.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted workspace {Name}", workspace.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workspace with ID {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets the default workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetDefaultWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting default workspace");

        try
        {
            return await _workspaceRepository.GetDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default workspace");
            throw;
        }
    }

    /// <summary>
    /// Sets a workspace as the default workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> SetDefaultWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting workspace with ID {Id} as default", workspaceId);

        try
        {
            var result = await _workspaceRepository.SetDefaultAsync(workspaceId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (result)
            {
                _logger.LogInformation("Successfully set workspace with ID {Id} as default", workspaceId);
            }
            else
            {
                _logger.LogWarning("Failed to set workspace with ID {Id} as default - workspace not found", workspaceId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set workspace with ID {Id} as default", workspaceId);
            throw;
        }
    }

    /// <summary>
    /// Searches for workspaces by name or description.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching workspaces</returns>
    public async Task<IEnumerable<Workspace>> SearchWorkspacesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching workspaces with term {SearchTerm}", searchTerm);

        try
        {
            var workspaces = await _workspaceRepository.GetAllAsync(cancellationToken);
            var filteredWorkspaces = workspaces
                .Where(w => w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (w.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            return filteredWorkspaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search workspaces with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Gets the default path for a workspace.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <returns>The default workspace path</returns>
    private static string GetDefaultWorkspacePath(string name)
    {
        var sanitized = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Snitcher", "Workspaces", sanitized);
    }
}
