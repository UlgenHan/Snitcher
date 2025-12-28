using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Interceptors
{
    public class InterceptorManager
    {
        private readonly IEnumerable<IRequestInterceptor> _requestInterceptors;
        private readonly IEnumerable<IResponseInterceptor> _responseInterceptors;
        private readonly ILogger _logger;

        public InterceptorManager(
            IEnumerable<IRequestInterceptor> requestInterceptors,
            IEnumerable<IResponseInterceptor> responseInterceptors,
            ILogger logger)
        {
            _requestInterceptors = requestInterceptors ?? throw new ArgumentNullException(nameof(requestInterceptors));
            _responseInterceptors = responseInterceptors ?? throw new ArgumentNullException(nameof(responseInterceptors));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Core.Models.HttpRequestMessage> ApplyRequestInterceptorsAsync(
            Core.Models.HttpRequestMessage request,
            Flow flow,
            CancellationToken cancellationToken = default)
        {
            var currentRequest = request;

            foreach (var interceptor in _requestInterceptors.OrderBy(x => x.Priority))
            {
                try
                {
                    _logger.LogInfo("Applying request interceptor: {0}", interceptor.GetType().Name);
                    currentRequest = await interceptor.InterceptAsync(currentRequest, flow, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Request interceptor {0} failed", interceptor.GetType().Name);
                    // Continue with other interceptors
                }
            }

            return currentRequest;
        }

        public async Task<Core.Models.HttpResponseMessage> ApplyResponseInterceptorsAsync(
            Core.Models.HttpResponseMessage response,
            Flow flow,
            CancellationToken cancellationToken = default)
        {
            var currentResponse = response;

            foreach (var interceptor in _responseInterceptors.OrderBy(x => x.Priority))
            {
                try
                {
                    _logger.LogInfo("Applying response interceptor: {0}", interceptor.GetType().Name);
                    currentResponse = await interceptor.InterceptAsync(currentResponse, flow, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Response interceptor {0} failed", interceptor.GetType().Name);
                    // Continue with other interceptors
                }
            }

            return currentResponse;
        }
    }
}
