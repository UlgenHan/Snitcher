using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.ViewModels;
using Snitcher.UI.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Snitcher.UI.Desktop;

public partial class SnitcherMainWindow : Window
{
    public SnitcherMainWindow()
    {
        InitializeComponent();
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        
        // Update the maximize button icon
        var maximizeButton = this.FindControl<Button>("MaximizeButton");
        if (maximizeButton != null)
        {
            var icon = maximizeButton.Content as FluentIcons.Avalonia.FluentIcon;
            if (icon != null)
            {
                icon.Icon = WindowState == WindowState.Maximized ? 
                    FluentIcons.Common.Icon.SquareMultiple : FluentIcons.Common.Icon.Maximize;
            }
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Handle double-click to maximize/restore
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            
            // Update the maximize button icon
            var maximizeButton = this.FindControl<Button>("MaximizeButton");
            if (maximizeButton != null)
            {
                var icon = maximizeButton.Content as FluentIcons.Avalonia.FluentIcon;
                if (icon != null)
                {
                    icon.Icon = WindowState == WindowState.Maximized ? 
                        FluentIcons.Common.Icon.SquareMultiple : FluentIcons.Common.Icon.Maximize;
                }
            }
            return;
        }
        
        // Handle single click for dragging
        if (e.Source is Control source && source.Name != "MinimizeButton" && 
            source.Name != "MaximizeButton" && source.Name != "CloseButton")
        {
            BeginMoveDrag(e);
        }
    }

    private void OnWorkspacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Workspace workspace)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            viewModel?.OpenWorkspace(workspace);
        }
    }

    private void OnProjectPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Project project)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            viewModel?.OpenProject(project);
            
            // Open the main application window with full UI
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                var appWindow = new MainApplicationWindow
                {
                    DataContext = serviceProvider.GetRequiredService<MainApplicationWindowViewModel>()
                };
                appWindow.Show();
            }
            
            // Hide main window instead of closing it
            this.Close();
        }
    }

    private void OnProjectButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Project project)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            viewModel?.OpenProject(project);
              
            // Hide main window instead of closing it
            // this.Hide();
            // For now we will close it
            this.Close();
        }
    }

    private void OnWorkspacePointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Avalonia.Media.Brush.Parse("#333333");
        }
    }

    private void OnWorkspacePointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Avalonia.Media.Brush.Parse("#282828");
        }
    }

    private void OnProjectPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Avalonia.Media.Brush.Parse("#333333");
        }
    }

    private void OnProjectPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = Avalonia.Media.Brush.Parse("#282828");
        }
    }

    private async void OnCreateWorkspacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as SnitcherMainViewModel;
        if (viewModel != null)
        {
            await viewModel.CreateWorkspaceCommand.ExecuteAsync(null);
        }
    }

    private async void OnCreateProjectPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as SnitcherMainViewModel;
        if (viewModel != null)
        {
            await viewModel.CreateProjectCommand.ExecuteAsync(null);
        }
    }

    private async void OnDeleteWorkspaceClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Workspace workspace)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            if (viewModel != null)
            {
                await viewModel.DeleteWorkspaceCommand.ExecuteAsync(workspace);
            }
        }
    }

    private async void OnDeleteProjectClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Project project)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            if (viewModel != null)
            {
                await viewModel.DeleteProjectCommand.ExecuteAsync(project);
            }
        }
    }

    private async void OnSearchKeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var viewModel = DataContext as SnitcherMainViewModel;
            if (viewModel != null)
            {
                await viewModel.SearchCommand.ExecuteAsync(null);
            }
        }
    }

    private void OnRefreshClicked(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = DataContext as SnitcherMainViewModel;
        if (viewModel != null)
        {
            _ = viewModel.RefreshDataCommand.ExecuteAsync(null);
        }
    }
}