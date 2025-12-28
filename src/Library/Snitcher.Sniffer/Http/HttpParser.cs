using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snitcher.Sniffer.Core.Interfaces;

namespace Snitcher.Sniffer.Http
{
    public class HttpParser : IHttpParser
    {
        private readonly ILogger _logger;

        public HttpParser(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Core.Models.HttpRequestMessage> ParseRequestAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            var readBuffer = new byte[1024];
            int bytesRead;
            bool foundBlankLine = false;

            // Read until we find the end of headers (\r\n\r\n)
            while (!foundBlankLine && (bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken)) > 0)
            {
                buffer.Write(readBuffer, 0, bytesRead);
                var bytes = buffer.ToArray();
                var asString = Encoding.ASCII.GetString(bytes);
                if (asString.Contains("\r\n\r\n"))
                    foundBlankLine = true;
            }

            var fullRequest = buffer.ToArray();
            var requestText = Encoding.ASCII.GetString(fullRequest);

            _logger.LogInfo("Raw request:\n{0}", requestText);

            return ParseRequestText(requestText);
        }

        public async Task<Core.Models.HttpResponseMessage> ParseResponseAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("🔍 Starting to parse HTTP response from stream");
            
            var buffer = new MemoryStream();
            var readBuffer = new byte[1024];
            int bytesRead;
            bool foundBlankLine = false;

            // Read until we find the end of headers (\r\n\r\n)
            while (!foundBlankLine && (bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken)) > 0)
            {
                buffer.Write(readBuffer, 0, bytesRead);
                var bytes = buffer.ToArray();
                var asString = Encoding.ASCII.GetString(bytes);
                if (asString.Contains("\r\n\r\n"))
                    foundBlankLine = true;
            }

            var fullResponse = buffer.ToArray();
            var responseText = Encoding.ASCII.GetString(fullResponse);

            _logger.LogInfo("📋 HTTP Response Headers:\n{0}", responseText.Split('\r')[0]);

            // Parse headers to determine if we need to read body
            var response = ParseResponseText(responseText);
            
            // Read body if present based on Content-Length or chunked encoding
            if (response.Headers.TryGetValue("Content-Length", out var contentLengthStr) && int.TryParse(contentLengthStr, out var contentLength) && contentLength > 0)
            {
                _logger.LogInfo("📦 Reading response body with Content-Length: {0}", contentLength);
                
                // Read exact content length bytes
                var bodyBuffer = new byte[contentLength];
                var totalRead = 0;
                while (totalRead < contentLength)
                {
                    var toRead = Math.Min(bodyBuffer.Length - totalRead, readBuffer.Length);
                    bytesRead = await stream.ReadAsync(readBuffer, 0, toRead, cancellationToken);
                    if (bytesRead == 0) 
                    {
                        _logger.LogWarning("⚠️ Stream closed prematurely while reading body. Read {0}/{1} bytes", totalRead, contentLength);
                        break;
                    }
                    
                    Array.Copy(readBuffer, 0, bodyBuffer, totalRead, bytesRead);
                    totalRead += bytesRead;
                }
                response.Body = bodyBuffer;
                _logger.LogInfo("✅ Successfully read {0} bytes of response body", totalRead);
            }
            else if (response.Headers.TryGetValue("Transfer-Encoding", out var transferEncoding) && transferEncoding.Contains("chunked"))
            {
                _logger.LogInfo("📦 Reading chunked response body");
                
                // Handle chunked encoding
                var bodyStream = new MemoryStream();
                while (true)
                {
                    // Read chunk size line
                    var chunkSizeLine = await ReadLineAsync(stream, cancellationToken);
                    if (string.IsNullOrEmpty(chunkSizeLine)) break;
                    
                    if (!int.TryParse(chunkSizeLine.Trim().Split(';')[0], System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
                        break;
                    
                    if (chunkSize == 0) break; // Last chunk
                    
                    // Read chunk data
                    var chunkBuffer = new byte[chunkSize];
                    var totalRead = 0;
                    while (totalRead < chunkSize)
                    {
                        bytesRead = await stream.ReadAsync(chunkBuffer, totalRead, chunkSize - totalRead, cancellationToken);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }
                    bodyStream.Write(chunkBuffer, 0, totalRead);
                    
                    // Read trailing CRLF
                    await ReadLineAsync(stream, cancellationToken);
                }
                response.Body = bodyStream.ToArray();
                _logger.LogInfo("✅ Successfully read chunked response body");
            }
            else
            {
                _logger.LogInfo("📦 No Content-Length or chunked encoding found, reading remaining data");
                
                // No content length or chunked encoding - read what's available
                var remainingBuffer = new MemoryStream();
                while ((bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken)) > 0)
                {
                    remainingBuffer.Write(readBuffer, 0, bytesRead);
                }
                response.Body = remainingBuffer.ToArray();
                _logger.LogInfo("✅ Read {0} bytes of remaining response data", remainingBuffer.Length);
            }

            _logger.LogInfo("🎯 HTTP Response parsing complete. Status: {0} {1}, Body size: {2} bytes", 
                response.StatusCode, response.ReasonPhrase, response.Body.Length);
            
            return response;
        }

        private async Task<string> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new MemoryStream();
            var byteBuffer = new byte[1];
            
            while (true)
            {
                var bytesRead = await stream.ReadAsync(byteBuffer, 0, 1, cancellationToken);
                if (bytesRead == 0) return buffer.Length > 0 ? Encoding.ASCII.GetString(buffer.ToArray()) : null;
                
                buffer.WriteByte(byteBuffer[0]);
                
                if (byteBuffer[0] == '\n')
                {
                    var bytes = buffer.ToArray();
                    return Encoding.ASCII.GetString(bytes).Trim('\r', '\n');
                }
            }
        }

        public async Task WriteRequestAsync(Core.Models.HttpRequestMessage request, Stream stream, CancellationToken cancellationToken = default)
        {
            var requestText = BuildRequestText(request);
            var requestBytes = Encoding.ASCII.GetBytes(requestText);

            await stream.WriteAsync(requestBytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInfo("Sent request:\n{0}", requestText);
        }

        public async Task WriteResponseAsync(Core.Models.HttpResponseMessage response, Stream stream, CancellationToken cancellationToken = default)
        {
            var responseText = BuildResponseText(response);
            var responseBytes = Encoding.ASCII.GetBytes(responseText);

            await stream.WriteAsync(responseBytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private Core.Models.HttpRequestMessage ParseRequestText(string requestText)
        {
            var lines = requestText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0)
                throw new InvalidOperationException("Empty request");

            // Parse request line: GET /path HTTP/1.1 or CONNECT host:port HTTP/1.1
            var requestLineParts = lines[0].Split(' ', 3);
            if (requestLineParts.Length != 3)
                throw new InvalidOperationException($"Invalid request line: {lines[0]}");

            var request = new Core.Models.HttpRequestMessage
            {
                Method = requestLineParts[0],
                HttpVersion = requestLineParts[2]
            };

            // Fix: Handle CONNECT requests differently
            if (request.Method.ToString() == "CONNECT")
            {
                // For CONNECT, the target is host:port, not a full URL
                var target = requestLineParts[1];
                var parts = target.Split(':');
                if (parts.Length == 2)
                {
                    var host = parts[0];
                    var port = int.Parse(parts[1]);
                    // Create a fake URL for CONNECT requests
                    request.Url = new Uri($"https://{host}:{port}");
                }
                else
                {
                    throw new InvalidOperationException($"Invalid CONNECT target: {target}");
                }
            }
            else
            {
                // For regular HTTP requests, parse as full URL
                var target = requestLineParts[1];
                if (target.StartsWith("/"))
                {
                    // Relative URL - need to extract from Host header
                    request.Url = new Uri($"http://localhost{target}"); // Will be updated later
                }
                else
                {
                    request.Url = new Uri(target);
                }
            }

            // Parse headers
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) break; // End of headers

                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    request.Headers[name] = value;

                    // Fix: Update URL for relative requests
                    if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) && request.Url.ToString().Contains("localhost"))
                    {
                        var scheme = request.Method == Core.Models.HttpMethod.Connect ? "https" : "http";
                        request.Url = new Uri($"{scheme}://{value}{request.Url.PathAndQuery}");
                    }
                }
            }

            return request;
        }

        private Core.Models.HttpResponseMessage ParseResponseText(string responseText)
        {
            var lines = responseText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (lines.Length == 0)
                throw new InvalidOperationException("Empty response");

            // Parse status line: HTTP/1.1 200 OK
            var statusLineParts = lines[0].Split(' ', 3);
            if (statusLineParts.Length < 2)
                throw new InvalidOperationException($"Invalid status line: {lines[0]}");

            var response = new Core.Models.HttpResponseMessage
            {
                HttpVersion = statusLineParts[0],
                StatusCode = int.Parse(statusLineParts[1]),
                ReasonPhrase = statusLineParts.Length > 2 ? statusLineParts[2] : "OK"
            };

            // Parse headers
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) break; // End of headers

                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var name = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    response.Headers[name] = value;
                }
            }

            return response;
        }

        private string BuildRequestText(Core.Models.HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            // Fix: Handle CONNECT requests differently
            if (request.Method == Core.Models.HttpMethod.Connect)
            {
                var hostPort = $"{request.Url.Host}:{request.Url.Port}";
                sb.AppendLine($"{request.Method} {hostPort} {request.HttpVersion}");
            }
            else
            {
                sb.AppendLine($"{request.Method} {request.Url.PathAndQuery} {request.HttpVersion}");
            }

            foreach (var header in request.Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }

            sb.AppendLine(); // Empty line after headers

            if (request.Body.Length > 0)
            {
                sb.Append(Encoding.ASCII.GetString(request.Body));
            }

            return sb.ToString();
        }

        private string BuildResponseText(Core.Models.HttpResponseMessage response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{response.HttpVersion} {response.StatusCode} {response.ReasonPhrase}");

            foreach (var header in response.Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }

            sb.AppendLine(); // Empty line after headers

            if (response.Body.Length > 0)
            {
                sb.Append(Encoding.ASCII.GetString(response.Body));
            }

            return sb.ToString();
        }
    }
}
