using Avalonia.Controls;
using System;
using Snitcher.UI.Desktop.ViewModels;

namespace Snitcher.UI.Desktop.Views
{
    public partial class MainApplicationWindow : Window
    {
        public MainApplicationWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            if (DataContext is MainApplicationWindowViewModel viewModel)
            {
                // Subscribe to window control commands
                viewModel.WindowMinimizeCommand.CanExecuteChanged += OnWindowCommandCanExecuteChanged;
                viewModel.WindowMaximizeCommand.CanExecuteChanged += OnWindowCommandCanExecuteChanged;
                viewModel.WindowCloseCommand.CanExecuteChanged += OnWindowCommandCanExecuteChanged;
            }
        }

        private void OnWindowCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            // Handle window control commands
            if (DataContext is MainApplicationWindowViewModel viewModel)
            {
                if (sender == viewModel.WindowMinimizeCommand)
                {
                    WindowState = WindowState.Minimized;
                }
                else if (sender == viewModel.WindowMaximizeCommand)
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
                else if (sender == viewModel.WindowCloseCommand)
                {
                    Close();
                }
            }
        }
    }
}
