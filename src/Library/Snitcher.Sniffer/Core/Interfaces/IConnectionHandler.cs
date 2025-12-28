using System.Net.Sockets;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IConnectionHandler
    {
        Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken = default);
        event EventHandler<FlowEventArgs>? FlowCaptured;
    }
}
