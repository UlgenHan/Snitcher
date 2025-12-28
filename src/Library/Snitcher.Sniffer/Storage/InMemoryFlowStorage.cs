using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Storage
{
    public class InMemoryFlowStorage : IFlowStorage
    {
        private readonly Dictionary<Guid, Flow> _flows = new();
        private readonly object _lock = new();
        private readonly int _maxFlows;

        public InMemoryFlowStorage(int maxFlows = 10000)
        {
            _maxFlows = maxFlows;
        }

        public Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _flows[flow.Id] = flow;

                // Remove old flows if we exceed the limit
                if (_flows.Count > _maxFlows)
                {
                    var oldestFlows = _flows.Values
                        .OrderBy(f => f.Timestamp)
                        .Take(_flows.Count - _maxFlows)
                        .ToList();

                    foreach (var oldFlow in oldestFlows)
                    {
                        _flows.Remove(oldFlow.Id);
                    }
                }
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

        public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_flows.Count);
            }
        }

        public Task<IEnumerable<Flow>> GetFlowsByDomainAsync(string domain, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IEnumerable<Flow> flows = _flows.Values
                    .Where(f => f.Request.Url.Host.Equals(domain, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.Timestamp);
                return Task.FromResult(flows);
            }
        }

        public Task<IEnumerable<Flow>> GetFlowsByStatusAsync(FlowStatus status, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IEnumerable<Flow> flows = _flows.Values
                    .Where(f => f.Status == status)
                    .OrderByDescending(f => f.Timestamp);
                return Task.FromResult(flows);
            }
        }
    }
}
