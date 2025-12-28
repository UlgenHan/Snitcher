using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Interceptors
{
    public class StatusCodeInterceptor : IResponseInterceptor
    {
        private readonly Dictionary<int, int> _statusCodeMap;
        private readonly ILogger _logger;

        public StatusCodeInterceptor(Dictionary<int, int> statusCodeMap, ILogger logger)
        {
            _statusCodeMap = statusCodeMap ?? new Dictionary<int, int>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 100;

        public async Task<Core.Models.HttpResponseMessage> InterceptAsync(Core.Models.HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)
        {
            if (_statusCodeMap.TryGetValue(response.StatusCode, out var newStatusCode))
            {
                var originalStatusCode = response.StatusCode;
                response.StatusCode = newStatusCode;

                _logger.LogInfo("Changed status code from {0} to {1} for {2}",
                    originalStatusCode, newStatusCode, flow.Request.Url);
            }

            return await Task.FromResult(response);
        }
    }
}
