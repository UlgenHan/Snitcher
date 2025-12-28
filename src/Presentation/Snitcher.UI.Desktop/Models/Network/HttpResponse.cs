using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Snitcher.UI.Desktop.Models.Network
{
    public partial class HttpResponse : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private int _statusCode;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<HeaderItem> _headers = new();

        [ObservableProperty]
        private string _body = string.Empty;

        [ObservableProperty]
        private TimeSpan _responseTime;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;
    }
}
