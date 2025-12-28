using Microsoft.EntityFrameworkCore;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;

namespace Snitcher.Repository.Repositories;

/// <summary>
/// Entity Framework implementation of the project repository.
/// Handles data access operations for ProjectEntity entities.
/// </summary>
public class ProjectRepository : EfRepository<ProjectEntity>, IProjectRepository
{
    private readonly new SnitcherDbContext _context;

    /// <summary>
    /// Initializes a new instance of the ProjectRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    public ProjectRepository(SnitcherDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a project by name within a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    public async Task<ProjectEntity?> GetByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && p.Name == name && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a project by path within a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The project if found, otherwise null</returns>
    public async Task<ProjectEntity?> GetByPathAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && p.Path == path && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all projects belonging to a specific workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects in the workspace</returns>
    public async Task<IEnumerable<ProjectEntity>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a project with the specified name exists within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="name">The project name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project exists, otherwise false</returns>
    public async Task<bool> ExistsByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.Name == name && !p.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Checks if a project with the specified path exists within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="path">The project file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the project exists, otherwise false</returns>
    public async Task<bool> ExistsByPathAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.Path == path && !p.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Searches for projects by name or description within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching projects</returns>
    public async Task<IEnumerable<ProjectEntity>> SearchAsync(Guid workspaceId, string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetByWorkspaceIdAsync(workspaceId, cancellationToken);

        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted &&
                       (p.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                        (p.Description != null && p.Description.ToLowerInvariant().Contains(lowerSearchTerm))))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets projects that have been analyzed since the specified date within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="since">The date to search from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of analyzed projects</returns>
    public async Task<IEnumerable<ProjectEntity>> GetAnalyzedSinceAsync(Guid workspaceId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted &&
                       p.LastAnalyzedAt.HasValue && p.LastAnalyzedAt >= since)
            .OrderByDescending(p => p.LastAnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets projects that haven't been analyzed recently within a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier</param>
    /// <param name="beforeDate">Projects analyzed before this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of projects needing analysis</returns>
    public async Task<IEnumerable<ProjectEntity>> GetProjectsNeedingAnalysisAsync(Guid workspaceId, DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted &&
                       (!p.LastAnalyzedAt.HasValue || p.LastAnalyzedAt < beforeDate))
            .OrderBy(p => p.LastAnalyzedAt)
            .ToListAsync(cancellationToken);
    }
}
