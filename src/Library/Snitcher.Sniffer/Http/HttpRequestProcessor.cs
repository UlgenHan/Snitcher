using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;
using Snitcher.Sniffer.Http.TCP;

namespace Snitcher.Sniffer.Http
{
    public class HttpRequestProcessor
    {
        private readonly TcpConnectionManager _tcpManager;
        private readonly IHttpParser _httpParser;
        private readonly IEnumerable<IRequestInterceptor> _requestInterceptors;
        private readonly IEnumerable<IResponseInterceptor> _responseInterceptors;
        private readonly ILogger _logger;

        public HttpRequestProcessor(
            TcpConnectionManager tcpManager,
            IHttpParser httpParser,
            IEnumerable<IRequestInterceptor> requestInterceptors,
            IEnumerable<IResponseInterceptor> responseInterceptors,
            ILogger logger)
        {
            _tcpManager = tcpManager ?? throw new ArgumentNullException(nameof(tcpManager));
            _httpParser = httpParser ?? throw new ArgumentNullException(nameof(httpParser));
            _requestInterceptors = requestInterceptors ?? throw new ArgumentNullException(nameof(requestInterceptors));
            _responseInterceptors = responseInterceptors ?? throw new ArgumentNullException(nameof(responseInterceptors));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Core.Models.HttpResponseMessage> ProcessRequestAsync(Core.Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
        {
            // Apply request interceptors
            foreach (var interceptor in _requestInterceptors.OrderBy(x => x.Priority))
            {
                request = await interceptor.InterceptAsync(request, flow, cancellationToken);
            }

            // Forward to upstream server
            var response = await ForwardRequestAsync(request, cancellationToken);

            // Apply response interceptors
            foreach (var interceptor in _responseInterceptors.OrderBy(x => x.Priority))
            {
                response = await interceptor.InterceptAsync(response, flow, cancellationToken);
            }

            return response;
        }

        private async Task<Core.Models.HttpResponseMessage> ForwardRequestAsync(Core.Models.HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            var host = request.Url.Host;
            var port = request.Url.Port;

            _logger.LogInfo("Forwarding {0} request to {1}:{2}", request.Method, host, port);

            var server = await _tcpManager.ConnectWithTimeoutAsync(host, port, TimeSpan.FromSeconds(10), cancellationToken);

                using var serverStream = server.GetStream();

            // Send request
            await _httpParser.WriteRequestAsync(request, serverStream, cancellationToken);

            // Read response
            var response = await _httpParser.ParseResponseAsync(serverStream, cancellationToken);

            _logger.LogInfo("Received response: {0} {1}", response.StatusCode, response.ReasonPhrase);

            return response;
        }
    }
}
