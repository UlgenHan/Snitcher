using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Interfaces
{
    public interface IFlowStorage
    {
        Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default);
        Task<Flow?> GetFlowAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Flow>> GetFlowsAsync(int? limit = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<Flow>> GetFlowsAsync(Func<Flow, bool> predicate, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
