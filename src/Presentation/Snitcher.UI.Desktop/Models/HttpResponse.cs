using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace Snitcher.UI.Desktop.Models
{
    public partial class HttpResponse : ObservableObject
    {
        [ObservableProperty]
        private int _statusCode;

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private long _responseTime;

        [ObservableProperty]
        private long _contentLength;

        [ObservableProperty]
        private string _body = "";

        [ObservableProperty]
        private ObservableCollection<HttpHeader> _headers = new();

        [ObservableProperty]
        private string _contentType = "";

        [ObservableProperty]
        private bool _isSuccess;

        [ObservableProperty]
        private string _error = "";

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;

        public HttpResponse()
        {
        }

        public HttpResponse(int statusCode, string statusText, string body, IEnumerable<HttpHeader> headers, long responseTime)
        {
            StatusCode = statusCode;
            StatusText = statusText;
            Body = body;
            Headers = new ObservableCollection<HttpHeader>(headers);
            ResponseTime = responseTime;
            ContentLength = body?.Length ?? 0;
            ContentType = GetContentTypeFromHeaders();
            IsSuccess = statusCode >= 200 && statusCode < 300;
            Timestamp = DateTime.Now;
        }

        public static HttpResponse CreateError(string error, long responseTime = 0)
        {
            return new HttpResponse
            {
                StatusCode = 0,
                StatusText = "Error",
                Error = error,
                ResponseTime = responseTime,
                IsSuccess = false,
                Timestamp = DateTime.Now
            };
        }

        public Dictionary<string, string> GetHeadersDictionary()
        {
            return Headers.Where(h => !string.IsNullOrWhiteSpace(h.Key) && !string.IsNullOrWhiteSpace(h.Value))
                          .ToDictionary(h => h.Key, h => h.Value);
        }

        private string GetContentTypeFromHeaders()
        {
            var contentTypeHeader = Headers?.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
            return contentTypeHeader?.Value ?? "";
        }

        public bool IsJson()
        {
            return (ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public bool IsXml()
        {
            return (ContentType?.Contains("application/xml", StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (ContentType?.Contains("text/xml", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public bool IsHtml()
        {
            return ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public bool IsText()
        {
            return ContentType?.Contains("text/", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public T? TryParseJson<T>()
        {
            if (!IsJson() || string.IsNullOrWhiteSpace(Body))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(Body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return default;
            }
        }

        public string GetFormattedBody()
        {
            if (string.IsNullOrWhiteSpace(Body))
                return "";

            try
            {
                if (IsJson())
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(Body);
                    return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                }

                if (IsXml())
                {
                    // Simple XML formatting - could be enhanced with proper XML formatter
                    return Body;
                }

                return Body;
            }
            catch
            {
                return Body;
            }
        }

        public string GetSizeDisplay()
        {
            if (ContentLength < 1024)
                return $"{ContentLength} B";
            if (ContentLength < 1024 * 1024)
                return $"{ContentLength / 1024.0:F1} KB";
            return $"{ContentLength / (1024.0 * 1024.0):F1} MB";
        }

        public string GetResponseTimeDisplay()
        {
            if (ResponseTime < 1000)
                return $"{ResponseTime} ms";
            return $"{ResponseTime / 1000.0:F1} s";
        }
    }

    public partial class HttpResponseHistory : ObservableObject
    {
        [ObservableProperty]
        private HttpResponse _response;

        [ObservableProperty]
        private HttpRequest _request;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;

        [ObservableProperty]
        private string _name = "";

        public HttpResponseHistory(HttpResponse response, HttpRequest request)
        {
            Response = response;
            Request = request;
            Name = $"{request.Method} {request.Url}";
        }
    }
}
