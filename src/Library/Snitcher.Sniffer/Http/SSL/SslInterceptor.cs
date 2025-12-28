using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Snitcher.Sniffer.Core.Interfaces;

namespace Snitcher.Sniffer.Http.SSL
{
    public class SslInterceptor
    {
        private readonly ICertificateManager _certificateManager;
        private readonly ILogger _logger;

        public SslInterceptor(ICertificateManager certificateManager, ILogger logger)
        {
            _certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(SslStream ClientSsl, SslStream ServerSsl)> InterceptAsync(
        TcpClient client,
        TcpClient server,
        string hostname,
        CancellationToken cancellationToken = default)
        {
            using var clientStream = client.GetStream();
            using var serverStream = server.GetStream();

            // Send 200 Connection Established to client
            string connectResponse = "HTTP/1.1 200 Connection Established\r\n\r\n";
            byte[] responseBytes = Encoding.ASCII.GetBytes(connectResponse);
            await clientStream.WriteAsync(responseBytes, cancellationToken);
            await clientStream.FlushAsync(cancellationToken);

            _logger.LogInfo("Sent 200 Connection Established. Starting TLS interception...");

            // Create SSL stream with client (using generated certificate)
            var clientCert = await _certificateManager.GenerateCertificateForHostAsync(hostname, cancellationToken);
            var clientSslStream = new SslStream(clientStream, false);

            await clientSslStream.AuthenticateAsServerAsync(
                new SslServerAuthenticationOptions
                {
                    ServerCertificate = clientCert,
                    ClientCertificateRequired = false,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, cancellationToken);

            _logger.LogInfo("TLS handshake with client complete.");

            // Create SSL stream with server (normal client TLS)
            var serverSslStream = new SslStream(serverStream, false);

            await serverSslStream.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = hostname,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, cancellationToken);

            _logger.LogInfo("TLS handshake with upstream complete.");

            return (clientSslStream, serverSslStream);
        }

        public async Task BridgeStreamsAsync(SslStream clientStream, SslStream serverStream, CancellationToken cancellationToken = default)
        {
            var clientToServer = clientStream.CopyToAsync(serverStream, cancellationToken);
            var serverToClient = serverStream.CopyToAsync(clientStream, cancellationToken);

            await Task.WhenAny(clientToServer, serverToClient);
        }
    }
}
