using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Core.Services
{
    public class InMemoryFlowStorage : IFlowStorage
    {
        private readonly Dictionary<Guid, Flow> _flows = new();
        private readonly object _lock = new();

        public Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _flows[flow.Id] = flow;
            }
            return Task.CompletedTask;
        }

        public Task<Flow?> GetFlowAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_flows.TryGetValue(id, out var flow) ? flow : null);
            }
        }

        public Task<IEnumerable<Flow>> GetFlowsAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IEnumerable<Flow> flows = _flows.Values.OrderByDescending(f => f.Timestamp);
                if (limit.HasValue)
                {
                    flows = flows.Take(limit.Value);
                }
                return Task.FromResult(flows);
            }
        }

        public Task<IEnumerable<Flow>> GetFlowsAsync(Func<Flow, bool> predicate, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IEnumerable<Flow> flows = _flows.Values.Where(predicate).OrderByDescending(f => f.Timestamp);
                return Task.FromResult(flows);
            }
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _flows.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
