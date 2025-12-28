using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snitcher.UI.Desktop.Models;
using Snitcher.UI.Desktop.Domains.RequestBuilder;
using Snitcher.UI.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Snitcher.UI.Desktop.Domains.RequestBuilder
{
    public partial class RequestBuilderViewModel : ViewModelBase
    {
        // Current Request
        [ObservableProperty]
        private HttpRequest _currentRequest = new();
        
        // Response
        [ObservableProperty]
        private HttpResponse? _currentResponse;
        
        // UI State
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private string _statusMessage = "Ready to send request";
        
        [ObservableProperty]
        private BodyType _selectedBodyType = BodyType.None;
        
        [ObservableProperty]
        private AuthenticationType _selectedAuthenticationType = AuthenticationType.None;
        
        [ObservableProperty]
        private int _selectedTabIndex = 0;
        
        // Collections
        [ObservableProperty]
        private ObservableCollection<RequestCollection> _collections = new();
        
        [ObservableProperty]
        private RequestCollection? _selectedCollection;
        
        // History
        [ObservableProperty]
        private ObservableCollection<HttpResponseHistory> _responseHistory = new();
        
        // Services
        private readonly RequestSender _requestSender;

        public RequestBuilderViewModel()
        {
            _requestSender = new RequestSender();
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Initialize with sample request
            CurrentRequest = new HttpRequest
            {
                Name = "Sample Request",
                Method = "GET",
                Url = "https://jsonplaceholder.typicode.com/posts/1",
                BodyType = BodyType.None,
                AuthenticationType = AuthenticationType.None
            };

            // Add sample collections
            Collections.Add(new RequestCollection
            {
                Name = "Sample Collection",
                Description = "Sample requests for testing",
                Requests = new ObservableCollection<HttpRequest> { CurrentRequest }
            });
        }

        [RelayCommand]
        private async Task SendRequest()
        {
            if (string.IsNullOrWhiteSpace(CurrentRequest.Url))
            {
                StatusMessage = "Please enter a URL";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Sending request...";

                var response = await _requestSender.SendRequestAsync(CurrentRequest);
                
                CurrentResponse = response;
                
                if (response.IsSuccess)
                {
                    StatusMessage = $"Request successful - {response.StatusCode} {response.StatusText}";
                }
                else
                {
                    StatusMessage = $"Request failed - {response.StatusCode} {response.StatusText}";
                }

                // Add to history
                var historyItem = new HttpResponseHistory(response, CurrentRequest);
                ResponseHistory.Insert(0, historyItem);
                
                // Keep only last 50 items
                while (ResponseHistory.Count > 50)
                {
                    ResponseHistory.RemoveAt(ResponseHistory.Count - 1);
                }
            }
            catch (Exception ex)
            {
                CurrentResponse = HttpResponse.CreateError(ex.Message);
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void SaveRequest()
        {
            if (SelectedCollection != null)
            {
                // Check if request already exists in collection
                var existingRequest = SelectedCollection.Requests.FirstOrDefault(r => r.Id == CurrentRequest.Id);
                if (existingRequest != null)
                {
                    // Update existing request
                    existingRequest.Name = CurrentRequest.Name;
                    existingRequest.Method = CurrentRequest.Method;
                    existingRequest.Url = CurrentRequest.Url;
                    existingRequest.Headers = new ObservableCollection<HttpHeader>(CurrentRequest.Headers);
                    existingRequest.Parameters = new ObservableCollection<HttpParameter>(CurrentRequest.Parameters);
                    existingRequest.Body = CurrentRequest.Body;
                    existingRequest.BodyType = CurrentRequest.BodyType;
                    existingRequest.AuthenticationType = CurrentRequest.AuthenticationType;
                    existingRequest.Authentication = CurrentRequest.Authentication;
                    existingRequest.Description = CurrentRequest.Description;
                    existingRequest.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Add new request
                    SelectedCollection.Requests.Add(CurrentRequest.Clone());
                }
                
                StatusMessage = "Request saved to collection";
            }
            else
            {
                StatusMessage = "Please select a collection to save the request";
            }
        }

        [RelayCommand]
        private void NewRequest()
        {
            CurrentRequest = new HttpRequest
            {
                Name = "New Request",
                Method = "GET",
                BodyType = BodyType.None,
                AuthenticationType = AuthenticationType.None
            };
            
            CurrentResponse = null;
            StatusMessage = "Created new request";
        }

        [RelayCommand]
        private void DuplicateRequest()
        {
            CurrentRequest = CurrentRequest.Clone();
            StatusMessage = "Request duplicated";
        }

        [RelayCommand]
        private void AddHeader()
        {
            CurrentRequest.Headers.Add(new HttpHeader());
        }

        [RelayCommand]
        public void RemoveHeader(HttpHeader header)
        {
            CurrentRequest.Headers.Remove(header);
        }

        [RelayCommand]
        private void AddParameter()
        {
            CurrentRequest.Parameters.Add(new HttpParameter());
        }

        [RelayCommand]
        public void RemoveParameter(HttpParameter parameter)
        {
            CurrentRequest.Parameters.Remove(parameter);
        }

        [RelayCommand]
        public void AddTest()
        {
            CurrentRequest.Tests.Add(new HttpTest { Name = $"Test {CurrentRequest.Tests.Count + 1}" });
        }

        [RelayCommand]
        public void RemoveTest(HttpTest test)
        {
            CurrentRequest.Tests.Remove(test);
        }

        [RelayCommand]
        private void LoadFromHistory(HttpResponseHistory history)
        {
            CurrentRequest = history.Request.Clone();
            CurrentResponse = history.Response;
            StatusMessage = "Loaded request from history";
        }

        [RelayCommand]
        private void ClearHistory()
        {
            ResponseHistory.Clear();
            StatusMessage = "History cleared";
        }

        partial void OnSelectedBodyTypeChanged(BodyType value)
        {
            CurrentRequest.BodyType = value;
            
            // Set default content type based on body type
            if (value == BodyType.Raw)
            {
                var contentTypeHeader = CurrentRequest.Headers.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
                if (contentTypeHeader != null)
                {
                    contentTypeHeader.Value = "application/json";
                }
            }
        }

        partial void OnSelectedAuthenticationTypeChanged(AuthenticationType value)
        {
            CurrentRequest.AuthenticationType = value;
        }

        public static readonly string[] HttpMethods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "CUSTOM" };
    }
}
