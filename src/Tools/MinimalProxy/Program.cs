using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MinimalProxy
{
    class Program
    {
        private static ILogger? _logger;

        static async Task Main(string[] args)
        {
            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<Program>();

            Console.WriteLine("🔧 Minimal HTTPS Proxy Test");
            Console.WriteLine("==========================");

            var proxy = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
            
            try
            {
                proxy.Start();
                Console.WriteLine("✅ Proxy started on http://localhost:8080");
                Console.WriteLine("📝 Test with: curl -x http://localhost:8080 --insecure https://httpbin.org/get");
                Console.WriteLine("⏸️  Press ENTER to stop...\n");

                while (true)
                {
                    var client = await proxy.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Proxy error");
            }
            finally
            {
                proxy.Stop();
            }
        }

        static async Task HandleClient(TcpClient client)
        {
            var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            _logger!.LogInformation("🔗 New connection from {Client}", clientEndPoint);

            try
            {
                using var clientStream = client.GetStream();
                
                // Read the HTTP request
                var buffer = new byte[4096];
                var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                _logger.LogInformation("📨 Request:\n{Request}", request.Split('\n')[0]);

                // Parse the request
                var lines = request.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return;

                var requestLine = lines[0];
                var parts = requestLine.Split(' ');

                if (parts.Length >= 2 && parts[0].ToUpper() == "CONNECT")
                {
                    // Handle HTTPS CONNECT
                    await HandleConnect(clientStream, parts[1], clientEndPoint);
                }
                else if (parts.Length >= 3)
                {
                    // Handle HTTP GET/POST
                    await HandleHttp(clientStream, request, clientEndPoint);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error handling client {Client}", clientEndPoint);
            }
            finally
            {
                client.Close();
            }
        }

        static async Task HandleConnect(NetworkStream clientStream, string target, string clientEndPoint)
        {
            try
            {
                // Parse target host:port
                var targetParts = target.Split(':');
                var host = targetParts[0];
                var port = int.Parse(targetParts[1]);

                _logger!.LogInformation("🔒 HTTPS CONNECT to {Host}:{Port}", host, port);

                // Connect to target
                using var targetClient = new TcpClient();
                await targetClient.ConnectAsync(host, port);
                using var targetStream = targetClient.GetStream();

                // Send 200 Connection established
                var response = "HTTP/1.1 200 Connection established\r\n\r\n";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);

                _logger!.LogInformation("✅ Tunnel established for {Client}", clientEndPoint);

                // Create tunnel - copy data both ways
                var clientToTarget = CopyStreamAsync(clientStream, targetStream);
                var targetToClient = CopyStreamAsync(targetStream, clientStream);

                await Task.WhenAny(clientToTarget, targetToClient);

                _logger!.LogInformation("🔚 Tunnel closed for {Client}", clientEndPoint);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CONNECT failed for {Client}", clientEndPoint);
                
                // Send error response
                var errorResponse = "HTTP/1.1 502 Bad Gateway\r\n\r\n";
                var errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                await clientStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
        }

        static async Task HandleHttp(NetworkStream clientStream, string request, string clientEndPoint)
        {
            try
            {
                // Simple HTTP response for testing
                var response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nProxy HTTP response";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);

                _logger!.LogInformation("📡 HTTP response sent to {Client}", clientEndPoint);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "HTTP failed for {Client}", clientEndPoint);
            }
        }

        static async Task CopyStreamAsync(Stream source, Stream destination)
        {
            var buffer = new byte[8192];
            int bytesRead;

            try
            {
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogDebug(ex, "Stream copy ended");
            }
        }
    }
}
