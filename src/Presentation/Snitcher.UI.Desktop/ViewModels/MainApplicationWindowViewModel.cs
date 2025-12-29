using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Microsoft.Extensions.DependencyInjection;
using Snitcher.UI.Desktop.Domains.Proxy;
using Snitcher.UI.Desktop.Domains.RequestBuilder;
using Snitcher.UI.Desktop.Domains.Automation;
using Snitcher.UI.Desktop.Domains.Collections;
using Snitcher.UI.Desktop.Domains.Workspace;

namespace Snitcher.UI.Desktop.ViewModels
{
    public partial class MainApplicationWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ViewModelBase _currentViewModel;

        [ObservableProperty]
        private string _currentViewTitle = "Welcome";

        [ObservableProperty]
        private string _currentViewDescription = "Get started with your API development journey";

        [ObservableProperty]
        private bool _isSidebarExpanded = false;


        public MainApplicationWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Start with the Welcome view
            CurrentViewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
            UpdateViewInfo("Welcome", "Get started with your API development journey");
        }

        private void UpdateViewInfo(string title, string description)
        {
            CurrentViewTitle = title;
            CurrentViewDescription = description;
        }

        [RelayCommand]
        private void NavigateToWelcome()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
            UpdateViewInfo("Welcome", "Get started with your API development journey");
        }

        [RelayCommand]
        private void NavigateToRequestBuilder()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<RequestBuilderViewModel>();
            UpdateViewInfo("Request Builder", "Create and test HTTP requests with powerful tools");
        }

        [RelayCommand]
        private void NavigateToCollections()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<CollectionsExplorerViewModel>();
            UpdateViewInfo("Collections", "Organize and manage your API requests");
        }

        [RelayCommand]
        private void NavigateToProxyInspector()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<ProxyInspectorViewModel>();
            UpdateViewInfo("Proxy Inspector", "Monitor and debug HTTP traffic");
        }

        [RelayCommand]
        private void NavigateToAutomation()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<AutomationWorkflowViewModel>();
            UpdateViewInfo("Automation Workflow", "Automate your API testing workflows");
        }

        [RelayCommand]
        private void NavigateToExtensions()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<ExtensionsViewModel>();
            UpdateViewInfo("Extensions", "Extend functionality with plugins");
        }

        [RelayCommand]
        private void NavigateToWorkspaceManager()
        {
            CurrentViewModel = _serviceProvider.GetRequiredService<WorkspaceManagerViewModel>();
            UpdateViewInfo("Workspace Manager", "Manage your workspaces and projects");
        }

        [RelayCommand]
        private void WindowMinimize()
        {
            // Trigger the command to notify the view
            WindowMinimizeCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void WindowMaximize()
        {
            // Trigger the command to notify the view
            WindowMaximizeCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void WindowClose()
        {
            // Trigger the command to notify the view
            WindowCloseCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }
    }
}
