using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Interceptors
{
    public class UserAgentInterceptor : IRequestInterceptor
    {
        private readonly string _userAgent;
        private readonly ILogger _logger;

        public UserAgentInterceptor(string userAgent, ILogger logger)
        {
            _userAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 100;

        public async Task<Core.Models.HttpRequestMessage> InterceptAsync(Core.Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
        {
            if (request.Headers.ContainsKey("User-Agent"))
            {
                var originalUserAgent = request.Headers["User-Agent"];
                request.Headers["User-Agent"] = _userAgent;

                _logger.LogInfo("Changed User-Agent from '{0}' to '{1}' for {2}",
                    originalUserAgent, _userAgent, request.Url);
            }

            return await Task.FromResult(request);
        }
    }
}
