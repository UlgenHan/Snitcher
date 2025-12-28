using System.Net.Sockets;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;
using Snitcher.Sniffer.Http.SSL;
using Snitcher.Sniffer.Http.TCP;

namespace Snitcher.Sniffer.Http.ConnectionManagement
{
    public class NetworkConnectionHandler
    {
        private readonly TcpConnectionManager _tcpManager;
        private readonly SslInterceptor _sslInterceptor;
        private readonly IHttpParser _httpParser;
        private readonly ILogger _logger;

        public NetworkConnectionHandler(
            TcpConnectionManager tcpManager,
            SslInterceptor sslInterceptor,
            IHttpParser httpParser,
            ILogger logger)
        {
            _tcpManager = tcpManager ?? throw new ArgumentNullException(nameof(tcpManager));
            _sslInterceptor = sslInterceptor ?? throw new ArgumentNullException(nameof(sslInterceptor));
            _httpParser = httpParser ?? throw new ArgumentNullException(nameof(httpParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleConnectAsync(
            TcpClient client,
            Core.Models.HttpRequestMessage connectRequest,
            Flow flow,
            CancellationToken cancellationToken = default)
        {
            var hostPort = connectRequest.Url.Host;
            var port = connectRequest.Url.Port;

            _logger.LogInfo("CONNECT request for {0}:{1}", hostPort, port);

            // Connect to upstream server
            var server = await _tcpManager.ConnectWithTimeoutAsync(
                hostPort, port, TimeSpan.FromSeconds(10), cancellationToken);

            using (server)
            {
                if (connectRequest.Headers.ContainsKey("Proxy-Connection"))
                {
                    // HTTPS with TLS interception
                    await HandleHttpsInterceptionAsync(client, server, hostPort, flow, cancellationToken);
                }
                else
                {
                    // Simple TCP tunnel
                    await HandleTcpTunnelAsync(client, server, cancellationToken);
                }
            }
        }

        private async Task HandleHttpsInterceptionAsync(
            TcpClient client,
            TcpClient server,
            string hostname,
            Flow flow,
            CancellationToken cancellationToken)
        {
            var (clientSsl, serverSsl) = await _sslInterceptor.InterceptAsync(
                client, server, hostname, cancellationToken);

            using (clientSsl)
            using (serverSsl)
            {
                // Parse the actual HTTP request over TLS
                var request = await _httpParser.ParseRequestAsync(clientSsl, cancellationToken);
                flow.Request = request;

                // TODO: Apply interceptors and forward request
                // For now, just bridge the streams
                await _sslInterceptor.BridgeStreamsAsync(clientSsl, serverSsl, cancellationToken);
            }
        }

        private async Task HandleTcpTunnelAsync(TcpClient client, TcpClient server, CancellationToken cancellationToken)
        {
            using var clientStream = client.GetStream();
            using var serverStream = server.GetStream();

            var clientToServer = clientStream.CopyToAsync(serverStream, cancellationToken);
            var serverToClient = serverStream.CopyToAsync(clientStream, cancellationToken);

            await Task.WhenAny(clientToServer, serverToClient);
        }
    }
}
