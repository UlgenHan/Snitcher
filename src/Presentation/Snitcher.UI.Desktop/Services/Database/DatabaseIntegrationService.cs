using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Interfaces;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Workspace = Snitcher.UI.Desktop.Models.WorkSpaces.Workspace;
using Namespace = Snitcher.UI.Desktop.Models.WorkSpaces.Namespace;

namespace Snitcher.UI.Desktop.Services.Database;

/// <summary>
/// Database integration service that bridges UI models with database entities.
/// Simplified architecture with only Workspaces and Projects tables.
/// </summary>
public class DatabaseIntegrationService : IDatabaseIntegrationService
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectService _projectService;
    private readonly ILogger<DatabaseIntegrationService> _logger;

    public DatabaseIntegrationService(
        IWorkspaceService workspaceService,
        IProjectService projectService,
        ILogger<DatabaseIntegrationService> logger)
    {
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the database service.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database service...");
            
            // Ensure default workspace exists
            await EnsureDefaultWorkspaceAsync();
            
            _logger.LogInformation("Database service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database service");
            throw;
        }
    }

    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    /// <returns>Collection of workspaces</returns>
    public async Task<IEnumerable<Workspace>> GetWorkspacesAsync()
    {
        try
        {
            var workspaces = await _workspaceService.GetAllWorkspacesAsync();
            return workspaces.Select(MapToWorkspaceModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workspaces");
            return new List<Workspace>();
        }
    }

    /// <summary>
    /// Gets a workspace by ID.
    /// </summary>
    /// <param name="id">The workspace ID</param>
    /// <returns>The workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetWorkspaceAsync(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var workspace = await _workspaceService.GetWorkspaceByIdAsync(guid);
            return workspace != null ? MapToWorkspaceModel(workspace) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workspace {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Creates a new workspace.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="description">The workspace description</param>
    /// <returns>The created workspace</returns>
    public async Task<Workspace> CreateWorkspaceAsync(string name, string description = "")
    {
        try
        {
            var workspace = await _workspaceService.CreateWorkspaceAsync(name, description);
            return MapToWorkspaceModel(workspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workspace {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Updates a workspace.
    /// </summary>
    /// <param name="workspace">The workspace to update</param>
    /// <returns>The updated workspace</returns>
    public async Task<Workspace> UpdateWorkspaceAsync(Workspace workspace)
    {
        try
        {
            if (!Guid.TryParse(workspace.Id, out var guid))
                throw new ArgumentException("Invalid workspace ID");

            // Get the existing workspace from the database
            var existingWorkspace = await _workspaceService.GetWorkspaceByIdAsync(guid);
            if (existingWorkspace == null)
                throw new ArgumentException("Workspace not found");

            // Update the properties
            existingWorkspace.Name = workspace.Name;
            existingWorkspace.Description = workspace.Description;
            existingWorkspace.IsDefault = workspace.IsDefault;

            var updated = await _workspaceService.UpdateWorkspaceAsync(existingWorkspace);
            return MapToWorkspaceModel(updated);
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
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteWorkspaceAsync(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
                return false;

            return await _workspaceService.DeleteWorkspaceAsync(guid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workspace {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// Gets projects within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>Collection of projects</returns>
    public async Task<IEnumerable<Snitcher.UI.Desktop.Models.WorkSpaces.Project>> GetProjectsAsync(string workspaceId)
    {
        try
        {
            _logger.LogInformation("Loading projects for workspace {WorkspaceId}", workspaceId);
            
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
            {
                _logger.LogWarning("Invalid workspace ID: {WorkspaceId}", workspaceId);
                return new List<Snitcher.UI.Desktop.Models.WorkSpaces.Project>();
            }

            // Get projects using the new ProjectService
            var projectEntities = await _projectService.GetProjectsByWorkspaceAsync(workspaceGuid);
            
            _logger.LogInformation("Found {Count} projects for workspace {WorkspaceId}", projectEntities.Count(), workspaceId);

            // Map to UI models
            var projects = projectEntities.Select(p => new Snitcher.UI.Desktop.Models.WorkSpaces.Project
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                WorkspaceId = workspaceId,
                Path = p.Path,
                Version = p.Version ?? string.Empty,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            _logger.LogInformation("Successfully loaded {Count} projects for workspace {WorkspaceId}", projects.Count, workspaceId);
            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get projects for workspace {WorkspaceId}", workspaceId);
            return new List<Snitcher.UI.Desktop.Models.WorkSpaces.Project>();
        }
    }

    /// <summary>
    /// Creates a new project within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="name">The project name</param>
    /// <param name="description">The project description</param>
    /// <param name="path">The project path (optional)</param>
    /// <returns>The created project</returns>
    public async Task<Snitcher.UI.Desktop.Models.WorkSpaces.Project> CreateProjectAsync(string workspaceId, string name, string description = "", string path = "")
    {
        try
        {
            _logger.LogInformation("Creating project {Name} in workspace {WorkspaceId}", name, workspaceId);
            
            if (!Guid.TryParse(workspaceId, out var workspaceGuid))
                throw new ArgumentException("Invalid workspace ID", nameof(workspaceId));

            // Create the project using the new ProjectService
            var projectEntity = await _projectService.CreateProjectAsync(workspaceGuid, name, description, path);

            // Map to UI model
            var project = new Snitcher.UI.Desktop.Models.WorkSpaces.Project
            {
                Id = projectEntity.Id.ToString(),
                Name = projectEntity.Name,
                Description = projectEntity.Description ?? string.Empty,
                WorkspaceId = workspaceId,
                Path = projectEntity.Path,
                Version = projectEntity.Version ?? string.Empty,
                CreatedAt = projectEntity.CreatedAt,
                UpdatedAt = projectEntity.UpdatedAt
            };

            _logger.LogInformation("Successfully created project {Name} with ID {Id}", name, project.Id);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project {Name} in workspace {WorkspaceId}", name, workspaceId);
            throw;
        }
    }

    /// <summary>
    /// Gets namespaces within a workspace.
    /// REMOVED: Namespace functionality is no longer supported.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>Empty collection - namespaces are no longer supported</returns>
    public async Task<IEnumerable<Namespace>> GetNamespacesAsync(string workspaceId)
    {
        _logger.LogInformation("Namespaces are no longer supported. Returning empty collection for workspace {WorkspaceId}", workspaceId);
        return await Task.FromResult(new List<Namespace>());
    }

    /// <summary>
    /// Creates a new namespace within a workspace.
    /// REMOVED: Namespace functionality is no longer supported.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="name">The namespace name</param>
    /// <param name="fullName">The full namespace name</param>
    /// <param name="parentNamespaceId">The parent namespace ID (optional)</param>
    /// <returns>Throws NotSupportedException - namespaces are no longer supported</returns>
    public async Task<Namespace> CreateNamespaceAsync(string workspaceId, string name, string fullName, string? parentNamespaceId = null)
    {
        throw new NotSupportedException("Namespace functionality is no longer supported in the simplified architecture.");
    }

    /// <summary>
    /// Deletes a project from the database.
    /// </summary>
    /// <param name="projectId">The project ID</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteProjectAsync(string projectId)
    {
        try
        {
            _logger.LogInformation("Deleting project {ProjectId}", projectId);
            
            if (!Guid.TryParse(projectId, out var guid))
                return false;

            return await _projectService.DeleteProjectAsync(guid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project {ProjectId}", projectId);
            return false;
        }
    }

    /// <summary>
    /// Deletes a namespace from the database.
    /// REMOVED: Namespace functionality is no longer supported.
    /// </summary>
    /// <param name="namespaceId">The namespace ID</param>
    /// <returns>Throws NotSupportedException - namespaces are no longer supported</returns>
    public async Task<bool> DeleteNamespaceAsync(string namespaceId)
    {
        throw new NotSupportedException("Namespace functionality is no longer supported in the simplified architecture.");
    }

    /// <summary>
    /// Searches for workspaces and projects.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>Search results</returns>
    public async Task<SearchResults> SearchAsync(string searchTerm)
    {
        try
        {
            _logger.LogInformation("Starting search for term: {SearchTerm}", searchTerm);
            
            // Get ALL workspaces first (not just matching ones) to search for projects
            var allWorkspaces = await _workspaceService.GetAllWorkspacesAsync();
            var allWorkspaceModels = allWorkspaces.Select(MapToWorkspaceModel).ToList();
            _logger.LogInformation("Found {Count} total workspaces", allWorkspaceModels.Count);

            // Search workspaces by name and description (filter the matching ones)
            var matchingWorkspaces = allWorkspaceModels
                .Where(w => w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (w.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            _logger.LogInformation("Found {Count} workspaces matching search term", matchingWorkspaces.Count);

            // Search projects across ALL workspaces
            var allProjects = new List<Snitcher.UI.Desktop.Models.WorkSpaces.Project>();
            
            foreach (var workspace in allWorkspaceModels)
            {
                var workspaceId = Guid.Parse(workspace.Id);
                var projects = await _projectService.GetProjectsByWorkspaceAsync(workspaceId);
                _logger.LogInformation("Found {Count} projects in workspace {WorkspaceName}", projects.Count(), workspace.Name);
                
                var projectModels = projects.Select(p => MapToProjectModel(p, workspace));
                allProjects.AddRange(projectModels);
            }
            
            _logger.LogInformation("Total projects found across all workspaces: {Count}", allProjects.Count);

            // Filter projects by search term (name and description)
            var matchingProjects = allProjects
                .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (p.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            
            _logger.LogInformation("Found {Count} projects matching search term", matchingProjects.Count);

            return new SearchResults
            {
                Workspaces = matchingWorkspaces,
                Projects = matchingProjects
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search for {SearchTerm}", searchTerm);
            return new SearchResults();
        }
    }

    /// <summary>
    /// Maps a ProjectEntity to a UI Project model.
    /// </summary>
    /// <param name="entity">The project entity</param>
    /// <param name="workspace">The workspace model</param>
    /// <returns>The UI project model</returns>
    private static Snitcher.UI.Desktop.Models.WorkSpaces.Project MapToProjectModel(Core.Entities.ProjectEntity entity, Workspace workspace)
    {
        return new Snitcher.UI.Desktop.Models.WorkSpaces.Project
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description ?? "",
            Path = entity.Path ?? "",
            Version = entity.Version ?? "1.0.0",
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            WorkspaceId = workspace.Id
        };
    }

    /// <summary>
    /// Ensures that a default workspace exists.
    /// </summary>
    private async Task EnsureDefaultWorkspaceAsync()
    {
        try
        {
            var defaultWorkspace = await _workspaceService.GetDefaultWorkspaceAsync();
            if (defaultWorkspace == null)
            {
                _logger.LogInformation("Creating default workspace...");
                var workspace = await _workspaceService.CreateWorkspaceAsync(
                    "Default Workspace", 
                    "Default workspace for Snitcher projects",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Snitcher", "Default")
                );
                
                await _workspaceService.SetDefaultWorkspaceAsync(workspace.Id);
                _logger.LogInformation("Default workspace created with ID {Id}", workspace.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default workspace exists");
            throw;
        }
    }

    /// <summary>
    /// Maps a Core workspace entity to a UI workspace model.
    /// </summary>
    /// <param name="entity">The core workspace entity</param>
    /// <returns>The UI workspace model</returns>
    private static Workspace MapToWorkspaceModel(Core.Entities.Workspace entity)
    {
        return new Workspace
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description ?? "",
            IsDefault = entity.IsDefault,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Projects = new ObservableCollection<Snitcher.UI.Desktop.Models.WorkSpaces.Project>(),
            Namespaces = new ObservableCollection<Namespace>()
        };
    }
}

/// <summary>
/// Search results container.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Gets or sets the matching workspaces.
    /// </summary>
    public List<Workspace> Workspaces { get; set; } = new();

    /// <summary>
    /// Gets or sets the matching projects.
    /// </summary>
    public List<Snitcher.UI.Desktop.Models.WorkSpaces.Project> Projects { get; set; } = new();
}
