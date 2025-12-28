using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.Network
{
    public partial class RequestCollection : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private ObservableCollection<HttpRequest> _requests = new();

        [ObservableProperty]
        private ObservableCollection<RequestCollection> _subCollections = new();

        [ObservableProperty]
        private string _projectId = string.Empty;

        [ObservableProperty]
        private bool _isExpanded = true;
    }
}
