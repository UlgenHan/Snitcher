using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using mdDI = Microsoft.Extensions.DependencyInjection;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.Services.Database;

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
            var databaseService = mdDI.ServiceProviderServiceExtensions.GetRequiredService<IDatabaseIntegrationService>(serviceProvider);
            var logger = mdDI.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateWorkspaceViewModel>>(serviceProvider);
            var viewModel = new CreateWorkspaceViewModel(databaseService as DatabaseIntegrationService, logger);
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
