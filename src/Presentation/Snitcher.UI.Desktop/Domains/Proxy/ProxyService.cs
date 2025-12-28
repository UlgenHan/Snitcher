using Microsoft.Extensions.Logging;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;
using Snitcher.Sniffer.Core.Services;
using Snitcher.Sniffer.Http;
using Snitcher.Sniffer.Certificates;
using Snitcher.UI.Desktop.Models;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Snitcher.UI.Desktop.Domains.Proxy
{
    public interface IProxyService
    {
        Task<bool> StartAsync(int port);
        Task<bool> StopAsync();
        bool IsRunning { get; }
        event EventHandler<FlowItem>? FlowCaptured;
        event EventHandler<string>? StatusChanged;
        event EventHandler<string>? ErrorOccurred;
    }

    public class ProxyService : IProxyService, IDisposable
    {
        private readonly ILogger<ProxyService> _logger;
        private readonly IProxyServer _proxyServer;
        private readonly IConnectionHandler _connectionHandler;
        private readonly ICertificateManager _certificateManager;
        private readonly Snitcher.Sniffer.Core.Interfaces.ILogger _snifferLogger;
        private bool _disposed;

        public bool IsRunning => _proxyServer.IsRunning;

        public event EventHandler<FlowItem>? FlowCaptured;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;

        public ProxyService(ILogger<ProxyService> logger, ICertificateManager certificateManager, Snitcher.Sniffer.Core.Interfaces.ILogger snifferLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
            _snifferLogger = snifferLogger ?? throw new ArgumentNullException(nameof(snifferLogger));
            
            // Create required dependencies
            var httpParser = new HttpParser(_snifferLogger);
            
            // Initialize CA certificate asynchronously
            _ = Task.Run(async () => 
            {
                try
                {
                    await _certificateManager.GetOrCreateCACertificateAsync("mitmproxy");
                    _logger.LogInformation("CA certificate initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize CA certificate");
                }
            });
            
            var flowStorage = new InMemoryFlowStorage();
            var requestInterceptors = new List<IRequestInterceptor>();
            var responseInterceptors = new List<IResponseInterceptor>();
            
            // Create connection handler
            _connectionHandler = new ConnectionHandler(
                httpParser, 
                _certificateManager, 
                flowStorage, 
                requestInterceptors, 
                responseInterceptors, 
                _snifferLogger);
            
            // Create proxy server
            _proxyServer = new ProxyServer(_connectionHandler, _snifferLogger);
            
            // Subscribe to flow events
            _proxyServer.FlowCaptured += OnFlowCaptured;
        }

        public async Task<bool> StartAsync(int port)
        {
            try
            {
                if (_proxyServer.IsRunning)
                {
                    _logger.LogWarning("Proxy server is already running");
                    return false;
                }

                // Initialize CA certificate for HTTPS interception
                _logger.LogInformation("Initializing CA certificate for HTTPS interception...");
                var caPassword = "snitcher-ca";
                await _certificateManager.GetOrCreateCACertificateAsync(caPassword);
                
                // Check if CA certificate is trusted
                if (!_certificateManager.IsCACertificateTrusted())
                {
                    _logger.LogWarning("CA certificate is not trusted. HTTPS interception may not work properly.");
                    _logger.LogInformation("To install the CA certificate, run the application as administrator or manually install the mitmproxy-ca.pfx file.");
                }
                else
                {
                    _logger.LogInformation("CA certificate is trusted and ready for HTTPS interception.");
                }

                var options = new ProxyOptions
                {
                    ListenPort = port,
                    ListenAddress = "127.0.0.1",
                    InterceptHttps = true,
                    EnableLogging = true
                };

                await _proxyServer.StartAsync(options);
                
                _logger.LogInformation($"Proxy server started on port {port}");
                StatusChanged?.Invoke(this, $"Running on port {port}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start proxy server");
                ErrorOccurred?.Invoke(this, $"Failed to start: {ex.Message}");
                StatusChanged?.Invoke(this, "Failed to start");
                return false;
            }
        }

        public async Task<bool> StopAsync()
        {
            try
            {
                if (!_proxyServer.IsRunning)
                {
                    _logger.LogWarning("Proxy server is not running");
                    return false;
                }

                await _proxyServer.StopAsync();
                
                _logger.LogInformation("Proxy server stopped");
                StatusChanged?.Invoke(this, "Stopped");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop proxy server");
                ErrorOccurred?.Invoke(this, $"Failed to stop: {ex.Message}");
                return false;
            }
        }

        private void OnFlowCaptured(object? sender, FlowEventArgs e)
        {
            try
            {
                var flowItem = FlowMapper.MapToFlowItem(e.Flow);
                FlowCaptured?.Invoke(this, flowItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map flow to UI model");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_proxyServer.IsRunning)
                    {
                        _proxyServer.StopAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during disposal");
                }

                _disposed = true;
            }
        }
    }

    // Adapter to convert between Microsoft.Extensions.Logging and Snitcher logger
    internal class SnifferLoggerAdapter : Snitcher.Sniffer.Core.Interfaces.ILogger
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

        public void LogFlow(Flow flow)
        {
            _logger.LogDebug("Flow captured: {Method} {Url}", flow.Request.Method, flow.Request.Url);
        }
    }
}
