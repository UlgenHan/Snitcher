using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Models.WorkSpaces;

namespace Snitcher.UI.Desktop.Dialogs;

public partial class CreateWorkspaceDialog : Window
{
    private CreateWorkspaceViewModel ViewModel => (CreateWorkspaceViewModel)DataContext!;

    public CreateWorkspaceDialog()
    {
        InitializeComponent();
        
        // Get ViewModel from DI container
        var serviceProvider = App.ServiceProvider;
        if (serviceProvider != null)
        {
            var databaseService = serviceProvider.GetRequiredService<Services.Database.DatabaseIntegrationService>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateWorkspaceViewModel>>();
            var viewModel = new CreateWorkspaceViewModel(databaseService, logger);
            DataContext = viewModel;
            
            // Subscribe to workspace created event
            viewModel.WorkspaceCreated += OnWorkspaceCreated;
        }
    }

    public CreateWorkspaceDialog(CreateWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to workspace created event after DataContext is set
        if (ViewModel != null)
        {
            ViewModel.WorkspaceCreated += OnWorkspaceCreated;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWorkspaceCreated(Workspace? workspace)
    {
        if (workspace != null)
        {
            Close(workspace);
        }
        else
        {
            Close(null);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Unsubscribe from event
        ViewModel.WorkspaceCreated -= OnWorkspaceCreated;
        base.OnClosing(e);
    }
}
