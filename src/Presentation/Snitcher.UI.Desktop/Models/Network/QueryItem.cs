using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.Network
{
    public partial class QueryItem : ObservableObject
    {
        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private bool _enabled = true;
    }
}
