using Microsoft.Extensions.Logging;
using Snitcher.Sniffer.Core.Services;
using Snitcher.Sniffer.Core.Models;
using Snitcher.Sniffer.Http;
using Snitcher.Sniffer.Certificates;
using Snitcher.Sniffer.Core.Interfaces;
using System.Net.Http;
using SnifferLogger = Snitcher.Sniffer.Core.Interfaces.ILogger;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace ProxyTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<Program>();

            Console.WriteLine("🚀 Starting Snitcher Proxy Test...");
            Console.WriteLine("=====================================");

            try
            {
                // Create proxy service
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

                // Subscribe to flow events
                int flowCount = 0;
                proxyServer.FlowCaptured += (sender, e) =>
                {
                    flowCount++;
                    var flow = e.Flow;
                    Console.WriteLine($"\n📊 Flow #{flowCount} Captured:");
                    Console.WriteLine($"   Method: {flow.Request.Method}");
                    Console.WriteLine($"   URL: {flow.Request.Url}");
                    Console.WriteLine($"   Status: {flow.Response.StatusCode} {flow.Response.ReasonPhrase}");
                    Console.WriteLine($"   Duration: {flow.Duration.TotalMilliseconds:F0}ms");
                };

                // Start proxy
                var proxyOptions = new ProxyOptions
                {
                    ListenAddress = "127.0.0.1",
                    ListenPort = 8080,
                    InterceptHttps = true
                };

                Console.WriteLine($"🔧 Starting proxy on {proxyOptions.ListenAddress}:{proxyOptions.ListenPort}...");
                await proxyServer.StartAsync(proxyOptions);

                Console.WriteLine("✅ Proxy started successfully!");
                Console.WriteLine("\n📝 Test URLs:");
                Console.WriteLine("   HTTP:  http://httpbin.org/get");
                Console.WriteLine("   HTTPS: https://httpbin.org/get");
                Console.WriteLine("   User URL: https://sosyal.teknofest.app/api/v1/accounts/lookup?acct=direnispostasi1453");
                Console.WriteLine("\n🔄 Testing proxy functionality...");

                // Test with HttpClient
                await TestWithHttpClient(logger);

                Console.WriteLine($"\n📈 Total flows captured: {flowCount}");
                Console.WriteLine("\n⏸️  Press ENTER to stop the proxy...");
                Console.ReadLine();

                // Stop proxy
                await proxyServer.StopAsync();
                Console.WriteLine("🛑 Proxy stopped.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Proxy test failed");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine("\n👋 Test completed. Press ENTER to exit...");
            Console.ReadLine();
        }

        static async Task TestWithHttpClient(MsLogger logger)
        {
            try
            {
                // Create HttpClient with proxy
                var proxy = new System.Net.WebProxy
                {
                    Address = new Uri("http://localhost:8080"),
                    BypassProxyOnLocal = false
                };

                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };

                // Bypass certificate validation for HTTPS
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Snitcher-Proxy-Test/1.0");

                // Test HTTP
                Console.WriteLine("\n🌐 Testing HTTP request...");
                var httpResponse = await client.GetAsync("http://httpbin.org/get");
                Console.WriteLine($"   HTTP Status: {httpResponse.StatusCode}");
                var httpContent = await httpResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"   Response length: {httpContent.Length} characters");

                // Test HTTPS
                Console.WriteLine("\n🔒 Testing HTTPS request...");
                var httpsResponse = await client.GetAsync("https://httpbin.org/get");
                Console.WriteLine($"   HTTPS Status: {httpsResponse.StatusCode}");
                var httpsContent = await httpsResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"   Response length: {httpsContent.Length} characters");

                // Test user's specific URL
                Console.WriteLine("\n🎯 Testing user's URL...");
                client.DefaultRequestHeaders.Add("Cookie", "_cfuvid=MsT0X6Les2AIdQUQtM.a9mRvVGuhaYga.0nLzdlwE-1765116776.7853727-1.0.1.1-HbPJp0i6Nx9cB0j");
                var userResponse = await client.GetAsync("https://sosyal.teknofest.app/api/v1/accounts/lookup?acct=direnispostasi1453");
                Console.WriteLine($"   User URL Status: {userResponse.StatusCode}");
                var userContent = await userResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"   Response length: {userContent.Length} characters");

                if (!string.IsNullOrEmpty(userContent))
                {
                    Console.WriteLine("   ✅ User's URL request successful!");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HTTP client test failed");
                Console.WriteLine($"   ❌ HTTP client test failed: {ex.Message}");
            }
        }
    }

    // Simple logger adapter for testing
    public class SnifferLoggerAdapter : SnifferLogger
    {
        private readonly MsLogger _logger;

        public SnifferLoggerAdapter(MsLogger logger)
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
