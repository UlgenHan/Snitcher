using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.Services.Database;

namespace Snitcher.UI.Desktop.ViewModels;

public partial class CreateProjectViewModel : ObservableObject
{
    private readonly DatabaseIntegrationService _databaseService;
    private readonly ILogger<CreateProjectViewModel> _logger;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private Namespace? _selectedNamespace;

    [ObservableProperty]
    private ObservableCollection<Workspace> _availableWorkspaces = new();

    [ObservableProperty]
    private ObservableCollection<Namespace> _availableNamespaces = new();

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action<Project?>? ProjectCreated;

    public CreateProjectViewModel(
        DatabaseIntegrationService databaseService,
        ILogger<CreateProjectViewModel> logger)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            var workspaces = await _databaseService.GetWorkspacesAsync();
            AvailableWorkspaces.Clear();
            foreach (var workspace in workspaces.OrderBy(w => w.Name))
            {
                AvailableWorkspaces.Add(workspace);
            }

            if (AvailableWorkspaces.Any())
            {
                SelectedWorkspace = AvailableWorkspaces.First();
                await LoadNamespacesForWorkspace(SelectedWorkspace);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspaces");
            ErrorMessage = $"Failed to load workspaces: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OnWorkspaceChanged()
    {
        if (SelectedWorkspace != null)
        {
            await LoadNamespacesForWorkspace(SelectedWorkspace);
        }
    }

    private async Task LoadNamespacesForWorkspace(Workspace workspace)
    {
        try
        {
            var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
            AvailableNamespaces.Clear();
            
            // Add "Root Level" option
            AvailableNamespaces.Add(new Namespace { Id = "root", Name = "Root Level", FullName = "Root Level" });
            
            foreach (var ns in namespaces.OrderBy(n => n.FullName))
            {
                AvailableNamespaces.Add(ns);
            }

            SelectedNamespace = AvailableNamespaces.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load namespaces");
            ErrorMessage = $"Failed to load namespaces: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Project name is required";
                return;
            }

            if (SelectedWorkspace == null)
            {
                ErrorMessage = "Please select a workspace";
                return;
            }

            var project = await _databaseService.CreateProjectAsync(
                SelectedWorkspace.Id, 
                Name, 
                Description,
                SelectedNamespace?.Id == "root" ? null : SelectedNamespace?.Id
            );
            ProjectCreated?.Invoke(project);

            // Reset form
            Name = string.Empty;
            Description = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            ErrorMessage = $"Failed to create project: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateProject()
    {
        return !IsLoading && 
               !string.IsNullOrWhiteSpace(Name) && 
               SelectedWorkspace != null;
    }

    [RelayCommand]
    private void Cancel()
    {
        ProjectCreated?.Invoke(null);
    }

    partial void OnNameChanged(string value)
    {
        ErrorMessage = string.Empty;
        CreateProjectCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedWorkspaceChanged(Workspace? value)
    {
        ErrorMessage = string.Empty;
        CreateProjectCommand.NotifyCanExecuteChanged();
    }
}
