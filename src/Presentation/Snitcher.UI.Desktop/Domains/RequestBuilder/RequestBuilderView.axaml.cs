using Avalonia.Controls;
using Snitcher.UI.Desktop.Models;
using Snitcher.UI.Desktop.Domains.RequestBuilder;

namespace Snitcher.UI.Desktop.Domains.RequestBuilder
{
    public partial class RequestBuilderView : UserControl
    {
        public RequestBuilderView()
        {
            InitializeComponent();
        }

        private void RemoveParameter_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HttpParameter parameter)
            {
                var viewModel = DataContext as RequestBuilderViewModel;
                viewModel?.RemoveParameter(parameter);
            }
        }

        private void RemoveHeader_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HttpHeader header)
            {
                var viewModel = DataContext as RequestBuilderViewModel;
                viewModel?.RemoveHeader(header);
            }
        }

        private void RemoveTest_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is HttpTest test)
            {
                var viewModel = DataContext as RequestBuilderViewModel;
                viewModel?.RemoveTest(test);
            }
        }
    }
}
