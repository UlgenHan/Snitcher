using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Interceptors
{
    public class HeaderInjectorInterceptor : IRequestInterceptor
    {
        private readonly Dictionary<string, string> _headers;
        private readonly ILogger _logger;

        public HeaderInjectorInterceptor(Dictionary<string, string> headers, ILogger logger)
        {
            _headers = headers ?? new Dictionary<string, string>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 200;

        public async Task<Core.Models.HttpRequestMessage> InterceptAsync(Core.Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
        {
            foreach (var header in _headers)
            {
                if (!request.Headers.ContainsKey(header.Key))
                {
                    request.Headers[header.Key] = header.Value;
                    _logger.LogInfo("Added header '{0}: {1}' to request {2}",
                        header.Key, header.Value, request.Url);
                }
            }

            return await Task.FromResult(request);
        }
    }
}
