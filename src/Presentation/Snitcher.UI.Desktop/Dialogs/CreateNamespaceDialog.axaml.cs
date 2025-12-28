using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Models.WorkSpaces;

namespace Snitcher.UI.Desktop.Dialogs;

public partial class CreateNamespaceDialog : Window
{
    private CreateNamespaceViewModel? ViewModel => DataContext as CreateNamespaceViewModel;

    public CreateNamespaceDialog()
    {
        InitializeComponent();
        
        // Get ViewModel from DI container
        var serviceProvider = App.ServiceProvider;
        if (serviceProvider != null)
        {
            var databaseService = serviceProvider.GetRequiredService<Services.Database.DatabaseIntegrationService>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateNamespaceViewModel>>();
            DataContext = new CreateNamespaceViewModel(databaseService, logger);
            
            // Subscribe to namespace created event after DataContext is set
            if (ViewModel != null)
            {
                ViewModel.NamespaceCreated += OnNamespaceCreated;
            }
        }
    }

    public CreateNamespaceDialog(CreateNamespaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to namespace created event after DataContext is set
        if (ViewModel != null)
        {
            ViewModel.NamespaceCreated += OnNamespaceCreated;
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

    private void OnNamespaceCreated(Namespace? namespaceObj)
    {
        if (namespaceObj != null)
        {
            Close(namespaceObj);
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
            ViewModel.NamespaceCreated -= OnNamespaceCreated;
        }
        base.OnClosing(e);
    }
}
