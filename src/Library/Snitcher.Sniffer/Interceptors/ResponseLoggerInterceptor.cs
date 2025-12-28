using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Interceptors
{
    public class ResponseLoggerInterceptor : IResponseInterceptor
    {
        private readonly ILogger _logger;

        public ResponseLoggerInterceptor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 1000; // Run last

        public async Task<Core.Models.HttpResponseMessage> InterceptAsync(Core.Models.HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("Response: {0} {1} - Content-Type: {2}, Content-Length: {3}",
                response.StatusCode,
                response.ReasonPhrase,
                response.Headers.GetValueOrDefault("Content-Type", "unknown"),
                response.Headers.GetValueOrDefault("Content-Length", "unknown"));

            // Log response body if it's text content and not too large
            if (response.Body.Length > 0 && response.Body.Length < 1024 * 10) // Less than 10KB
            {
                var contentType = response.Headers.GetValueOrDefault("Content-Type", "");
                if (contentType.Contains("text/") || contentType.Contains("json") || contentType.Contains("xml"))
                {
                    var bodyText = Encoding.UTF8.GetString(response.Body);
                    _logger.LogInfo("Response body: {0}", bodyText);
                }
            }

            return await Task.FromResult(response);
        }
    }
}
