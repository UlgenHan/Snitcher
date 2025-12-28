# HttpParser

## Overview
The HttpParser class is a comprehensive HTTP protocol parser responsible for converting raw network streams into structured HTTP request and response objects, and vice versa. It handles the complete HTTP/1.1 protocol including headers, body parsing, chunked transfer encoding, and special CONNECT method handling for HTTPS tunneling.

**Why it exists:** To provide a reliable, standards-compliant HTTP parser that can handle real-world network traffic including edge cases and various transfer encodings.

**Problem it solves:** Enables the proxy to understand and manipulate HTTP traffic by parsing raw bytes into structured objects that can be analyzed, modified, and reconstructed.

**What would break if removed:** The proxy would be unable to understand HTTP traffic. No requests or responses could be parsed, making the entire interception functionality non-functional.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.IO (Stream, MemoryStream, StreamReader)
  - System.Text (Encoding, StringBuilder)
  - System.Globalization (NumberStyles for hex parsing)
- **Persistence/communication:** Stream-based I/O for network data
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** .NET 8.0 runtime, network stream compatibility
- **Version hints:** Uses modern async patterns, cancellation token support

## Architectural Role
- **Layer:** Infrastructure/Protocol layer
- **Responsibility boundaries:**
  - MUST: Parse HTTP requests/responses, handle chunked encoding, support CONNECT method
  - MUST NOT: Make network connections, handle SSL/TLS, implement business logic
- **Dependencies:**
  - **Incoming:** ILogger (for debugging and monitoring)
  - **Outgoing:** Structured HTTP objects, log entries

## Execution Flow
**Request Parsing Flow:**
1. Read stream until blank line (end of headers)
2. Convert bytes to ASCII string for header parsing
3. Parse request line (method, URL, version)
4. Parse headers into dictionary
5. Handle CONNECT method special case
6. Update URL for relative requests using Host header
7. Return structured HttpRequestMessage

**Response Parsing Flow:**
1. Read stream until blank line (end of headers)
2. Parse status line and headers
3. Determine body reading strategy based on headers
4. Read body using Content-Length, chunked encoding, or remaining data
5. Return structured HttpResponseMessage with body

**Writing Flow:**
1. Convert structured objects to text format
2. Handle CONNECT method special case for requests
3. Write to stream with proper encoding

**Synchronous vs asynchronous:** All public methods are async with proper cancellation token support.

**Threading notes:** Stateless implementation - safe for concurrent use from multiple threads.

**Lifecycle:** Created per dependency injection → Parse/Write methods called repeatedly → No disposal needed

## Public API / Surface Area
**Constructors:**
- `HttpParser(ILogger logger)` - Creates parser with logging capability

**Public Methods:**
- `Task<HttpRequestMessage> ParseRequestAsync(Stream stream, CancellationToken cancellationToken = default)` - Parses HTTP request from network stream
- `Task<HttpResponseMessage> ParseResponseAsync(Stream stream, CancellationToken cancellationToken = default)` - Parses HTTP response from network stream
- `Task WriteRequestAsync(HttpRequestMessage request, Stream stream, CancellationToken cancellationToken = default)` - Writes HTTP request to stream
- `Task WriteResponseAsync(HttpResponseMessage response, Stream stream, CancellationToken cancellationToken = default)` - Writes HTTP response to stream

**Expected input/output:**
- Input: Network streams containing raw HTTP data
- Output: Structured HttpRequestMessage/HttpResponseMessage objects
- Input for writing: Structured HTTP objects
- Output for writing: Raw bytes written to network streams

**Side effects:** Reads from and writes to network streams, generates log entries

**Error behavior:** Throws InvalidOperationException for malformed HTTP, propagates stream I/O exceptions, logs parsing progress

## Internal Logic Breakdown
**Lines 19-42 (ParseRequestAsync):**
- Creates MemoryStream buffer to accumulate header data
- Reads stream in 1KB chunks until blank line found
- Uses ASCII encoding for HTTP protocol compliance
- Logs raw request for debugging
- Delegates to ParseRequestText for actual parsing

**Lines 44-147 (ParseResponseAsync):**
- Similar header reading pattern as request parsing
- Logs response headers for debugging
- Calls ParseResponseText for header parsing
- Implements three body reading strategies:
  1. Content-Length: Reads exact number of bytes
  2. Chunked encoding: Parses chunk size lines and data
  3. Fallback: Reads all remaining data
- Detailed logging for body reading progress
- Comprehensive error handling for premature stream closure

**Lines 149-167 (ReadLineAsync):**
- Helper method for reading lines ending in \n
- Reads one byte at a time until newline found
- Trims \r and \n from returned line
- Returns null if stream ends before any data

**Lines 169-187 (WriteRequestAsync/WriteResponseAsync):**
- Convert structured objects to text using helper methods
- Encode as ASCII and write to stream
- Log outgoing messages for debugging

**Lines 189-262 (ParseRequestText):**
- Splits request into lines by \r\n
- Parses request line: METHOD URL VERSION
- Special handling for CONNECT method vs regular HTTP
- For CONNECT: Creates fake HTTPS URL from host:port
- For regular requests: Handles relative vs absolute URLs
- Parses headers into dictionary with name: value format
- Updates relative URLs using Host header
- Validates request line format

**Lines 264-298 (ParseResponseText):**
- Parses status line: VERSION STATUS REASON
- Handles minimal status line (VERSION STATUS)
- Provides default "OK" reason phrase if missing
- Parses headers using same pattern as request parsing

**Lines 300-348 (BuildRequestText/BuildResponseText):**
- Constructs HTTP text from structured objects
- Special CONNECT handling in request building
- Includes headers in name: value format
- Adds blank line after headers
- Appends body if present, encoded as ASCII

## Patterns & Principles Used
**Design Patterns:**
- **Strategy Pattern:** Different body reading strategies based on headers
- **Template Method:** Standard parsing flow with extension points
- **Builder Pattern:** Text building methods for HTTP messages

**Architectural Patterns:**
- **Parser Pattern:** Converts unstructured data to structured objects
- **Serializer Pattern:** Converts objects back to text format

**Why these patterns were chosen:**
- Strategy pattern enables flexible body reading based on transfer encoding
- Template method ensures consistent parsing while allowing customization
- Parser/serializer pattern provides clean separation of concerns

**Trade-offs:**
- ASCII encoding limits support for non-ASCII header values
- MemoryStream usage may consume memory for large headers
- Byte-by-byte reading in ReadLineAsync is inefficient for long lines

**Anti-patterns avoided:**
- No regular expressions for parsing (more fragile)
- No blocking operations in async methods
- Proper resource disposal with using statements

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of ILogger for debugging
- No other dependencies - pure parsing logic

**Configuration Sources:**
- No external configuration
- Behavior controlled by HTTP protocol standards
- Encoding fixed to ASCII for HTTP compliance

**Runtime Wiring:**
- No state to configure
- Thread-safe design allows singleton registration
- No dynamic component creation

**Registration Points:**
- Should be registered as IHttpParser in DI container
- Singleton lifetime appropriate (stateless)
- Used by ConnectionHandler for all HTTP parsing needs

## Example Usage
**Minimal Example:**
```csharp
var logger = new ConsoleLogger();
var parser = new HttpParser(logger);

// Parse request from network stream
var request = await parser.ParseRequestAsync(networkStream);

// Parse response from network stream
var response = await parser.ParseResponseAsync(networkStream);

// Write request to network stream
await parser.WriteRequestAsync(request, networkStream);
```

**Realistic Example with Error Handling:**
```csharp
public class HttpMessageProcessor
{
    private readonly IHttpParser _parser;
    
    public HttpMessageProcessor(IHttpParser parser)
    {
        _parser = parser;
    }
    
    public async Task<bool> ProcessRequestAsync(Stream clientStream, Stream serverStream)
    {
        try
        {
            // Parse client request
            var request = await _parser.ParseRequestAsync(clientStream);
            
            // Forward request to server
            await _parser.WriteRequestAsync(request, serverStream);
            
            // Parse server response
            var response = await _parser.ParseResponseAsync(serverStream);
            
            // Forward response to client
            await _parser.WriteResponseAsync(response, clientStream);
            
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid HTTP message format");
            return false;
        }
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Using non-ASCII encoding for HTTP headers
var utf8Bytes = Encoding.UTF8.GetBytes("Header: Value\r\n");

// WRONG: Not handling chunked encoding properly
if (response.Headers.ContainsKey("Transfer-Encoding"))
{
    // Naive implementation that breaks on chunked data
    var body = await ReadAllRemainingBytes(stream);
}

// WRONG: Mixing stream operations
var request = await parser.ParseRequestAsync(stream);
stream.Seek(0, SeekOrigin.Begin); // Stream already consumed
```

**How to test in isolation:**
```csharp
[Test]
public async Task ParseRequest_ShouldHandleConnectMethod()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var parser = new HttpParser(logger.Object);
    
    var connectRequest = "CONNECT example.com:443 HTTP/1.1\r\n" +
                        "Host: example.com:443\r\n" +
                        "Proxy-Connection: Keep-Alive\r\n" +
                        "\r\n";
    
    var stream = new MemoryStream(Encoding.ASCII.GetBytes(connectRequest));
    
    // Act
    var request = await parser.ParseRequestAsync(stream);
    
    // Assert
    Assert.That(request.Method, Is.EqualTo(HttpMethod.Connect));
    Assert.That(request.Url.Host, Is.EqualTo("example.com"));
    Assert.That(request.Url.Port, Is.EqualTo(443));
    Assert.That(request.Url.Scheme, Is.EqualTo("https"));
}
```

**How to mock or replace it:**
```csharp
public class MockHttpParser : IHttpParser
{
    public HttpRequestMessage NextRequest { get; set; }
    public HttpResponseMessage NextResponse { get; set; }
    
    public Task<HttpRequestMessage> ParseRequestAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NextRequest ?? new HttpRequestMessage 
        { 
            Method = HttpMethod.GET,
            Url = new Uri("http://example.com")
        });
    }
    
    public Task<HttpResponseMessage> ParseResponseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NextResponse ?? new HttpResponseMessage 
        { 
            StatusCode = 200,
            ReasonPhrase = "OK"
        });
    }
    
    public Task WriteRequestAsync(HttpRequestMessage request, Stream stream, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    public Task WriteResponseAsync(HttpResponseMessage response, Stream stream, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **New transfer encodings:** Add new branches in response body parsing section
- **HTTP/2 support:** Create new parser implementation or extend existing
- **Header validation:** Add validation logic in parsing methods
- **Compression support:** Add Content-Encoding handling in body parsing

**Where NOT to add logic:**
- Don't add network connection logic (belongs in connection handlers)
- Don't add SSL/TLS handling (belongs in SSL components)
- Don't add business logic for content analysis

**Safe extension points:**
- New body reading strategies in ParseResponseAsync
- Header processing extensions in parsing methods
- Custom encoding support in text building methods

**Common mistakes:**
- Changing encoding from ASCII (breaks HTTP standard compliance)
- Adding blocking operations in async methods
- Not properly handling edge cases in header parsing
- Ignoring cancellation tokens in long-running operations

**Refactoring warnings:**
- Changing header parsing format will break HTTP compatibility
- Modifying CONNECT method handling will break HTTPS tunneling
- Removing chunked encoding support will break modern web servers

## Failure Modes & Debugging
**Common runtime errors:**
- `InvalidOperationException`: Malformed HTTP request/response format
- `ArgumentException`: Invalid URL format in request line
- `FormatException`: Invalid hex number in chunked encoding
- `IOException`: Network stream failures during reading/writing

**Null/reference risks:**
- Logger validated in constructor
- Stream parameters not null-checked (assumed validated by caller)
- Return objects always fully initialized

**Performance risks:**
- Large headers may consume significant memory in MemoryStream
- Chunked encoding parsing may be slow for large responses
- Byte-by-byte reading in ReadLineAsync is inefficient
- ASCII string conversions for large bodies

**Logging points:**
- Raw request text for debugging
- Response header information
- Body reading progress and strategies
- Chunked encoding parsing details
- Final parsing results with body sizes

**How to debug step-by-step:**
1. Enable debug logging to see raw HTTP messages
2. Set breakpoint in ParseRequestText to trace request line parsing
3. Monitor header parsing loop for malformed headers
4. Check body reading strategy selection in ParseResponseAsync
5. Verify chunked encoding parsing for chunked responses
6. Test with various HTTP methods and header combinations
7. Use network capture tools to verify stream data matches expectations

## Cross-References
**Related classes:**
- `HttpRequestMessage` - Model representing parsed HTTP requests
- `HttpResponseMessage` - Model representing parsed HTTP responses
- `HttpMethod` - Enumeration of HTTP methods
- `IHttpParser` - Interface defining parser contract

**Upstream callers:**
- `ConnectionHandler` - Uses parser for all HTTP request/response processing
- Test fixtures - Direct calls for unit testing HTTP parsing logic

**Downstream dependencies:**
- HTTP model classes for data representation
- Logging infrastructure for debugging and monitoring

**Documents that should be read before/after:**
- Before: HttpRequestMessage/HttpResponseMessage model documentation
- Before: HttpMethod enumeration documentation
- After: ConnectionHandler documentation (parser usage context)
- Related: HTTP protocol specification RFC 7230

## Knowledge Transfer Notes
**Reusable concepts:**
- Stream-based parsing with proper async/await patterns
- HTTP/1.1 protocol implementation details
- Chunked transfer encoding parsing algorithm
- CONNECT method handling for HTTPS tunneling
- Header parsing with validation and error handling

**Project-specific elements:**
- Snitcher's HTTP model structure
- Integration with Snitcher's logging system
- Specific CONNECT method handling for proxy functionality
- Body reading strategy selection based on headers

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for parsing and writing HTTP messages
2. Implement request parsing with proper line ending handling
3. Add response parsing with status line and header processing
4. Implement body reading strategies (Content-Length, chunked, fallback)
5. Add CONNECT method special handling for proxy scenarios
6. Create text building methods for serialization
7. Add comprehensive error handling and validation
8. Include detailed logging for debugging and monitoring

**Key architectural insights:**
- HTTP protocol parsing requires careful handling of line endings and encodings
- Multiple body reading strategies needed for real-world compatibility
- CONNECT method requires special URL handling for HTTPS tunneling
- Stream-based parsing enables efficient memory usage for large messages
- Comprehensive logging is essential for debugging protocol issues
