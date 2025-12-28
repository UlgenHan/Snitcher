using Snitcher.Sniffer.Core.Models;
using Snitcher.UI.Desktop.Models;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using HttpRequest = Snitcher.Sniffer.Core.Models.HttpRequestMessage;
using HttpResponse = Snitcher.Sniffer.Core.Models.HttpResponseMessage;

namespace Snitcher.UI.Desktop.Domains.Proxy
{
    public static class FlowMapper
    {
        public static FlowItem MapToFlowItem(Flow flow)
        {
            var flowItem = new FlowItem
            {
                Id = flow.Id.ToString(),
                Protocol = flow.Request.Url.Scheme,
                Host = flow.Request.Url.Host,
                Path = flow.Request.Url.AbsolutePath,
                QueryString = flow.Request.Url.Query,
                FullUrl = flow.Request.Url.ToString(),
                Method = flow.Request.Method.ToString().ToUpper(),
                Status = flow.Response.StatusCode,
                StatusText = flow.Response.ReasonPhrase,
                RequestTime = flow.Timestamp,
                ResponseTime = flow.Timestamp.Add(flow.Duration),
                Duration = (int)flow.Duration.TotalMilliseconds,
                RequestSize = flow.Request.Body.Length,
                ResponseSize = flow.Response.Body.Length,
                ContentType = GetHeaderValue(flow.Request.Headers, "Content-Type") ?? GetHeaderValue(flow.Response.Headers, "Content-Type") ?? "",
                ContentEncoding = GetHeaderValue(flow.Response.Headers, "Content-Encoding") ?? "",
                ClientIp = flow.ClientAddress,
                ServerIp = flow.Request.Url.Host,
                RequestBody = flow.Request.Body.Length > 0 ? Encoding.UTF8.GetString(flow.Request.Body) : "",
                ResponseBody = flow.Response.Body.Length > 0 ? Encoding.UTF8.GetString(flow.Response.Body) : ""
            };

            // Map headers
            flowItem.RequestHeaders = new ObservableCollection<ProxyHeaderItem>(
                flow.Request.Headers.Select(h => new ProxyHeaderItem { Key = h.Key, Value = h.Value }));
            
            flowItem.ResponseHeaders = new ObservableCollection<ProxyHeaderItem>(
                flow.Response.Headers.Select(h => new ProxyHeaderItem { Key = h.Key, Value = h.Value }));

            // Parse URL parameters
            flowItem.UrlParameters = new Dictionary<string, string>();
            var query = flow.Request.Url.Query;
            if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
            {
                var parameters = query.Substring(1).Split('&');
                foreach (var param in parameters)
                {
                    var parts = param.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        flowItem.UrlParameters[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                    }
                }
            }

            return flowItem;
        }

        private static string? GetHeaderValue(Dictionary<string, string> headers, string key)
        {
            return headers.TryGetValue(key, out var value) ? value : null;
        }
    }
}
