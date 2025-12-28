using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snitcher.Sniffer.Interceptors
{
    public static class InterceptorExtensions
    {
        public static string GetValueOrDefault(this Dictionary<string, string> headers, string key, string defaultValue = "")
        {
            return headers.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static bool IsTextContent(this Core.Models.HttpResponseMessage response)
        {
            var contentType = response.Headers.GetValueOrDefault("Content-Type", "").ToLowerInvariant();
            return contentType.Contains("text/") ||
                   contentType.Contains("json") ||
                   contentType.Contains("xml") ||
                   contentType.Contains("javascript");
        }

        public static bool IsSuccess(this Core.Models.HttpResponseMessage response)
        {
            return response.StatusCode >= 200 && response.StatusCode < 300;
        }
    }
}
