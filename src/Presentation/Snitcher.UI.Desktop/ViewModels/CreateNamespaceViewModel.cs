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

public partial class CreateNamespaceViewModel : ObservableObject
{
    private readonly DatabaseIntegrationService _databaseService;
    private readonly ILogger<CreateNamespaceViewModel> _logger;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private Namespace? _parentNamespace;

    [ObservableProperty]
    private ObservableCollection<Workspace> _availableWorkspaces = new();

    [ObservableProperty]
    private ObservableCollection<Namespace> _availableNamespaces = new();

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action<Namespace?>? NamespaceCreated;

    public CreateNamespaceViewModel(
        DatabaseIntegrationService databaseService,
        ILogger<CreateNamespaceViewModel> logger)
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
            _logger.LogError(ex, "Failed to initialize");
            ErrorMessage = $"Failed to load data: {ex.Message}";
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

            ParentNamespace = AvailableNamespaces.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load namespaces");
            ErrorMessage = $"Failed to load namespaces: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreateNamespace))]
    private async Task CreateNamespaceAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Namespace name is required";
                return;
            }

            if (SelectedWorkspace == null)
            {
                ErrorMessage = "Please select a workspace";
                return;
            }

            // Create the namespace
            var newNamespace = new Namespace
            {
                Name = Name,
                FullName = string.IsNullOrWhiteSpace(FullName) ? Name : FullName,
                WorkspaceId = SelectedWorkspace.Id,
                ParentNamespaceId = ParentNamespace?.Id == "root" ? null : ParentNamespace?.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Call the database service to create the namespace
            var createdNamespace = await _databaseService.CreateNamespaceAsync(
                SelectedWorkspace.Id, 
                Name, 
                string.IsNullOrWhiteSpace(FullName) ? Name : FullName,
                ParentNamespace?.Id == "root" ? null : ParentNamespace?.Id
            );

            NamespaceCreated?.Invoke(createdNamespace);

            // Reset form
            Name = string.Empty;
            FullName = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create namespace");
            ErrorMessage = $"Failed to create namespace: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateNamespace()
    {
        return !IsLoading && 
               !string.IsNullOrWhiteSpace(Name) && 
               SelectedWorkspace != null &&
               ParentNamespace != null;
    }

    [RelayCommand]
    private void Cancel()
    {
        NamespaceCreated?.Invoke(null);
    }

    partial void OnNameChanged(string value)
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullName = value;
        }
        CreateNamespaceCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedWorkspaceChanged(Workspace? value)
    {
        ErrorMessage = string.Empty;
        CreateNamespaceCommand.NotifyCanExecuteChanged();
        if (value != null)
        {
            _ = OnWorkspaceChanged();
        }
    }

    partial void OnParentNamespaceChanged(Namespace? value)
    {
        ErrorMessage = string.Empty;
        CreateNamespaceCommand.NotifyCanExecuteChanged();
    }
}
