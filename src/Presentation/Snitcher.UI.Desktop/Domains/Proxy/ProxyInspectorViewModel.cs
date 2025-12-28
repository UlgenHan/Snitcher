using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Snitcher.UI.Desktop.Models;
using Snitcher.UI.Desktop.Domains.Proxy;
using Snitcher.UI.Desktop.ViewModels;
using Avalonia.Media;

namespace Snitcher.UI.Desktop.Domains.Proxy
{
    public partial class ProxyInspectorViewModel : ViewModelBase
    {
        private readonly IProxyService _proxyService;
        private readonly ILogger<ProxyInspectorViewModel> _logger;
        private readonly ObservableCollection<FlowItem> _allFlowItems = new();

        [ObservableProperty]
        private ObservableCollection<FlowItem> _flowItems = new();

        [ObservableProperty]
        private FlowItem? _selectedFlow;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private bool _isProxyRunning = false;

        [ObservableProperty]
        private int _proxyPort = 8080;

        [ObservableProperty]
        private string _proxyStatus = "Stopped";

        [ObservableProperty]
        private string _toggleProxyButtonText = "Start";

        [ObservableProperty]
        private Color _proxyStatusColor = Colors.Red;

        [ObservableProperty]
        private bool _isProxyStarting = false;

        [ObservableProperty]
        private bool _isProxyStopping = false;

        public bool IsProxyBusy => IsProxyStarting || IsProxyStopping;

        [ObservableProperty]
        private int _totalRequests = 0;

        [ObservableProperty]
        private int _totalResponses = 0;

        [ObservableProperty]
        private string _selectedFilter = "All";

        public ProxyInspectorViewModel(IProxyService proxyService, ILogger<ProxyInspectorViewModel> logger)
        {
            _proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to proxy service events
            _proxyService.FlowCaptured += OnFlowCaptured;
            _proxyService.StatusChanged += OnStatusChanged;
            _proxyService.ErrorOccurred += OnErrorOccurred;
            
            // Initialize with empty state (real data will come from proxy)
            UpdateStatistics();
        }

        private void OnFlowCaptured(object? sender, FlowItem flow)
        {
            try
            {
                // Add to main collection on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _allFlowItems.Insert(0, flow); // Add at beginning for chronological order
                    ApplyFilters();
                    UpdateStatistics();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling captured flow");
            }
        }

        private void OnStatusChanged(object? sender, string status)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Don't override status if we're in intermediate states
                if (!IsProxyStarting && !IsProxyStopping)
                {
                    ProxyStatus = status;
                    IsProxyRunning = _proxyService.IsRunning;
                    
                    // Update button text and status color based on actual proxy state
                    if (IsProxyRunning)
                    {
                        ToggleProxyButtonText = "Stop";
                        ProxyStatusColor = Colors.LimeGreen;
                        // Update status to be more descriptive
                        if (status.Contains("Running"))
                        {
                            ProxyStatus = "Listening for connections...";
                        }
                    }
                    else
                    {
                        ToggleProxyButtonText = "Start";
                        ProxyStatusColor = Colors.Red;
                        if (status == "Stopped")
                        {
                            ProxyStatus = "Stopped";
                        }
                    }
                }
            });
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _logger.LogError("Proxy error: {Error}", error);
                // Could show error notification here
            });
        }

        private void UpdateStatistics()
        {
            TotalRequests = _allFlowItems.Count;
            TotalResponses = _allFlowItems.Count; // In this context, each flow represents a request-response pair
        }

        [RelayCommand]
        private async Task ToggleProxy()
        {
            try
            {
                if (IsProxyRunning)
                {
                    // Start stopping process
                    IsProxyStopping = true;
                    ProxyStatus = "Stopping...";
                    ProxyStatusColor = Colors.Orange;
                    ToggleProxyButtonText = "Stopping...";
                    
                    var success = await _proxyService.StopAsync();
                    if (!success)
                    {
                        ProxyStatus = "Failed to stop";
                        ProxyStatusColor = Colors.Red;
                        _logger.LogWarning("Failed to stop proxy server");
                    }
                }
                else
                {
                    // Start starting process
                    IsProxyStarting = true;
                    ProxyStatus = "Preparing to listen...";
                    ProxyStatusColor = Colors.Orange;
                    ToggleProxyButtonText = "Starting...";
                    
                    var success = await _proxyService.StartAsync(ProxyPort);
                    if (!success)
                    {
                        ProxyStatus = "Failed to start";
                        ProxyStatusColor = Colors.Red;
                        ToggleProxyButtonText = "Start";
                        _logger.LogWarning("Failed to start proxy server");
                    }
                }
                
                // Reset intermediate states
                IsProxyStarting = false;
                IsProxyStopping = false;
            }
            catch (Exception ex)
            {
                ProxyStatus = "Error occurred";
                ProxyStatusColor = Colors.Red;
                ToggleProxyButtonText = "Start";
                IsProxyStarting = false;
                IsProxyStopping = false;
                _logger.LogError(ex, "Error toggling proxy server");
            }
        }

        [RelayCommand]
        private void ClearFlows()
        {
            try
            {
                _allFlowItems.Clear();
                FlowItems.Clear();
                SelectedFlow = null;
                UpdateStatistics();
                _logger.LogInformation("Cleared all flows");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing flows");
            }
        }

        [RelayCommand]
        private void ExportFlows()
        {
            // Export logic would go here
        }

        [RelayCommand]
        private void ImportFlows()
        {
            // Import logic would go here
        }

        [RelayCommand]
        private void DeleteFlow(FlowItem flow)
        {
            try
            {
                if (flow != null)
                {
                    _allFlowItems.Remove(flow);
                    FlowItems.Remove(flow);
                    if (SelectedFlow == flow)
                    {
                        SelectedFlow = null;
                    }
                    UpdateStatistics();
                    _logger.LogInformation("Deleted flow {FlowId}", flow.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting flow");
            }
        }

        [RelayCommand]
        private void ReplayFlow(FlowItem flow)
        {
            // Replay logic would go here
        }

        [RelayCommand]
        private void SetFilter(string filter)
        {
            try
            {
                SelectedFilter = filter;
                ApplyFilters();
                _logger.LogDebug("Applied filter: {Filter}", filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting filter");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allFlowItems.AsEnumerable();

                // Apply method filter
                if (SelectedFilter != "All")
                {
                    if (SelectedFilter == "Errors")
                    {
                        filtered = filtered.Where(f => f.Status >= 400);
                    }
                    else
                    {
                        filtered = filtered.Where(f => f.Method.Equals(SelectedFilter, StringComparison.OrdinalIgnoreCase));
                    }
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(f => 
                        f.Host.ToLowerInvariant().Contains(searchTerm) ||
                        f.Path.ToLowerInvariant().Contains(searchTerm) ||
                        f.FullUrl.ToLowerInvariant().Contains(searchTerm) ||
                        f.Method.ToLowerInvariant().Contains(searchTerm));
                }

                // Update the observable collection
                FlowItems.Clear();
                foreach (var item in filtered.Take(1000)) // Limit to 1000 items for performance
                {
                    FlowItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying filters");
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        public void Dispose()
        {
            try
            {
                if (_proxyService.IsRunning)
                {
                    _proxyService.StopAsync().GetAwaiter().GetResult();
                }

                // Unsubscribe from events
                _proxyService.FlowCaptured -= OnFlowCaptured;
                _proxyService.StatusChanged -= OnStatusChanged;
                _proxyService.ErrorOccurred -= OnErrorOccurred;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }
}
