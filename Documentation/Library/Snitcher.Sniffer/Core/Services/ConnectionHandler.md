# ConnectionHandler

## Overview
The ConnectionHandler class is the core component responsible for processing individual TCP connections received by the proxy server. It handles both HTTP and HTTPS traffic, parses requests/responses, applies interceptors, and manages the complete lifecycle of a network flow from client connection to response delivery.

**Why it exists:** To provide a unified handler for all proxy traffic, abstracting away the complexities of HTTP parsing, SSL/TLS handling, and request/response forwarding.

**Problem it solves:** Enables interception and modification of HTTP/HTTPS traffic by implementing a complete proxy workflow that can parse, process, and forward network requests while maintaining flow tracking and interceptor chains.

**What would break if removed:** The entire traffic processing pipeline would fail. No HTTP requests could be parsed, no HTTPS tunnels could be established, and no flow data would be captured or stored.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Net.Sockets (TCP networking)
  - System.Net.Security (SSL/TLS streams)
  - System.Security.Cryptography.X509Certificates (certificate handling)
  - Microsoft.Extensions.Logging.Abstractions (logging abstraction)
- **Persistence/communication:** TCP sockets, SSL streams, HTTP protocol parsing
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** Windows/Linux/macOS with .NET 8.0 runtime
- **Version hints:** Uses modern async/await patterns, nullable reference types, cancellation token support

## Architectural Role
- **Layer:** Application/Infrastructure layer (traffic processing)
- **Responsibility boundaries:**
  - MUST: Parse HTTP requests/responses, handle CONNECT methods, forward traffic, apply interceptors
  - MUST NOT: Manage TCP listening, handle certificate generation, implement business logic for flow analysis
- **Dependencies:**
  - **Incoming:** IHttpParser, ICertificateManager, IFlowStorage, IRequestInterceptor[], IResponseInterceptor[], ILogger
  - **Outgoing:** Flow events, network traffic to target servers, stored flow data

## Execution Flow
1. **Connection Entry:** HandleConnectionAsync receives TcpClient from ProxyServer
2. **Flow Creation:** Creates new Flow with client address and timestamp
3. **Request Parsing:** Parses initial HTTP request from client stream
4. **Interceptor Application:** Applies request interceptors in priority order
5. **Protocol Routing:** Routes to HTTPS CONNECT handler or HTTP request handler
6. **HTTPS Path:** Establishes tunnel or performs SSL interception
7. **HTTP Path:** Forwards request to target and forwards response back
8. **Response Processing:** Applies response interceptors
9. **Flow Completion:** Stores flow, updates status, raises FlowCaptured event

**Synchronous vs asynchronous:** Fully asynchronous with proper cancellation token support throughout.

**Threading notes:** Each connection handled independently on its own task thread. No shared state between connections.

**Lifecycle:** Created per dependency injection → HandleConnectionAsync (called per connection) → Flow stored and event raised → Method completes

## Public API / Surface Area
**Constructors:**
- `ConnectionHandler(IHttpParser httpParser, ICertificateManager certificateManager, IFlowStorage flowStorage, IEnumerable<IRequestInterceptor> requestInterceptors, IEnumerable<IResponseInterceptor> responseInterceptors, ILogger logger)` - Creates instance with all required dependencies

**Public Methods:**
- `Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken = default)` - Main entry point for processing a client connection

**Events:**
- `EventHandler<FlowEventArgs>? FlowCaptured` - Fired when a flow is completed and stored

**Expected input/output:**
- Input: TcpClient representing client connection
- Output: Flow data stored, FlowCaptured event raised with complete request/response information

**Side effects:** Creates network connections to target servers, modifies HTTP traffic via interceptors, stores flow data, generates log entries

**Error behavior:** Catches and logs exceptions, sets flow status to Failed, always stores flow and raises event even on failure

## Internal Logic Breakdown
**Lines 23-37 (Constructor):**
- Validates and injects all dependencies using null-checking pattern
- Dependencies include HTTP parser, certificate manager, flow storage, interceptor collections, and logger
- No complex initialization - all work is done per connection

**Lines 39-84 (HandleConnectionAsync - Main Method):**
- Extracts client endpoint for flow tracking
- Creates new Flow instance with client address
- Uses client stream with using statement for proper disposal
- Parses initial HTTP request using IHttpParser
- Applies request interceptors in priority order using LINQ OrderBy
- Routes to HTTPS CONNECT handler or HTTP handler based on request method
- Comprehensive try-catch for error handling with flow status management
- Finally block ensures flow storage, logging, and event raising regardless of success/failure

**Lines 86-134 (HandleHttpsConnectAsync):**
- Extracts target host and port from CONNECT request URL
- Logs HTTPS tunnel establishment attempt
- Creates TCP connection to target server with timeout support
- Sends "200 Connection established" response to client
- Implements bidirectional tunneling using CopyStreamAsync
- Uses Task.WhenAny to wait for tunnel completion in either direction
- Error handling creates 502 Bad Gateway response with error details

**Lines 136-211 (InterceptHttpsTrafficAsync - UNUSED):**
- **Note:** This method appears to be legacy/unimplemented code
- Intended for full HTTPS traffic interception with SSL stream handling
- Parses HTTPS requests from SSL stream, applies interceptors, forwards to target
- Contains complete request/response cycle with SSL stream management
- Currently not called from main flow - may be intended for future SSL interception

**Lines 213-222 (CopyStreamAsync):**
- Implements bidirectional stream copying for tunneling
- Uses 8KB buffer for efficient data transfer
- Reads from source, writes to destination until EOF
- Proper async/await with cancellation token support

**Lines 224-245 (HandleHttpRequestAsync):**
- Extracts target host and port from request URL (defaults to port 80)
- Logs HTTP request details
- Calls ForwardRequestToTarget for actual HTTP forwarding
- Stores response in flow object
- Sends response back to client using SendResponseAsync

**Lines 247-269 (ForwardRequestToTarget):**
- Creates TCP connection to target server
- Handles both HTTP and HTTPS target connections
- For HTTPS: Creates SslStream with certificate validation bypass
- Calls SendRequestAsync to transmit request
- Uses IHttpParser to parse response from target
- Returns parsed HttpResponseMessage

**Lines 271-301 (SendRequestAsync):**
- Constructs HTTP request line with method, path, and HTTP version
- Ensures Host header is present (adds if missing)
- Writes all headers in proper format
- Sends empty line to terminate headers section
- Writes request body if present
- Flushes stream to ensure transmission

**Lines 303-349 (SendResponseAsync):**
- Constructs HTTP status line with status code and reason phrase
- Automatically adds Content-Length header if body present
- Adds Connection: close header if not present
- Writes all headers in proper format
- Sends body if present
- Includes comprehensive logging of response details
- Error handling with re-throwing

## Patterns & Principles Used
**Design Patterns:**
- **Strategy Pattern:** Different handling strategies for HTTP vs HTTPS
- **Chain of Responsibility:** Interceptor chain for request/response processing
- **Template Method:** Standard flow processing pipeline with extension points
- **Factory Pattern:** Creates appropriate handlers based on request type

**Architectural Patterns:**
- **Proxy Pattern:** Acts as HTTP proxy client and server
- **Mediator Pattern:** Coordinates between multiple components (parser, interceptors, storage)
- **Pipeline Pattern:** Request flows through processing pipeline

**Why these patterns were chosen:**
- Strategy pattern enables clean separation between HTTP and HTTPS handling
- Chain of responsibility allows flexible interceptor composition
- Template method ensures consistent flow processing while allowing customization
- Pipeline pattern provides extensibility for traffic processing

**Trade-offs:**
- Complex interceptor chain may impact performance
- SSL interception method exists but unused (code debt)
- Synchronous certificate validation bypass for HTTPS targets

**Anti-patterns avoided:**
- No static state or shared mutable data between connections
- Proper resource disposal with using statements
- No blocking operations in async methods

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of all 6 dependencies
- Interceptor collections allow for zero or many interceptors
- No service locator pattern used

**Configuration Sources:**
- No direct configuration - behavior controlled by injected dependencies
- HTTP/HTTPS routing determined by request method
- Interceptor execution order determined by Priority property

**Runtime Wiring:**
- FlowCaptured event raised in finally block ensures notification
- Interceptor chains built at injection time
- No dynamic component creation during operation

**Registration Points:**
- Should be registered as IConnectionHandler in DI container
- Typically scoped or singleton depending on interceptor state
- Interceptors registered separately and injected as collections

## Example Usage
**Minimal Example:**
```csharp
var httpParser = new HttpParser();
var certManager = new CertificateManager();
var flowStorage = new InMemoryFlowStorage();
var requestInterceptors = new List<IRequestInterceptor>();
var responseInterceptors = new List<IResponseInterceptor>();
var logger = new ConsoleLogger();

var handler = new ConnectionHandler(
    httpParser, certManager, flowStorage, 
    requestInterceptors, responseInterceptors, logger);

await handler.HandleConnectionAsync(tcpClient);
```

**Realistic Example with Interceptors:**
```csharp
public class ProxyConnectionHandler
{
    private readonly IConnectionHandler _connectionHandler;
    
    public ProxyConnectionHandler(IConnectionHandler connectionHandler)
    {
        _connectionHandler = connectionHandler;
        _connectionHandler.FlowCaptured += OnFlowCaptured;
    }
    
    private void OnFlowCaptured(object? sender, FlowEventArgs e)
    {
        // Process captured flow
        if (e.Flow.Request.Headers.ContainsKey("Authorization"))
        {
            LogSecurityEvent(e.Flow);
        }
        
        // Store in database
        _database.SaveFlow(e.Flow);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Null dependencies
var handler = new ConnectionHandler(null, null, null, null, null, null);

// WRONG: Sharing TcpClient between handlers
await handler.HandleConnectionAsync(sharedTcpClient); // TcpClient should be unique per connection

// WRONG: Not awaiting the async method
handler.HandleConnectionAsync(tcpClient); // Fire-and-forget loses error handling
```

**How to test in isolation:**
```csharp
[Test]
public async Task HandleConnection_ShouldParseAndStoreFlow()
{
    // Arrange
    var mockParser = new Mock<IHttpParser>();
    var mockStorage = new Mock<IFlowStorage>();
    var mockLogger = new Mock<ILogger>();
    
    var request = new HttpRequestMessage { Method = HttpMethod.GET };
    var response = new HttpResponseMessage { StatusCode = 200 };
    
    mockParser.Setup(p => p.ParseRequestAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(request);
    mockParser.Setup(p => p.ParseResponseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(response);
    
    var handler = new ConnectionHandler(
        mockParser.Object, Mock.Of<ICertificateManager>(), 
        mockStorage.Object, Enumerable.Empty<IRequestInterceptor>(),
        Enumerable.Empty<IResponseInterceptor>(), mockLogger.Object);
    
    var tcpClient = CreateMockTcpClient("GET http://example.com HTTP/1.1\r\n\r\n");
    
    // Act
    await handler.HandleConnectionAsync(tcpClient);
    
    // Assert
    mockStorage.Verify(s => s.StoreFlowAsync(It.IsAny<Flow>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

**How to mock or replace it:**
```csharp
public class MockConnectionHandler : IConnectionHandler
{
    public List<Flow> CapturedFlows { get; } = new();
    
    public event EventHandler<FlowEventArgs>? FlowCaptured;
    
    public async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken = default)
    {
        var flow = new Flow 
        { 
            ClientAddress = "mock-client",
            Request = new HttpRequestMessage { Method = HttpMethod.GET },
            Response = new HttpResponseMessage { StatusCode = 200 }
        };
        
        await Task.Delay(10, cancellationToken); // Simulate processing
        FlowCaptured?.Invoke(this, new FlowEventArgs(flow));
        CapturedFlows.Add(flow);
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Custom protocols:** Add new handling methods alongside HandleHttpsConnectAsync and HandleHttpRequestAsync
- **Request modification:** Implement new IRequestInterceptor classes
- **Response processing:** Implement new IResponseInterceptor classes
- **Flow enrichment:** Extend Flow model or add post-processing in finally block

**Where NOT to add logic:**
- Don't add TCP listening logic (belongs in ProxyServer)
- Don't add certificate generation (belongs in ICertificateManager)
- Don't add business logic for flow analysis (use event subscribers)

**Safe extension points:**
- Interceptor implementations for request/response modification
- Event handlers for FlowCaptured for custom processing
- Inheritance for specialized connection handling

**Common mistakes:**
- Adding blocking operations that delay connection processing
- Not properly disposing network resources
- Modifying shared state in interceptor implementations
- Ignoring cancellation tokens in long-running operations

**Refactoring warnings:**
- Changing interceptor interface will break all interceptor implementations
- Modifying Flow model requires database schema updates
- Removing interceptor priority ordering will break deterministic processing

## Failure Modes & Debugging
**Common runtime errors:**
- `SocketException`: Target server unreachable or network issues
- `IOException`: Client disconnects during request/response processing
- `AuthenticationException`: SSL/TLS handshake failures
- `ArgumentException`: Malformed HTTP requests/responses

**Null/reference risks:**
- All dependencies validated in constructor
- TcpClient stream properly wrapped with using statement
- Flow properties initialized with default values

**Performance risks:**
- Large HTTP bodies may consume significant memory
- Slow target servers can block connection processing
- Complex interceptor chains may increase latency
- High connection rates may exhaust thread pool

**Logging points:**
- Connection start with client endpoint
- HTTPS tunnel establishment and completion
- HTTP request forwarding details
- Response sending with status and body size
- Error conditions with full exception details

**How to debug step-by-step:**
1. Set breakpoint in HandleConnectionAsync to trace connection entry
2. Monitor request parsing in IHttpParser.ParseRequestAsync
3. Verify interceptor chain execution order and modifications
4. Check HTTPS CONNECT method routing and tunnel establishment
5. Trace HTTP request forwarding and response parsing
6. Verify flow storage and event raising in finally block
7. Use network capture tools (Wireshark) to verify network traffic

## Cross-References
**Related classes:**
- `IHttpParser` - Parses HTTP requests and responses from streams
- `ICertificateManager` - Manages SSL certificates for HTTPS interception
- `IFlowStorage` - Persists captured flow data
- `IRequestInterceptor` - Modifies HTTP requests before forwarding
- `IResponseInterceptor` - Modifies HTTP responses before client delivery
- `Flow` - Data model representing captured HTTP transaction

**Upstream callers:**
- `ProxyServer` - Calls HandleConnectionAsync for each client connection
- Test fixtures - Direct calls for unit testing

**Downstream dependencies:**
- HTTP parsing components for protocol analysis
- Certificate management for SSL handling
- Storage systems for flow persistence
- Interceptor implementations for traffic modification

**Documents that should be read before/after:**
- Before: IHttpParser documentation (understanding HTTP parsing)
- Before: Interceptor interfaces documentation (understanding modification pipeline)
- After: Flow model documentation (understanding captured data structure)
- Related: NetworkConnectionHandler documentation (alternative implementation)

## Knowledge Transfer Notes
**Reusable concepts:**
- Async stream processing with proper cancellation token handling
- Pipeline pattern for request/response processing
- Bidirectional tunneling for TCP proxy functionality
- Comprehensive error handling with resource cleanup
- Event-driven architecture for real-time data streaming

**Project-specific elements:**
- Snitcher's Flow model structure for HTTP transaction tracking
- Integration with Snitcher's interceptor system for traffic modification
- Specific HTTP/HTTPS proxy implementation patterns
- Connection lifecycle management with flow tracking

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for connection handling with async method signature
2. Implement request parsing using stream reading and HTTP protocol knowledge
3. Create separate handling paths for different protocols (HTTP vs HTTPS)
4. Implement interceptor chain using dependency injection and ordering
5. Add comprehensive error handling with proper resource disposal
6. Implement flow tracking with timestamp and status management
7. Use events for decoupled notification of completed operations
8. Add detailed logging for debugging and monitoring

**Key architectural insights:**
- Separation of concerns between connection management and protocol handling
- Use of interceptor pattern for extensible traffic processing
- Proper async/await usage prevents thread blocking
- Event-driven design enables multiple consumers without coupling
- Comprehensive error handling ensures system stability under network failures
