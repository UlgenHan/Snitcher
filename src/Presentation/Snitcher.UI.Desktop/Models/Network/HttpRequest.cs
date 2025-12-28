using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.Network
{
    public partial class HttpRequest : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _method = "GET";

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private ObservableCollection<HeaderItem> _headers = new();

        [ObservableProperty]
        private ObservableCollection<QueryItem> _queryParams = new();

        [ObservableProperty]
        private string _body = string.Empty;

        [ObservableProperty]
        private string _contentType = "application/json";

        [ObservableProperty]
        private DateTime _createdAt = DateTime.Now;
    }
}
