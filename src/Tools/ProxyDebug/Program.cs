using Microsoft.Extensions.Logging;
using Snitcher.Sniffer.Core.Services;
using Snitcher.Sniffer.Core.Models;
using Snitcher.Sniffer.Http;
using Snitcher.Sniffer.Certificates;
using Snitcher.Sniffer.Core.Interfaces;
using System.Net;

namespace ProxyDebug
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Snitcher Proxy Debug Tool");
            Console.WriteLine("=============================");

            // Setup detailed logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                // Create proxy components
                var proxyLogger = loggerFactory.CreateLogger<ProxyServer>();
                var snifferLogger = new SnifferLoggerAdapter(proxyLogger);
                
                var httpParser = new HttpParser(snifferLogger);
                var certificateManager = new CertificateManager(snifferLogger);
                var flowStorage = new InMemoryFlowStorage();
                var requestInterceptors = new List<IRequestInterceptor>();
                var responseInterceptors = new List<IResponseInterceptor>();

                var connectionHandler = new ConnectionHandler(
                    httpParser, 
                    certificateManager, 
                    flowStorage, 
                    requestInterceptors, 
                    responseInterceptors, 
                    snifferLogger);

                var proxyServer = new ProxyServer(connectionHandler, snifferLogger);

                // Subscribe to flow events with detailed logging
                int flowCount = 0;
                proxyServer.FlowCaptured += (sender, e) =>
                {
                    flowCount++;
                    var flow = e.Flow;
                    Console.WriteLine($"\n🔍 FLOW #{flowCount} DETECTED:");
                    Console.WriteLine($"   Timestamp: {flow.Timestamp:HH:mm:ss.fff}");
                    Console.WriteLine($"   Client: {flow.ClientAddress}");
                    Console.WriteLine($"   Method: {flow.Request.Method}");
                    Console.WriteLine($"   URL: {flow.Request.Url}");
                    Console.WriteLine($"   HTTP Version: {flow.Request.HttpVersion}");
                    Console.WriteLine($"   Headers Count: {flow.Request.Headers?.Count ?? 0}");
                    Console.WriteLine($"   Body Length: {flow.Request.Body?.Length ?? 0}");
                    Console.WriteLine($"   Response Status: {flow.Response.StatusCode} {flow.Response.ReasonPhrase}");
                    Console.WriteLine($"   Duration: {flow.Duration.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"   Status: {flow.Status}");
                    
                    // Log first few headers for debugging
                    if (flow.Request.Headers != null && flow.Request.Headers.Count > 0)
                    {
                        Console.WriteLine($"   Request Headers (first 3):");
                        foreach (var header in flow.Request.Headers.Take(3))
                        {
                            Console.WriteLine($"     {header.Key}: {header.Value}");
                        }
                    }
                };

                // Start proxy
                var proxyOptions = new ProxyOptions
                {
                    ListenAddress = "127.0.0.1",
                    ListenPort = 8080,
                    InterceptHttps = true
                };

                Console.WriteLine($"\n🚀 Starting proxy on {proxyOptions.ListenAddress}:{proxyOptions.ListenPort}...");
                Console.WriteLine("📝 Waiting for connections...\n");
                
                await proxyServer.StartAsync(proxyOptions);

                Console.WriteLine("✅ Proxy started successfully!");
                Console.WriteLine("\n📋 Test Commands:");
                Console.WriteLine("   HTTP:  curl -x http://localhost:8080 http://httpbin.org/get");
                Console.WriteLine("   HTTPS: curl -x http://localhost:8080 --insecure https://httpbin.org/get");
                Console.WriteLine("\n⏸️  Press ENTER to stop the proxy...");
                Console.ReadLine();

                // Stop proxy
                await proxyServer.StopAsync();
                Console.WriteLine("\n🛑 Proxy stopped.");
                Console.WriteLine($"📊 Total flows captured: {flowCount}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Proxy debug failed");
                Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
                Console.WriteLine($"📍 Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n👋 Press ENTER to exit...");
            Console.ReadLine();
        }
    }

    // Logger adapter
    public class SnifferLoggerAdapter : Snitcher.Sniffer.Core.Interfaces.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public SnifferLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInfo(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogFlow(Snitcher.Sniffer.Core.Models.Flow flow)
        {
            _logger.LogDebug("Flow captured: {Method} {Url}", flow.Request.Method, flow.Request.Url);
        }
    }
}
