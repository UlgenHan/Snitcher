using System.Net.Sockets;
using System.Net;
using Snitcher.Sniffer.Core.Interfaces;

namespace Snitcher.Sniffer.Http.TCP
{
    public class TcpConnectionManager
    {
        private readonly ILogger _logger;

        public TcpConnectionManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TcpClient> ConnectWithTimeoutAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                _logger.LogInfo("Resolving {0} (IPv4 only)...", host);
                var addresses = await Dns.GetHostAddressesAsync(host, cts.Token);
                var ipv4Addresses = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToArray();

                if (ipv4Addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.SocketError, $"No IPv4 addresses found for {host}");
                }

                foreach (var address in ipv4Addresses)
                {
                    try
                    {
                        _logger.LogInfo("Trying {0}:{1}...", address, port);
                        var tcpClient = new TcpClient();
                        await tcpClient.ConnectAsync(address, port);
                        _logger.LogInfo("Connected to {0}:{1}", address, port);
                        return tcpClient;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to connect to {0}:{1} - {2}", address, port, ex.Message);
                        continue;
                    }
                }

                throw new SocketException((int)SocketError.SocketError, $"Could not connect to any address for {host}:{port}");
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Connection to {host}:{port} timed out after {timeout.TotalSeconds}s");
            }
        }
    }
}

