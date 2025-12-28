using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snitcher.UI.Desktop.Models.Network;

namespace Snitcher.UI.Desktop.Services.Network
{
    public class RequestSender
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        public RequestSender()
        {
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }
        public async Task<HttpResponse> SendAsync(HttpRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var httpRequestMessage = CreateHttpRequestMessage(request);
                var response = await _httpClient.SendAsync(httpRequestMessage);

                stopwatch.Stop();

                return await ProcessHttpResponseAsync(response, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                return new HttpResponse
                {
                    StatusCode = 0,
                    StatusMessage = ex.Message,
                    ResponseTime = stopwatch.Elapsed,
                    Body = $"Request failed: {ex.Message}"
                };
            }
        }
        private HttpRequestMessage CreateHttpRequestMessage(HttpRequest request)
        {
            var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);
            // Add headers
            foreach (var header in request.Headers.Where(h => h.Enabled))
            {
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            // Add body
            if (!string.IsNullOrWhiteSpace(request.Body) &&
                request.Method.ToUpper() != "GET" &&
                request.Method.ToUpper() != "HEAD")
            {
                message.Content = new StringContent(request.Body, Encoding.UTF8, request.ContentType);
            }
            return message;
        }
        private async Task<HttpResponse> ProcessHttpResponseAsync(HttpResponseMessage response, TimeSpan elapsed)
        {
            var responseModel = new HttpResponse
            {
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.ReasonPhrase ?? "Unknown",
                ResponseTime = elapsed
            };
            // Add response headers
            foreach (var header in response.Headers)
            {
                responseModel.Headers.Add(new HeaderItem
                {
                    Key = header.Key,
                    Value = string.Join(", ", header.Value),
                    Enabled = true
                });
            }
            // Add response body
            responseModel.Body = await response.Content.ReadAsStringAsync();
            return responseModel;
        }
    }

}
