using Snitcher.UI.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Snitcher.UI.Desktop.Domains.RequestBuilder
{
    public class RequestSender : IRequestSender
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public RequestSender()
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                // Allow invalid certificates for testing
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Set default timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<HttpResponse> SendRequestAsync(HttpRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var httpRequest = CreateHttpRequestMessage(request);
                
                var response = await _httpClient.SendAsync(httpRequest);
                
                stopwatch.Stop();
                
                var responseBody = await response.Content.ReadAsStringAsync();
                var headers = response.Headers.Concat(response.Content.Headers)
                    .Select(h => new HttpHeader { Key = h.Key, Value = string.Join(", ", h.Value) })
                    .ToList();

                return new HttpResponse(
                    (int)response.StatusCode,
                    response.ReasonPhrase ?? "",
                    responseBody,
                    headers,
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return HttpResponse.CreateError(ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        private HttpRequestMessage CreateHttpRequestMessage(HttpRequest request)
        {
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);
            
            // Add headers
            foreach (var header in request.Headers.Where(h => h.Enabled))
            {
                if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                {
                    try
                    {
                        httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to add header {header.Key}: {ex.Message}");
                    }
                }
            }

            // Add authentication
            AddAuthentication(httpRequest, request);

            // Add body
            AddRequestBody(httpRequest, request);

            return httpRequest;
        }

        private void AddAuthentication(HttpRequestMessage httpRequest, HttpRequest request)
        {
            switch (request.AuthenticationType)
            {
                case AuthenticationType.Basic:
                    if (!string.IsNullOrWhiteSpace(request.Authentication.Username))
                    {
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Authentication.Username}:{request.Authentication.Password}"));
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Basic {credentials}");
                    }
                    break;

                case AuthenticationType.Bearer:
                    if (!string.IsNullOrWhiteSpace(request.Authentication.BearerToken))
                    {
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {request.Authentication.BearerToken}");
                    }
                    break;

                case AuthenticationType.ApiKey:
                    if (!string.IsNullOrWhiteSpace(request.Authentication.ApiKey) && !string.IsNullOrWhiteSpace(request.Authentication.ApiValue))
                    {
                        // Default to adding API key as a header
                        httpRequest.Headers.TryAddWithoutValidation(request.Authentication.ApiKey, request.Authentication.ApiValue);
                    }
                    break;
            }
        }

        private void AddRequestBody(HttpRequestMessage httpRequest, HttpRequest request)
        {
            if (request.Method == "GET" || request.Method == "HEAD" || request.BodyType == BodyType.None)
            {
                return;
            }

            string contentType = "application/json";
            var contentHeader = request.Headers.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
            if (contentHeader != null)
            {
                contentType = contentHeader.Value;
            }

            switch (request.BodyType)
            {
                case BodyType.Raw:
                    if (!string.IsNullOrWhiteSpace(request.Body))
                    {
                        try
                        {
                            // Try to parse and reformat JSON to ensure it's valid
                            var jsonElement = JsonSerializer.Deserialize<JsonElement>(request.Body);
                            var formattedJson = JsonSerializer.Serialize(jsonElement, _jsonOptions);
                            httpRequest.Content = new StringContent(formattedJson, Encoding.UTF8, "application/json");
                        }
                        catch
                        {
                            // If JSON parsing fails, send as-is
                            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                        }
                    }
                    break;

                case BodyType.Form:
                case BodyType.XForm:
                    var formContent = new FormUrlEncodedContent(ParseFormData(request.Body));
                    httpRequest.Content = formContent;
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(request.Body))
                    {
                        httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                    }
                    break;
            }
        }

        private Dictionary<string, string> ParseFormData(string formData)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrWhiteSpace(formData))
                return result;

            var lines = formData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    result[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return result;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
