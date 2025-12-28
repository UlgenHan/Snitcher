using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.Services.Database;

namespace Snitcher.UI.Desktop.Dialogs;

public partial class CreateProjectDialog : Window
{
    private CreateProjectViewModel? ViewModel => DataContext as CreateProjectViewModel;

    public CreateProjectDialog()
    {
        InitializeComponent();
        
        // Get ViewModel from DI container
        var serviceProvider = App.ServiceProvider;
        if (serviceProvider != null)
        {
            var databaseService = serviceProvider.GetRequiredService<Services.Database.IDatabaseIntegrationService>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateProjectViewModel>>();
            var viewModel = new CreateProjectViewModel(databaseService as DatabaseIntegrationService, logger);
            DataContext = viewModel;
            
            // Subscribe to project created event
            viewModel.ProjectCreated += OnProjectCreated;
        }
    }

    public CreateProjectDialog(CreateProjectViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to project created event after DataContext is set
        if (ViewModel != null)
        {
            ViewModel.ProjectCreated += OnProjectCreated;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (ViewModel != null)
        {
            await ViewModel.InitializeAsync();
        }
    }

    private void OnProjectCreated(Project? project)
    {
        if (project != null)
        {
            Close(project);
        }
        else
        {
            Close(null);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Unsubscribe from event if ViewModel exists
        if (ViewModel != null)
        {
            ViewModel.ProjectCreated -= OnProjectCreated;
        }
        base.OnClosing(e);
    }
}
