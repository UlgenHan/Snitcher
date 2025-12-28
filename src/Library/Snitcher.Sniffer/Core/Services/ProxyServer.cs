using System.Net;
using System.Net.Sockets;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Services
{
    public class ProxyServer : IProxyServer
    {
        private readonly IConnectionHandler _connectionHandler;
        private readonly ILogger _logger;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private ProxyOptions _options = new();

        public bool IsRunning { get; private set; }
        public event EventHandler<FlowEventArgs>? FlowCaptured;

        public ProxyServer(IConnectionHandler connectionHandler, ILogger logger)
        {
            _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to connection handler flow events
            _connectionHandler.FlowCaptured += OnFlowCaptured;
        }

        private void OnFlowCaptured(object? sender, FlowEventArgs e)
        {
            // Forward the flow captured event
            FlowCaptured?.Invoke(this, e);
        }

        public async Task StartAsync(ProxyOptions options, CancellationToken cancellationToken = default)
        {
            if (IsRunning) throw new InvalidOperationException("Proxy server is already running");

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _tcpListener = new TcpListener(IPAddress.Parse(_options.ListenAddress), _options.ListenPort);
                _tcpListener.Start();
                IsRunning = true;

                _logger.LogInfo("Proxy server started on {0}:{1}", _options.ListenAddress, _options.ListenPort);

                // Start accepting connections in the background without blocking
                _ = Task.Run(() => AcceptConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                
                // Return immediately so the UI knows the proxy has started
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start proxy server");
                IsRunning = false;
                throw;
            }
        }



        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRunning) return;

            _cancellationTokenSource?.Cancel();
            _tcpListener?.Stop();
            IsRunning = false;

            _logger.LogInfo($"Proxy server stopped");
            await Task.CompletedTask;
        }


        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener!.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleConnectionSafe(tcpClient), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting connection");
                }
            }
        }

        private async Task HandleConnectionSafe(TcpClient tcpClient)
        {
            try
            {
                await _connectionHandler.HandleConnectionAsync(tcpClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection from {0}", tcpClient.Client.RemoteEndPoint);
            }
            finally
            {
                tcpClient.Close();
            }
        }

        protected virtual void OnFlowCaptured(Flow flow) =>
            FlowCaptured?.Invoke(this, new FlowEventArgs(flow));
    }
}
