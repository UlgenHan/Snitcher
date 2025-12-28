using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.Services.Database;
using Snitcher.UI.Desktop.Dialogs;
using Microsoft.Extensions.Logging;

namespace Snitcher.UI.Desktop.ViewModels
{
    public partial class SnitcherMainViewModel : ObservableObject
    {
        private readonly IDatabaseIntegrationService _databaseService;
        private readonly ILogger<SnitcherMainViewModel> _logger;

        [ObservableProperty]
        private ObservableCollection<Workspace> _workspaces = new();

        [ObservableProperty]
        private ObservableCollection<Project> _recentProjects = new();

        [ObservableProperty]
        private ObservableCollection<Namespace> _namespaces = new();

        [ObservableProperty]
        private Workspace? _selectedWorkspace;

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private Namespace? _selectedNamespace;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _showSearchResults = false;

        [ObservableProperty]
        private SearchResults? _searchResults;

        [ObservableProperty]
        private bool _isWorkspaceOpened = false;

        public bool IsNotWorkspaceOpenedAndNotSearching => !IsWorkspaceOpened && !ShowSearchResults;

        public SnitcherMainViewModel(IDatabaseIntegrationService databaseService, ILogger<SnitcherMainViewModel> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Initializing database...";
                
                await _databaseService.InitializeAsync();
                await LoadDataAsync();
                
                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize application");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Workspaces.Clear();
                RecentProjects.Clear();
                var workspaces = await _databaseService.GetWorkspacesAsync();
                foreach (var workspace in workspaces.OrderBy(w => w.IsDefault ? 0 : 1).ThenBy(w => w.Name))
                {
                    // Load projects for this workspace
                    var projects = await _databaseService.GetProjectsAsync(workspace.Id);
                    workspace.Projects.Clear();
                    foreach (var project in projects)
                    {
                        workspace.Projects.Add(project);
                    }

                    // Load namespaces for this workspace
                    var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
                    workspace.Namespaces.Clear();
                    foreach (var ns in namespaces)
                    {
                        workspace.Namespaces.Add(ns);
                    }

                    Workspaces.Add(workspace);
                }

                // Load recent projects (take most recent across all workspaces, limited to 6)
                var recent = Workspaces
                    .SelectMany(w => w.Projects)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(6);
                foreach (var project in recent)
                {
                    RecentProjects.Add(project);
                }

                // Select the first workspace if none is selected
                if (SelectedWorkspace == null && Workspaces.Any())
                {
                    SelectedWorkspace = Workspaces.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load data");
                StatusMessage = $"Error loading data: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CreateWorkspace()
        {
            try
            {
                var dialog = new CreateWorkspaceDialog();
                var result = await dialog.ShowDialog<Workspace?>(GetParentWindow());
                
                if (result != null)
                {
                    // Refresh the entire workspaces list to show the new workspace and select it
                    if (Guid.TryParse(result.Id, out var createdId))
                    {
                        await RefreshWorkspacesAsync(createdId);
                    }
                    else
                    {
                        await RefreshWorkspacesAsync();
                    }
                    StatusMessage = "Workspace created successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create workspace");
                StatusMessage = $"Error creating workspace: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CreateProject(Workspace? workspace)
        {
            try
            {
                var targetWorkspace = workspace ?? SelectedWorkspace;
                if (targetWorkspace == null)
                {
                    StatusMessage = "Please select a workspace first";
                    return;
                }

                var dialog = new CreateProjectDialog();
                var result = await dialog.ShowDialog<Project?>(GetParentWindow());
                
                if (result != null)
                {
                    // Refresh workspaces to show the new project and keep selection
                    if (Guid.TryParse(targetWorkspace.Id, out var workspaceGuid))
                    {
                        await RefreshWorkspacesAsync(workspaceGuid);
                        SelectedWorkspace = Workspaces.FirstOrDefault(w => w.Id == targetWorkspace.Id) ?? SelectedWorkspace;
                    }
                    else
                    {
                        await RefreshWorkspaceAsync(targetWorkspace);
                    }
                    StatusMessage = "Project created successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create project");
                StatusMessage = $"Error creating project: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CreateNamespace(Workspace? workspace)
        {
            try
            {
                var targetWorkspace = workspace ?? SelectedWorkspace;
                if (targetWorkspace == null)
                {
                    StatusMessage = "Please select a workspace first";
                    return;
                }

                var dialog = new CreateNamespaceDialog();
                var result = await dialog.ShowDialog<Namespace?>(GetParentWindow());
                
                if (result != null)
                {
                    // Refresh the workspace to show new namespace
                    await RefreshWorkspaceAsync(targetWorkspace);
                    StatusMessage = "Namespace created successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create namespace");
                StatusMessage = $"Error creating namespace: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task OpenWorkspace(Workspace? workspace)
        {
            System.Diagnostics.Debug.WriteLine($"OpenWorkspace command called with workspace: {workspace?.Name ?? "null"}");
            
            if (workspace == null) 
            {
                System.Diagnostics.Debug.WriteLine("Workspace is null, returning");
                return;
            }

            try
            {
                StatusMessage = $"Opening workspace: {workspace.Name}";
                System.Diagnostics.Debug.WriteLine($"Setting SelectedWorkspace to: {workspace.Name}");
                
                SelectedWorkspace = workspace;
                await LoadWorkspaceDataAsync(workspace);
                
                // Show workspace detail view
                IsWorkspaceOpened = true;
                ShowSearchResults = false; // Hide search results when opening workspace
                
                StatusMessage = $"Workspace {workspace.Name} loaded with {workspace.Projects.Count} projects";
                System.Diagnostics.Debug.WriteLine($"Workspace {workspace.Name} loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open workspace");
                StatusMessage = $"Error opening workspace: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error opening workspace: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CloseWorkspace()
        {
            IsWorkspaceOpened = false;
            SelectedWorkspace = null;
            StatusMessage = "Ready";
        }

        [RelayCommand]
        public async Task OpenProject(Project? project)
        {
            if (project == null) return;

            try
            {
                StatusMessage = $"Opening project: {project.Name}";
                
                SelectedProject = project;
                
                // TODO: Open project in main application window
                System.Diagnostics.Debug.WriteLine($"Opening project: {project.Name}");
                
                StatusMessage = $"Project {project.Name} opened";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open project");
                StatusMessage = $"Error opening project: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task DeleteWorkspace(Workspace? workspace)
        {
            if (workspace == null) return;

            if (workspace.IsDefault)
            {
                StatusMessage = "Cannot delete the default workspace";
                return;
            }

            try
            {
                StatusMessage = $"Deleting workspace: {workspace.Name}";
                
                var success = await _databaseService.DeleteWorkspaceAsync(workspace.Id);
                if (success)
                {
                    // Remove related projects from recent list first
                    var toRemove = RecentProjects.Where(p => p.WorkspaceId == workspace.Id).ToList();
                    foreach (var project in toRemove)
                    {
                        RecentProjects.Remove(project);
                    }

                    Workspaces.Remove(workspace);
                    if (SelectedWorkspace?.Id == workspace.Id)
                    {
                        SelectedWorkspace = Workspaces.FirstOrDefault();
                    }
                    StatusMessage = "Workspace deleted successfully";
                }
                else
                {
                    StatusMessage = "Failed to delete workspace";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete workspace");
                StatusMessage = $"Error deleting workspace: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task DeleteProject(Project? project)
        {
            if (project == null) return;

            try
            {
                StatusMessage = $"Deleting project: {project.Name}";
                
                // Call database service to delete project
                var success = await _databaseService.DeleteProjectAsync(project.Id);
                if (success)
                {
                    var workspace = Workspaces.FirstOrDefault(w => w.Id == project.WorkspaceId);
                    if (workspace != null)
                    {
                        workspace.Projects.Remove(project);
                    }
                    RecentProjects.Remove(project);
                    StatusMessage = "Project deleted successfully";
                }
                else
                {
                    StatusMessage = "Failed to delete project";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete project");
                StatusMessage = $"Error deleting project: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task DeleteNamespace(Namespace? namespaceObj)
        {
            if (namespaceObj == null) return;

            try
            {
                StatusMessage = $"Deleting namespace: {namespaceObj.Name}";
                
                // Call database service to delete namespace (cascade delete projects)
                var success = await _databaseService.DeleteNamespaceAsync(namespaceObj.Id);
                if (success)
                {
                    var workspace = Workspaces.FirstOrDefault(w => w.Id == namespaceObj.WorkspaceId);
                    if (workspace != null)
                    {
                        // Remove the namespace
                        workspace.Namespaces.Remove(namespaceObj);
                        
                        // Remove all projects in this namespace
                        var projectsToDelete = workspace.Projects.Where(p => p.NamespaceId == namespaceObj.Id).ToList();
                        foreach (var project in projectsToDelete)
                        {
                            workspace.Projects.Remove(project);
                            RecentProjects.Remove(project);
                        }
                    }
                    StatusMessage = "Namespace and all its projects deleted successfully";
                }
                else
                {
                    StatusMessage = "Failed to delete namespace";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete namespace");
                StatusMessage = $"Error deleting namespace: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task Search()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                ShowSearchResults = false;
                SearchResults = null;
                return;
            }

            try
            {
                StatusMessage = "Searching...";
                IsLoading = true;
                
                var results = await _databaseService.SearchAsync(SearchTerm);
                SearchResults = results;
                ShowSearchResults = true;
                
                StatusMessage = $"Found {results.Workspaces.Count} workspace(s) and {results.Projects.Count} project(s)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search");
                StatusMessage = $"Error searching: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchTerm = string.Empty;
            ShowSearchResults = false;
            SearchResults = null;
            StatusMessage = "Ready";
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            try
            {
                StatusMessage = "Refreshing data...";
                IsLoading = true;
                
                await LoadDataAsync();
                
                StatusMessage = "Data refreshed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh data");
                StatusMessage = $"Error refreshing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadWorkspaceDataAsync(Workspace workspace)
        {
            try
            {
                var projects = await _databaseService.GetProjectsAsync(workspace.Id);
                workspace.Projects.Clear();
                foreach (var project in projects)
                {
                    workspace.Projects.Add(project);
                }

                var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
                Namespaces.Clear();
                foreach (var ns in namespaces)
                {
                    Namespaces.Add(ns);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load workspace data");
            }
        }

        private async Task RefreshWorkspaceAsync(Workspace workspace)
        {
            try
            {
                // Refresh projects
                var projects = await _databaseService.GetProjectsAsync(workspace.Id);
                workspace.Projects.Clear();
                foreach (var project in projects)
                {
                    workspace.Projects.Add(project);
                }

                // Refresh namespaces
                var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
                workspace.Namespaces.Clear();
                foreach (var ns in namespaces)
                {
                    workspace.Namespaces.Add(ns);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh workspace");
            }
        }

        private async Task RefreshWorkspacesAsync(Guid? preferredWorkspaceId = null)
        {
            try
            {
                var workspaces = await _databaseService.GetWorkspacesAsync();
                Workspaces.Clear();
                RecentProjects.Clear();
                foreach (var workspace in workspaces)
                {
                    // Load projects for each workspace
                    var projects = await _databaseService.GetProjectsAsync(workspace.Id);
                    workspace.Projects.Clear();
                    foreach (var project in projects)
                    {
                        workspace.Projects.Add(project);
                    }

                    // Load namespaces for each workspace (returns empty now)
                    var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
                    workspace.Namespaces.Clear();
                    foreach (var ns in namespaces)
                    {
                        workspace.Namespaces.Add(ns);
                    }

                    Workspaces.Add(workspace);
                }

                // Update recent projects
                var recent = Workspaces
                    .SelectMany(w => w.Projects)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(8);
                foreach (var project in recent)
                {
                    RecentProjects.Add(project);
                }

                // Restore selected workspace if preferred or existing
                if (preferredWorkspaceId.HasValue)
                {
                    var preferred = Workspaces.FirstOrDefault(w => Guid.TryParse(w.Id, out var id) && id == preferredWorkspaceId.Value);
                    if (preferred != null)
                    {
                        SelectedWorkspace = preferred;
                        return;
                    }
                }

                if (SelectedWorkspace != null && Workspaces.Any(w => w.Id == SelectedWorkspace.Id))
                {
                    SelectedWorkspace = Workspaces.First(w => w.Id == SelectedWorkspace.Id);
                }
                else if (Workspaces.Any())
                {
                    SelectedWorkspace = Workspaces.First();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh workspaces");
            }
        }

        private Window? GetParentWindow()
        {
            // This would need to be injected or passed in from the view
            // For now, we'll use a simple approach
            return App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;
        }

        partial void OnSelectedWorkspaceChanged(Workspace? value)
        {
            if (value != null)
            {
                _ = Task.Run(async () => await LoadWorkspaceDataAsync(value));
            }
        }

        partial void OnSearchTermChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ClearSearch();
            }
        }
    }
}
