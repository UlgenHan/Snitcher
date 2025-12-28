using Microsoft.EntityFrameworkCore;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;

namespace Snitcher.Repository.Repositories;

/// <summary>
/// Entity Framework implementation of the workspace repository.
/// </summary>
public class WorkspaceRepository : EfRepository<Workspace>, IWorkspaceRepository
{
    private readonly new SnitcherDbContext _context;

    /// <summary>
    /// Initializes a new instance of the WorkspaceRepository class.
    /// </summary>
    /// <param name="context">The database context</param>
    public WorkspaceRepository(SnitcherDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a workspace by name.
    /// </summary>
    /// <param name="name">The workspace name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Name == name, cancellationToken);
    }

    /// <summary>
    /// Gets the default workspace.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default workspace if found, otherwise null</returns>
    public async Task<Workspace?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);
    }

    /// <summary>
    /// Sets a workspace as the default workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, otherwise false</returns>
    public async Task<bool> SetDefaultAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // First, unset all existing default workspaces
            var existingDefaults = await _context.Workspaces
                .Where(w => w.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var workspace in existingDefaults)
            {
                workspace.IsDefault = false;
            }

            // Set the new default workspace
            var newDefault = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

            if (newDefault != null)
            {
                newDefault.IsDefault = true;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets workspaces with their projects included.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of workspaces with related data</returns>
    public async Task<IEnumerable<Workspace>> GetWithRelatedDataAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Include(w => w.Projects)
            .OrderBy(w => w.IsDefault ? 0 : 1)
            .ThenBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }
}
