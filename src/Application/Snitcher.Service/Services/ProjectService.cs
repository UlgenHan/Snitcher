using Microsoft.Extensions.Logging;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Interfaces;

namespace Snitcher.Service.Services;

/// <summary>
/// Service implementation for ProjectEntity business operations.
/// Provides high-level project management functionality for the new database architecture.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProjectService> _logger;

    /// <summary>
    /// Initializes a new instance of the ProjectService class.
    /// </summary>
    /// <param name="projectRepository">The project repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="logger">The logger</param>
    public ProjectService(IProjectRepository projectRepository, IUnitOfWork unitOfWork, ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
    public async Task<ProjectEntity> CreateProjectAsync(Guid workspaceId, string name, string description = "", string path = "", string? version = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating project {Name} in workspace {WorkspaceId}", name, workspaceId);

        // Validate input
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name is required", nameof(name));

        if (workspaceId == Guid.Empty)
            throw new ArgumentException("Workspace ID is required", nameof(workspaceId));

        // Check for duplicate name within workspace
        if (await _projectRepository.ExistsByNameAsync(workspaceId, name, cancellationToken))
            throw new InvalidOperationException($"A project with name '{name}' already exists in this workspace.");

        // Check for duplicate path within workspace if path is provided
        if (!string.IsNullOrWhiteSpace(path) && await _projectRepository.ExistsByPathAsync(workspaceId, path, cancellationToken))
            throw new InvalidOperationException($"A project with path '{path}' already exists in this workspace.");

        // Create the project
        var project = new ProjectEntity
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Path = path?.Trim() ?? string.Empty,
            Version = version?.Trim()
        };

        _logger.LogDebug("Saving project to database");
        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created project {Name} with ID {Id}", name, project.Id);
        return project;
    }

    /// <summary>
    /// Gets a project by its identifier.
    /// </summary>
    /// <param name="id">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    public async Task<ProjectEntity?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return null;

        _logger.LogDebug("Getting project by ID {Id}", id);
        return await _projectRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gets a project by name within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    public async Task<ProjectEntity?> GetProjectByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || string.IsNullOrWhiteSpace(name))
            return null;

        _logger.LogDebug("Getting project {Name} in workspace {WorkspaceId}", name, workspaceId);
        return await _projectRepository.GetByNameAsync(workspaceId, name, cancellationToken);
    }

    /// <summary>
    /// Gets all projects within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects in the workspace</returns>
    public async Task<IEnumerable<ProjectEntity>> GetProjectsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return Enumerable.Empty<ProjectEntity>();

        _logger.LogDebug("Getting projects for workspace {WorkspaceId}", workspaceId);
        return await _projectRepository.GetByWorkspaceIdAsync(workspaceId, cancellationToken);
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="project">The project to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated project</returns>
    public async Task<ProjectEntity> UpdateProjectAsync(ProjectEntity project, CancellationToken cancellationToken = default)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        _logger.LogInformation("Updating project {Name} with ID {Id}", project.Name, project.Id);

        // Validate input
        if (string.IsNullOrWhiteSpace(project.Name))
            throw new ArgumentException("Project name is required");

        if (project.WorkspaceId == Guid.Empty)
            throw new ArgumentException("Workspace ID is required");

        // Check for duplicate name (excluding current project)
        var existingProject = await _projectRepository.GetByNameAsync(project.WorkspaceId, project.Name, cancellationToken);
        if (existingProject != null && existingProject.Id != project.Id)
            throw new InvalidOperationException($"A project with name '{project.Name}' already exists in this workspace.");

        // Check for duplicate path (excluding current project)
        if (!string.IsNullOrWhiteSpace(project.Path))
        {
            var existingPathProject = await _projectRepository.GetByPathAsync(project.WorkspaceId, project.Path, cancellationToken);
            if (existingPathProject != null && existingPathProject.Id != project.Id)
                throw new InvalidOperationException($"A project with path '{project.Path}' already exists in this workspace.");
        }

        // Update the project
        project.UpdateTimestamp();
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated project {Name} with ID {Id}", project.Name, project.Id);
        return project;
    }

    /// <summary>
    /// Deletes a project (soft delete).
    /// </summary>
    /// <param name="id">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project was deleted, otherwise false</returns>
    public async Task<bool> DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return false;

        _logger.LogInformation("Deleting project with ID {Id}", id);

        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (project == null)
            return false;

        await _projectRepository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted project {Name} with ID {Id}", project.Name, id);
        return true;
    }

    /// <summary>
    /// Searches for projects within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching projects</returns>
    public async Task<IEnumerable<ProjectEntity>> SearchProjectsAsync(Guid workspaceId, string searchTerm, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return Enumerable.Empty<ProjectEntity>();

        _logger.LogDebug("Searching projects in workspace {WorkspaceId} with term '{SearchTerm}'", workspaceId, searchTerm);
        return await _projectRepository.SearchAsync(workspaceId, searchTerm, cancellationToken);
    }

    /// <summary>
    /// Updates the last analyzed timestamp for a project.
    /// </summary>
    /// <param name="projectId">The project identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the update was successful, otherwise false</returns>
    public async Task<bool> UpdateLastAnalyzedAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
            return false;

        _logger.LogDebug("Updating last analyzed timestamp for project {ProjectId}", projectId);

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project == null)
            return false;

        project.UpdateLastAnalyzed();
        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Gets projects that need analysis within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="beforeDate">Projects analyzed before this date need re-analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects needing analysis</returns>
    public async Task<IEnumerable<ProjectEntity>> GetProjectsNeedingAnalysisAsync(Guid workspaceId, DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return Enumerable.Empty<ProjectEntity>();

        _logger.LogDebug("Getting projects needing analysis in workspace {WorkspaceId} before {BeforeDate}", workspaceId, beforeDate);
        return await _projectRepository.GetProjectsNeedingAnalysisAsync(workspaceId, beforeDate, cancellationToken);
    }

    /// <summary>
    /// Checks if a project name is unique within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="excludeProjectId">Optional project ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name is unique, otherwise false</returns>
    public async Task<bool> IsProjectNameUniqueAsync(Guid workspaceId, string name, Guid? excludeProjectId = null, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || string.IsNullOrWhiteSpace(name))
            return false;

        _logger.LogDebug("Checking if project name {Name} is unique in workspace {WorkspaceId}", name, workspaceId);

        var existingProject = await _projectRepository.GetByNameAsync(workspaceId, name, cancellationToken);
        return existingProject == null || (excludeProjectId.HasValue && existingProject.Id == excludeProjectId.Value);
    }

    /// <summary>
    /// Validates if a project path is valid and not already in use within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project path</param>
    /// <param name="excludeProjectId">Optional project ID to exclude from validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the path is valid, otherwise false</returns>
    public async Task<bool> ValidateProjectPathAsync(Guid workspaceId, string path, Guid? excludeProjectId = null, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || string.IsNullOrWhiteSpace(path))
            return false;

        _logger.LogDebug("Validating project path {Path} in workspace {WorkspaceId}", path, workspaceId);

        var existingProject = await _projectRepository.GetByPathAsync(workspaceId, path, cancellationToken);
        return existingProject == null || (excludeProjectId.HasValue && existingProject.Id == excludeProjectId.Value);
    }
}
