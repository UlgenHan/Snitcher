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

public partial class CreateWorkspaceViewModel : ObservableObject
{
    private readonly DatabaseIntegrationService _databaseService;
    private readonly ILogger<CreateWorkspaceViewModel> _logger;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action<Workspace?>? WorkspaceCreated;

    public CreateWorkspaceViewModel(
        DatabaseIntegrationService databaseService,
        ILogger<CreateWorkspaceViewModel> logger)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [RelayCommand(CanExecute = nameof(CanCreateWorkspace))]
    private async Task CreateWorkspaceAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Workspace name is required";
                return;
            }

            var workspace = await _databaseService.CreateWorkspaceAsync(Name, Description);
            WorkspaceCreated?.Invoke(workspace);

            // Reset form
            Name = string.Empty;
            Description = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workspace");
            ErrorMessage = $"Failed to create workspace: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateWorkspace()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(Name);
    }

    [RelayCommand]
    private void Cancel()
    {
        WorkspaceCreated?.Invoke(null);
    }

    [RelayCommand]
    private void CloseDialog()
    {
        WorkspaceCreated?.Invoke(null);
    }

    partial void OnNameChanged(string value)
    {
        ErrorMessage = string.Empty;
        CreateWorkspaceCommand.NotifyCanExecuteChanged();
    }
}
