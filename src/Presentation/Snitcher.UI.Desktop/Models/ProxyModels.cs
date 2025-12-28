using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;

namespace Snitcher.UI.Desktop.Models
{
    public partial class FlowItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = "";

        [ObservableProperty]
        private string _protocol = "https";

        [ObservableProperty]
        private string _host = "";

        [ObservableProperty]
        private string _path = "";

        [ObservableProperty]
        private string _queryString = "";

        [ObservableProperty]
        private Dictionary<string, string> _urlParameters = new();

        [ObservableProperty]
        private string _fullUrl = "";

        [ObservableProperty]
        private string _method = "";

        [ObservableProperty]
        private int _status;

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private DateTime _requestTime;

        [ObservableProperty]
        private DateTime _responseTime;

        [ObservableProperty]
        private int _duration;

        [ObservableProperty]
        private int _requestSize;

        [ObservableProperty]
        private int _responseSize;

        [ObservableProperty]
        private string _contentType = "";

        [ObservableProperty]
        private string _contentEncoding = "";

        [ObservableProperty]
        private ObservableCollection<ProxyHeaderItem> _requestHeaders = new();

        [ObservableProperty]
        private ObservableCollection<ProxyHeaderItem> _responseHeaders = new();

        [ObservableProperty]
        private string _requestBody = "";

        [ObservableProperty]
        private string _responseBody = "";

        [ObservableProperty]
        private string _clientIp = "127.0.0.1";

        [ObservableProperty]
        private string _serverIp = "";

        public string MethodColor => Method switch
        {
            "GET" => "#49CC90",
            "POST" => "#FCA130",
            "PUT" => "#9012FE",
            "PATCH" => "#50E3C2",
            "DELETE" => "#F93E3E",
            "HEAD" => "#9012FE",
            "OPTIONS" => "#9012FE",
            _ => "#7C7C7C"
        };

        public string StatusColor => Status switch
        {
            >= 200 and < 300 => "#49CC90",
            >= 300 and < 400 => "#FCA130",
            >= 400 and < 500 => "#F93E3E",
            >= 500 => "#D32F2F",
            _ => "#7C7C7C"
        };

        public string FormattedDuration => Duration < 1000 ? $"{Duration}ms" : $"{Duration / 1000.0:F1}s";
        public string FormattedSize => ResponseSize < 1024 ? $"{ResponseSize}B" : ResponseSize < 1024 * 1024 ? $"{ResponseSize / 1024}KB" : $"{ResponseSize / (1024 * 1024):F1}MB";
        public string DisplayUrl => string.IsNullOrEmpty(QueryString) ? Path : $"{Path}?{QueryString}";
    }

    public partial class ProxyHeaderItem : ObservableObject
    {
        [ObservableProperty]
        private string _key = "";

        [ObservableProperty]
        private string _value = "";
    }
}
