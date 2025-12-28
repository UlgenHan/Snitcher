using System.Collections.Generic;
using System.Threading.Tasks;
using Snitcher.UI.Desktop.Models.WorkSpaces;

namespace Snitcher.UI.Desktop.Services.Database;

/// <summary>
/// Interface for database integration service to enable testing
/// </summary>
public interface IDatabaseIntegrationService
{
    Task InitializeAsync();
    Task<IEnumerable<Workspace>> GetWorkspacesAsync();
    Task<Workspace?> GetWorkspaceAsync(string id);
    Task<Workspace> CreateWorkspaceAsync(string name, string description = "");
    Task<Workspace> UpdateWorkspaceAsync(Workspace workspace);
    Task<bool> DeleteWorkspaceAsync(string id);
    Task<IEnumerable<Snitcher.UI.Desktop.Models.WorkSpaces.Project>> GetProjectsAsync(string workspaceId);
    Task<Snitcher.UI.Desktop.Models.WorkSpaces.Project> CreateProjectAsync(string workspaceId, string name, string description = "", string path = "");
    Task<IEnumerable<Namespace>> GetNamespacesAsync(string workspaceId);
    Task<Namespace> CreateNamespaceAsync(string workspaceId, string name, string fullName, string? parentNamespaceId = null);
    Task<bool> DeleteProjectAsync(string projectId);
    Task<bool> DeleteNamespaceAsync(string namespaceId);
    Task<SearchResults> SearchAsync(string searchTerm);
}
