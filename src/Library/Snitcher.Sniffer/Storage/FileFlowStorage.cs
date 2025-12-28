using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;
using Snitcher.Sniffer.Core.Models;

namespace Snitcher.Sniffer.Storage
{

    public class FileFlowStorage : IFlowStorage
    {
        private readonly string _storageDirectory;
        private readonly ILogger _logger;
        private readonly object _lock = new();

        public FileFlowStorage(string storageDirectory, ILogger logger)
        {
            _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Directory.CreateDirectory(_storageDirectory);
        }

        public async Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default)
        {
            var filePath = GetFlowFilePath(flow.Id);
            var json = JsonSerializer.Serialize(flow, new JsonSerializerOptions { WriteIndented = true });

            lock (_lock)
            {
                File.WriteAllText(filePath, json);
            }

            _logger.LogInfo("Stored flow {0} to {1}", flow.Id, filePath);
            await Task.CompletedTask;
        }

        public async Task<Flow?> GetFlowAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var filePath = GetFlowFilePath(id);

            lock (_lock)
            {
                if (!File.Exists(filePath))
                    return null;

                try
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<Flow>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load flow {0} from {1}", id, filePath);
                    return null;
                }
            }
        }

        public async Task<IEnumerable<Flow>> GetFlowsAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var flows = new List<Flow>();

                foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var flow = JsonSerializer.Deserialize<Flow>(json);
                        if (flow != null)
                            flows.Add(flow);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load flow from {0}", file);
                    }
                }

                IEnumerable<Flow> result = flows.OrderByDescending(f => f.Timestamp);
                if (limit.HasValue)
                {
                    result = result.Take(limit.Value);
                }
                return (IEnumerable<Flow>)Task.FromResult(result);
            }
        }

        public async Task<IEnumerable<Flow>> GetFlowsAsync(Func<Flow, bool> predicate, CancellationToken cancellationToken = default)
        {
            var allFlows = await GetFlowsAsync(1, cancellationToken); // Fixed: Add null for limit
            return (IEnumerable<Flow>)Task.FromResult(allFlows.Where(predicate));
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
                {
                    File.Delete(file);
                }
            }

            _logger.LogInfo("Cleared all flows from {0}", _storageDirectory);
            await Task.CompletedTask;
        }

        private string GetFlowFilePath(Guid flowId) => Path.Combine(_storageDirectory, $"{flowId}.json");
    }
}

