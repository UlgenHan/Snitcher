using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;
using System.Text;

namespace Snitcher.Sniffer.Core.Services
{
    public class ConnectionHandler : IConnectionHandler
    {
        private readonly IHttpParser _httpParser;
        private readonly ICertificateManager _certificateManager;
        private readonly IFlowStorage _flowStorage;
        private readonly IEnumerable<IRequestInterceptor> _requestInterceptors;
        private readonly IEnumerable<IResponseInterceptor> _responseInterceptors;
        private readonly ILogger _logger;

        public event EventHandler<FlowEventArgs>? FlowCaptured;

        public ConnectionHandler(
           IHttpParser httpParser,
           ICertificateManager certificateManager,
           IFlowStorage flowStorage,
           IEnumerable<IRequestInterceptor> requestInterceptors,
           IEnumerable<IResponseInterceptor> responseInterceptors,
           ILogger logger)
        {
            _httpParser = httpParser ?? throw new ArgumentNullException(nameof(httpParser));
            _certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
            _flowStorage = flowStorage ?? throw new ArgumentNullException(nameof(flowStorage));
            _requestInterceptors = requestInterceptors ?? throw new ArgumentNullException(nameof(requestInterceptors));
            _responseInterceptors = responseInterceptors ?? throw new ArgumentNullException(nameof(responseInterceptors));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken = default)
        {
            var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            var flow = new Flow { ClientAddress = clientEndPoint };

            try
            {
                using var clientStream = client.GetStream();

                // Read the initial request
                var request = await _httpParser.ParseRequestAsync(clientStream, cancellationToken);
                flow.Request = request;

                // Apply request interceptors
                foreach (var interceptor in _requestInterceptors.OrderBy(x => x.Priority))
                {
                    request = await interceptor.InterceptAsync(request, flow, cancellationToken);
                }

                // Handle CONNECT (HTTPS) vs regular HTTP
                if (request.Method.ToString() == "CONNECT")
                {
                    await HandleHttpsConnectAsync(clientStream, request, flow, cancellationToken);
                }
                else
                {
                    await HandleHttpRequestAsync(clientStream, request, flow, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                flow.Status = FlowStatus.Failed;
                _logger.LogError(ex, "Error handling connection from {0}", clientEndPoint);
            }
            finally
            {
                flow.Status = flow.Response.StatusCode > 0 ? FlowStatus.Completed : FlowStatus.Failed;
                flow.Duration = DateTime.UtcNow - flow.Timestamp;

                await _flowStorage.StoreFlowAsync(flow, cancellationToken);
                _logger.LogFlow(flow);
                
                // Raise the flow captured event
                FlowCaptured?.Invoke(this, new FlowEventArgs(flow));
            }
        }

        private async Task HandleHttpsConnectAsync(NetworkStream clientStream, Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken)
        {
            // Extract host and port from CONNECT request
            var connectTarget = request.Url.Host;
            var connectPort = request.Url.Port == 0 ? 443 : request.Url.Port;

            try
            {
                _logger.LogInfo("HTTPS CONNECT to {0}:{1} - Setting up tunnel", connectTarget, connectPort);

                // Connect to target server
                using var targetClient = new TcpClient();
                await targetClient.ConnectAsync(connectTarget, connectPort, cancellationToken);
                using var targetStream = targetClient.GetStream();

                // Send 200 Connection established response to client
                var connectResponse = "HTTP/1.1 200 Connection established\r\n\r\n";
                var connectBytes = Encoding.UTF8.GetBytes(connectResponse);
                await clientStream.WriteAsync(connectBytes, cancellationToken);
                await clientStream.FlushAsync(cancellationToken);

                _logger.LogInfo("✅ HTTPS tunnel established for {0}:{1}", connectTarget, connectPort);

                // Simple bidirectional tunneling
                var clientToTarget = CopyStreamAsync(clientStream, targetStream, cancellationToken);
                var targetToClient = CopyStreamAsync(targetStream, clientStream, cancellationToken);

                // Wait for either direction to complete
                await Task.WhenAny(clientToTarget, targetToClient);

                _logger.LogInfo("✅ HTTPS tunnel completed for {0}:{1}", connectTarget, connectPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HTTPS CONNECT to {0}:{1}", connectTarget, connectPort);
                flow.Response = new Models.HttpResponseMessage
                {
                    StatusCode = 502,
                    ReasonPhrase = "Bad Gateway",
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "text/plain" },
                        { "Proxy-Error", "true" }
                    },
                    Body = Encoding.UTF8.GetBytes($"Proxy Error: Failed to establish HTTPS tunnel to {connectTarget}:{connectPort}\nError: {ex.Message}")
                };
                throw;
            }
        }

        private async Task InterceptHttpsTrafficAsync(SslStream clientSslStream, SslStream targetSslStream, Flow flow, string targetHost, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo("🔍 Starting HTTPS traffic interception for {0}", targetHost);

                // Parse the actual HTTPS request from client
                Models.HttpRequestMessage httpsRequest;
                try
                {
                    httpsRequest = await _httpParser.ParseRequestAsync(clientSslStream, cancellationToken);
                    flow.Request = httpsRequest;
                    _logger.LogInfo("🔍 Intercepted HTTPS request: {0} {1}", httpsRequest.Method, httpsRequest.Url);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse HTTPS request from client for {0}", targetHost);
                    throw;
                }

                // Apply request interceptors
                foreach (var interceptor in _requestInterceptors.OrderBy(x => x.Priority))
                {
                    httpsRequest = await interceptor.InterceptAsync(httpsRequest, flow, cancellationToken);
                }

                // Forward request to target server
                try
                {
                    await SendRequestAsync(targetSslStream, httpsRequest, cancellationToken);
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Failed to forward HTTPS request to target {0}", targetHost);
                    throw;
                }

                // Parse response from target server
                Models.HttpResponseMessage httpsResponse;
                try
                {
                    httpsResponse = await _httpParser.ParseResponseAsync(targetSslStream, cancellationToken);
                    flow.Response = httpsResponse;
                    _logger.LogInfo("🔍 Intercepted HTTPS response: {0} {1}", httpsResponse.StatusCode, httpsResponse.ReasonPhrase);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "Failed to parse HTTPS response from target {0}", targetHost);
                    throw;
                }

                // Apply response interceptors
                foreach (var interceptor in _responseInterceptors.OrderBy(x => x.Priority))
                {
                    httpsResponse = await interceptor.InterceptAsync(httpsResponse, flow, cancellationToken);
                }

                // Send response back to client
                try
                {
                    await SendResponseAsync(clientSslStream, httpsResponse, cancellationToken);
                }
                catch (Exception sendEx)
                {
                    _logger.LogError(sendEx, "Failed to send HTTPS response to client for {0}", targetHost);
                    throw;
                }

                _logger.LogInfo("✅ HTTPS request/response intercepted and forwarded for {0}", targetHost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error intercepting HTTPS traffic for {0}", targetHost);
                throw;
            }
        }

        private async Task CopyStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }
        }

        private async Task HandleHttpRequestAsync(NetworkStream clientStream, Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken)
        {
            try
            {
                var targetHost = request.Url.Host;
                var targetPort = request.Url.Port == 0 ? 80 : request.Url.Port;

                _logger.LogInfo("HTTP request to {0}:{1}", targetHost, targetPort);

                // Forward request to target and get response
                var response = await ForwardRequestToTarget(request, targetHost, targetPort, false, cancellationToken);
                flow.Response = response;

                // Send response back to client
                await SendResponseAsync(clientStream, response, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HTTP request");
                throw;
            }
        }

        private async Task<Models.HttpResponseMessage> ForwardRequestToTarget(Models.HttpRequestMessage request, string host, int port, bool useSsl, CancellationToken cancellationToken)
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            Stream stream;
            if (useSsl)
            {
                var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, certificate, chain, errors) => true);
                await sslStream.AuthenticateAsClientAsync(host);
                stream = sslStream;
            }
            else
            {
                stream = tcpClient.GetStream();
            }

            // Send request
            await SendRequestAsync(stream, request, cancellationToken);

            // Parse response
            return await _httpParser.ParseResponseAsync(stream, cancellationToken);
        }

        private async Task SendRequestAsync(Stream stream, Models.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestLine = $"{request.Method} {request.Url.PathAndQuery} HTTP/1.1\r\n";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(requestLine), cancellationToken);

            // Ensure Host header is set
            if (!request.Headers.ContainsKey("Host"))
            {
                var hostHeader = $"{request.Url.Host}:{request.Url.Port}";
                var hostLine = $"Host: {hostHeader}\r\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(hostLine), cancellationToken);
            }

            // Send headers
            foreach (var header in request.Headers)
            {
                var headerLine = $"{header.Key}: {header.Value}\r\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(headerLine), cancellationToken);
            }

            // Send empty line to end headers
            await stream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), cancellationToken);

            // Send body if present
            if (request.Body.Length > 0)
            {
                await stream.WriteAsync(request.Body, cancellationToken);
            }

            await stream.FlushAsync(cancellationToken);
        }

        private async Task SendResponseAsync(Stream stream, Models.HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                // Send status line
                var statusLine = $"HTTP/1.1 {response.StatusCode} {response.ReasonPhrase}\r\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(statusLine), cancellationToken);

                // Ensure Content-Length header is present if there's a body
                if (response.Body.Length > 0 && !response.Headers.ContainsKey("Content-Length"))
                {
                    response.Headers["Content-Length"] = response.Body.Length.ToString();
                }

                // Add Connection header if not present
                if (!response.Headers.ContainsKey("Connection"))
                {
                    response.Headers["Connection"] = "close";
                }

                // Send headers
                foreach (var header in response.Headers)
                {
                    var headerLine = $"{header.Key}: {header.Value}\r\n";
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(headerLine), cancellationToken);
                }

                // Send empty line to end headers
                await stream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), cancellationToken);

                // Send body if present
                if (response.Body.Length > 0)
                {
                    await stream.WriteAsync(response.Body, cancellationToken);
                }

                await stream.FlushAsync(cancellationToken);
                
                _logger.LogInfo("✅ Response sent: {0} {1}, Body: {2} bytes", 
                    response.StatusCode, response.ReasonPhrase, response.Body.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send response to client");
                throw;
            }
        }
    }
}
