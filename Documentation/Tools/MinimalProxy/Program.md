# Program.cs (MinimalProxy)

## Overview

Program.cs is a minimal HTTPS proxy implementation designed for testing and development purposes. It demonstrates the fundamental concepts of proxy server operation including TCP connection handling, HTTP/HTTPS protocol parsing, CONNECT method support for HTTPS tunneling, and basic stream forwarding. This tool serves as both a learning resource and a testing utility for the Snitcher project's proxy functionality.

**Why it exists**: To provide a simple, standalone proxy implementation that can be used to test proxy functionality, understand proxy protocols, and verify that client applications can correctly work with proxy servers without requiring the full Snitcher infrastructure.

**Problem it solves**: Without this minimal proxy, testing proxy functionality would require setting up complex proxy infrastructure or relying on external proxy services. This tool provides immediate, controllable proxy capability for development and testing.

**What would break if removed**: Development and testing of proxy functionality would be more difficult. Developers would lose a simple tool for verifying proxy behavior and testing client applications against a real proxy server.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0

**Libraries**: Microsoft.Extensions.Logging, System.Net.Sockets

**UI frameworks**: N/A (console application)

**Persistence / communication technologies**: TCP sockets, HTTP/HTTPS protocols, stream forwarding

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, network permissions, available ports

**Version hints**: Uses modern async networking patterns, structured logging, TCP socket programming

## Architectural Role

**Layer**: Tools Layer (Development Utility)

**Responsibility boundaries**:
- MUST implement basic proxy server functionality
- MUST handle both HTTP and HTTPS protocols
- MUST provide connection tunneling for HTTPS
- MUST NOT implement advanced features like authentication
- MUST NOT persist data or maintain state

**What it MUST do**:
- Listen for incoming TCP connections
- Parse HTTP requests and identify protocol type
- Handle CONNECT method for HTTPS tunneling
- Forward data between client and target servers
- Provide logging for debugging and monitoring

**What it MUST NOT do**:
- Implement complex authentication or authorization
- Store or analyze traffic content
- Provide advanced proxy features
- Handle production-scale loads

**Dependencies (incoming)**: Developer, test scripts, CI/CD pipelines

**Dependencies (outgoing)**: .NET networking stack, logging framework

## Execution Flow

**Where execution starts**: Main method is called when the application is launched from command line.

**How control reaches this component**:
1. Developer runs the executable from command line
2. Main method sets up logging and starts TCP listener
3. Application enters connection acceptance loop
4. Each connection handled in separate task

**Call sequence (step-by-step)**:
1. Main method initializes logging infrastructure
2. TCP listener created and started on port 8080
3. Application enters infinite loop accepting connections
4. Each connection passed to HandleClient method
5. HandleClient parses request and routes to appropriate handler
6. HandleConnect or HandleHttp processes the request
7. Stream copying handles data forwarding
8. Connection closed when complete

**Synchronous vs asynchronous behavior**: Mixed - Main loop synchronous, connection handling asynchronous

**Threading / dispatcher / event loop notes**: Each connection handled on separate thread pool task, main thread handles connection acceptance

**Lifecycle**: Started → Accepting Connections → Processing Requests → Shutdown on ENTER key

## Public API / Surface Area

**Entry Point**:
- `static async Task Main(string[] args)`: Application entry point

**Connection Handling**:
- `static async Task HandleClient(TcpClient client)`: Processes individual client connections
- `static async Task HandleConnect(NetworkStream clientStream, string target, string clientEndPoint)`: Handles HTTPS CONNECT method
- `static async Task HandleHttp(NetworkStream clientStream, string request, string clientEndPoint)`: Handles HTTP requests

**Utility Methods**:
- `static async Task CopyStreamAsync(Stream source, Stream destination)`: Copies data between streams

**Expected input/output**:
- Input: TCP connections, HTTP requests, CONNECT requests
- Output: Forwarded data, HTTP responses, tunnel connections

**Side effects**:
- Opens network socket and listens for connections
- Creates outbound connections to target servers
- Logs all operations to console and logger

**Error behavior**: Logs errors and continues operating, sends error responses to clients when possible

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Main method (lines 12-48)**:
```csharp
static async Task Main(string[] args)
{
    // Setup logging
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    _logger = loggerFactory.CreateLogger<Program>();

    Console.WriteLine("🔧 Minimal HTTPS Proxy Test");
    Console.WriteLine("==========================");

    var proxy = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
    
    try
    {
        proxy.Start();
        Console.WriteLine("✅ Proxy started on http://localhost:8080");
        Console.WriteLine("📝 Test with: curl -x http://localhost:8080 --insecure https://httpbin.org/get");
        Console.WriteLine("⏸️  Press ENTER to stop...\n");

        while (true)
        {
            var client = await proxy.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }
    catch (Exception ex)
    {
        _logger!.LogError(ex, "Proxy error");
    }
    finally
    {
        proxy.Stop();
    }
}
```
- Sets up structured logging with console output
- Creates TCP listener on localhost:8080
- Provides user-friendly startup messages and test instructions
- Enters infinite loop accepting connections
- Handles each connection in separate background task
- Provides graceful shutdown with finally block

**HandleClient method (lines 50-92)**:
```csharp
static async Task HandleClient(TcpClient client)
{
    var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
    _logger!.LogInformation("🔗 New connection from {Client}", clientEndPoint);

    try
    {
        using var clientStream = client.GetStream();
        
        // Read the HTTP request
        var buffer = new byte[4096];
        var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        _logger.LogInformation("📨 Request:\n{Request}", request.Split('\n')[0]);

        // Parse the request
        var lines = request.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;

        var requestLine = lines[0];
        var parts = requestLine.Split(' ');

        if (parts.Length >= 2 && parts[0].ToUpper() == "CONNECT")
        {
            // Handle HTTPS CONNECT
            await HandleConnect(clientStream, parts[1], clientEndPoint);
        }
        else if (parts.Length >= 3)
        {
            // Handle HTTP GET/POST
            await HandleHttp(clientStream, request, clientEndPoint);
        }
    }
    catch (Exception ex)
    {
        _logger!.LogError(ex, "Error handling client {Client}", clientEndPoint);
    }
    finally
    {
        client.Close();
    }
}
```
- Extracts client endpoint for logging
- Reads initial request data from client
- Parses HTTP request line to determine protocol
- Routes to appropriate handler based on request type
- Implements comprehensive error handling and logging
- Ensures client connection is closed

**HandleConnect method (lines 94-134)**:
```csharp
static async Task HandleConnect(NetworkStream clientStream, string target, string clientEndPoint)
{
    try
    {
        // Parse target host:port
        var targetParts = target.Split(':');
        var host = targetParts[0];
        var port = int.Parse(targetParts[1]);

        _logger!.LogInformation("🔒 HTTPS CONNECT to {Host}:{Port}", host, port);

        // Connect to target
        using var targetClient = new TcpClient();
        await targetClient.ConnectAsync(host, port);
        using var targetStream = targetClient.GetStream();

        // Send 200 Connection established
        var response = "HTTP/1.1 200 Connection established\r\n\r\n";
        var responseBytes = Encoding.UTF8.GetBytes(response);
        await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);

        _logger!.LogInformation("✅ Tunnel established for {Client}", clientEndPoint);

        // Create tunnel - copy data both ways
        var clientToTarget = CopyStreamAsync(clientStream, targetStream);
        var targetToClient = CopyStreamAsync(targetStream, clientStream);

        await Task.WhenAny(clientToTarget, targetToClient);

        _logger!.LogInformation("🔚 Tunnel closed for {Client}", clientEndPoint);
    }
    catch (Exception ex)
    {
        _logger!.LogError(ex, "CONNECT failed for {Client}", clientEndPoint);
        
        // Send error response
        var errorResponse = "HTTP/1.1 502 Bad Gateway\r\n\r\n";
        var errorBytes = Encoding.UTF8.GetBytes(errorResponse);
        await clientStream.WriteAsync(errorBytes, 0, errorBytes.Length);
    }
}
```
- Parses CONNECT target (host:port format)
- Establishes TCP connection to target server
- Sends HTTP 200 response to establish tunnel
- Creates bidirectional data forwarding tunnel
- Uses Task.WhenAny to handle tunnel completion
- Provides error handling with appropriate HTTP error responses

**HandleHttp method (lines 136-151)**:
```csharp
static async Task HandleHttp(NetworkStream clientStream, string request, string clientEndPoint)
{
    try
    {
        // Simple HTTP response for testing
        var response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nProxy HTTP response";
        var responseBytes = Encoding.UTF8.GetBytes(response);
        await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);

        _logger!.LogInformation("📡 HTTP response sent to {Client}", clientEndPoint);
    }
    catch (Exception ex)
    {
        _logger!.LogError(ex, "HTTP failed for {Client}", clientEndPoint);
    }
}
```
- Provides simple HTTP response for testing
- Doesn't actually forward HTTP requests (simplified for demo)
- Includes proper HTTP headers and content type
- Logs successful response delivery

**CopyStreamAsync method (lines 153-169)**:
```csharp
static async Task CopyStreamAsync(Stream source, Stream destination)
{
    var buffer = new byte[8192];
    int bytesRead;

    try
    {
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead);
        }
    }
    catch (Exception ex)
    {
        _logger!.LogDebug(ex, "Stream copy ended");
    }
}
```
- Implements efficient stream copying with buffering
- Uses 8KB buffer for optimal performance
- Handles graceful termination on connection closure
- Logs debug information for troubleshooting

**Algorithms used**:
- HTTP request parsing and routing
- TCP connection management
- Bidirectional stream forwarding
- Buffer-based data copying

**Conditional logic explanation**:
- Request type detection based on HTTP method
- Error handling with appropriate HTTP responses
- Connection lifecycle management with try/finally
- Tunnel completion detection with Task.WhenAny

**State transitions**:
- Client Connected → Request Parsed → Handler Selected → Response Sent → Connection Closed

**Important invariants**:
- All connections are properly closed
- Errors are logged but don't crash server
- HTTP responses follow proper protocol format
- Tunnel operates bidirectionally until one side closes

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Proxy Pattern**: Acts as intermediary for client-server communication
- **Connection Handling Pattern**: Separate task per connection
- **Stream Processing Pattern**: Buffer-based data forwarding
- **Error Handling Pattern**: Graceful degradation with logging

**Network programming patterns**:
- **TCP Socket Programming**: Low-level network communication
- **HTTP Protocol Handling**: Request parsing and response generation
- **Tunneling Pattern**: Transparent data forwarding for HTTPS

**Why these patterns were chosen (inferred)**:
- Proxy pattern enables transparent network interception
- Connection per task prevents blocking
- Stream processing handles arbitrary data sizes
- Error handling ensures server stability

**Trade-offs**:
- Simple HTTP handling vs full forwarding: Easier but less functional
- Per-connection tasks vs thread pool: More scalable but complex
- Minimal logging vs detailed debugging: Simpler but less informative

**Anti-patterns avoided or possibly introduced**:
- Avoided: Blocking network operations
- Avoided: Resource leaks from unclosed connections
- Possible risk: Infinite loop without graceful shutdown

## Binding / Wiring / Configuration

**Network configuration**: Hardcoded localhost:8080 binding

**Logging configuration**: Console logging with Information level

**Runtime configuration**: Command-line arguments ignored (simplified)

**Network protocols**: HTTP/1.1, HTTPS via CONNECT method

**Error handling**: Exception logging with graceful degradation

## Example Usage (CRITICAL)

**Running the proxy**:
```bash
# Start the proxy server
dotnet run --project MinimalProxy

# Or run compiled executable
./MinimalProxy.exe
```

**Testing HTTP requests**:
```bash
# Test HTTP request through proxy
curl -x http://localhost:8080 http://example.com

# Expected response: "Proxy HTTP response"
```

**Testing HTTPS tunneling**:
```bash
# Test HTTPS through proxy tunnel
curl -x http://localhost:8080 --insecure https://httpbin.org/get

# Should return actual response from httpbin.org through tunnel
```

**Testing with browsers**:
```
1. Start the proxy server
2. Configure browser to use HTTP proxy 127.0.0.1:8080
3. Navigate to websites
4. Monitor console output for connection logs
```

**Incorrect usage example (and why it is wrong)**:
```bash
# WRONG: Using privileged ports without admin rights
# Modify code to use port 80 - will fail without admin

# WRONG: Expecting production features
# This proxy doesn't support authentication, caching, or advanced features

# WRONG: Assuming HTTP request forwarding
# HTTP requests get simple responses, not actual forwarding
```

**Development and debugging**:
```csharp
// Add more detailed logging for debugging
_logger.LogDebug("Raw request: {Request}", request);

// Add connection timeout handling
client.ReceiveTimeout = 30000; // 30 seconds

// Add request validation
if (string.IsNullOrWhiteSpace(request)) return;
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new request type detection in HandleClient
2. Implement new handler method for the protocol
3. Add appropriate logging and error handling
4. Test with appropriate client tools
5. Update usage documentation

**Where NOT to add logic**:
- Don't add complex authentication systems
- Don't implement persistent storage
- Don't add production-scale optimizations
- Don't include advanced security features

**Safe extension points**:
- Additional protocol handlers (FTP, SOCKS, etc.)
- Enhanced logging and monitoring
- Request/response modification capabilities
- Connection limiting and throttling
- Basic authentication support

**Common mistakes**:
- Forgetting to handle connection cleanup
- Blocking the main thread with network operations
- Not handling partial reads/writes correctly
- Ignoring buffer size limitations
- Not implementing proper error responses

**Refactoring warnings**:
- Changing port binding affects all clients
- Modifying protocol handling breaks compatibility
- Adding blocking operations can cause performance issues
- Removing error handling can cause crashes

## Failure Modes & Debugging

**Common runtime errors**:
- **SocketException**: Port already in use or network errors
- **IOException**: Connection failures or stream errors
- **FormatException**: Invalid target parsing in CONNECT
- **ObjectDisposedException**: Using closed connections

**Network debugging strategies**:
- Monitor console output for connection logs
- Use network tools like Wireshark for packet analysis
- Test with simple clients like curl
- Check port availability with netstat

**Performance issues**:
- Too many concurrent connections
- Large buffer sizes causing memory pressure
- Inefficient stream copying algorithms
- Blocking operations on main thread

**Security considerations**:
- No authentication or authorization
- Plain text logging of sensitive data
- No connection limiting or rate limiting
- Open proxy could be abused

**How to debug step-by-step**:
1. Check if port 8080 is available
2. Verify network permissions
3. Monitor console output for connection attempts
4. Test with simple HTTP requests first
5. Use curl with verbose output for debugging
6. Check firewall settings if connections fail

## Cross-References

**Related classes**:
- TcpListener (network listener)
- TcpClient (client connections)
- NetworkStream (data streaming)
- ILogger (logging interface)

**Upstream callers**:
- Developer command line
- Test scripts
- CI/CD pipelines

**Downstream dependencies**:
- Target servers (connected to via tunnel)
- Client applications (using the proxy)
- Network infrastructure

**Documents that should be read before/after**:
- Read: HTTP/1.1 protocol specification
- Read: HTTPS tunneling documentation
- Read: TCP socket programming guides
- Read: .NET networking best practices

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- TCP socket server implementation
- HTTP protocol parsing basics
- HTTPS tunneling concepts
- Asynchronous network programming
- Stream processing patterns
- Error handling in network applications

**What is project-specific**:
- Specific port configuration (8080)
- Simplified HTTP response handling
- Particular logging format and messages
- Snitcher-specific testing scenarios

**How to recreate this pattern from scratch elsewhere**:
1. Create TCP listener on appropriate port
2. Implement connection acceptance loop
3. Parse incoming HTTP requests
4. Route to appropriate protocol handlers
5. Implement CONNECT method for HTTPS tunneling
6. Add bidirectional stream forwarding
7. Include comprehensive error handling
8. Add logging for debugging and monitoring

**Key insights for implementation**:
- Always use async operations for network I/O
- Handle each connection in separate task to avoid blocking
- Implement proper resource cleanup with using/finally blocks
- Use appropriate buffer sizes for performance
- Log connection events for debugging
- Send proper HTTP error responses when needed
- Consider security implications of open proxies
