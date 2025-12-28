using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IProxyServer
    {
        Task StartAsync(ProxyOptions options, CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        bool IsRunning { get; }
        event EventHandler<FlowEventArgs>? FlowCaptured;
    }
}
