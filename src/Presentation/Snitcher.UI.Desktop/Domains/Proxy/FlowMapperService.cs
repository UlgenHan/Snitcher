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
    public class FlowMapperService : IFlowMapper
    {
        public FlowItem MapToFlowItem(Flow flow)
        {
            var flowItem = new FlowItem
            {
                Id = flow.Id.ToString(),
                Protocol = flow.Request.Url.Scheme,
                Host = flow.Request.Url.Host,
                Path = flow.Request.Url.AbsolutePath,
                QueryString = flow.Request.Url.Query ?? "",
                FullUrl = flow.Request.Url.ToString(),
                Method = flow.Request.Method,
                Status = flow.Response.StatusCode,
                StatusText = flow.Response.ReasonPhrase,
                Duration = (int)flow.Duration.TotalMilliseconds,
                RequestTime = flow.Timestamp,
                ResponseTime = flow.Timestamp.Add(flow.Duration),
                RequestSize = flow.Request.Body.Length,
                ResponseSize = flow.Response.Body.Length,
                ClientIp = flow.ClientAddress,
                ServerIp = flow.Request.Url.Host
            };

            // Map headers to ObservableCollection<ProxyHeaderItem>
            flowItem.RequestHeaders = new ObservableCollection<ProxyHeaderItem>(
                flow.Request.Headers.Select(h => new ProxyHeaderItem { Key = h.Key, Value = h.Value }));
            flowItem.ResponseHeaders = new ObservableCollection<ProxyHeaderItem>(
                flow.Response.Headers.Select(h => new ProxyHeaderItem { Key = h.Key, Value = h.Value }));

            // Map body content
            if (flow.Request.Body.Length > 0)
            {
                flowItem.RequestBody = Encoding.UTF8.GetString(flow.Request.Body);
            }

            if (flow.Response.Body.Length > 0)
            {
                flowItem.ResponseBody = Encoding.UTF8.GetString(flow.Response.Body);
            }

            // Extract URL parameters
            flowItem.UrlParameters = ExtractUrlParameters(flow.Request.Url);

            return flowItem;
        }

        private Dictionary<string, string> ExtractUrlParameters(Uri url)
        {
            var parameters = new Dictionary<string, string>();
            var query = url.Query;
            if (string.IsNullOrEmpty(query))
                return parameters;

            // Remove the '?' and split parameters
            var queryString = query.TrimStart('?');
            var pairs = queryString.Split('&');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    parameters[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
                }
                else if (keyValue.Length == 1)
                {
                    parameters[Uri.UnescapeDataString(keyValue[0])] = string.Empty;
                }
            }

            return parameters;
        }
    }
}
