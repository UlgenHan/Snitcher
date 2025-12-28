# ProxyServer

## Overview
The ProxyServer class is the main entry point for the HTTP/HTTPS proxy functionality in the Snitcher.Sniffer library. It implements a TCP listener that accepts incoming client connections and delegates their handling to a connection handler. This class serves as the orchestrator for all network traffic interception and flow capture operations.

**Why it exists:** To provide a high-level interface for starting and stopping a proxy server that can intercept HTTP/HTTPS traffic between clients and their intended destinations.

**Problem it solves:** Enables network traffic monitoring and interception by acting as a man-in-the-middle proxy that can capture, analyze, and modify HTTP requests and responses.

**What would break if removed:** The entire proxy functionality would cease to work. No network traffic could be intercepted, making the sniffer library non-functional for its primary purpose.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Net.Sockets (for TCP networking)
  - Microsoft.Extensions.Logging.Abstractions (for logging abstraction)
- **Persistence/communication:** TCP socket communication
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** Windows/Linux/macOS with .NET 8.0 runtime
- **Version hints:** TargetFramework net8.0, uses modern C# features (nullable reference types, async/await)

## Architectural Role
- **Layer:** Infrastructure/Networking layer
- **Responsibility boundaries:** 
  - MUST: Start/stop TCP listener, accept connections, delegate connection handling
  - MUST NOT: Process HTTP content directly, handle SSL/TLS, parse HTTP protocols
- **Dependencies:**
  - **Incoming:** IConnectionHandler (for connection processing), ILogger (for logging)
  - **Outgoing:** Events to FlowCaptured subscribers, TCP connections to clients

## Execution Flow
1. **Creation:** Constructor receives IConnectionHandler and ILogger dependencies, subscribes to FlowCaptured events
2. **Start:** StartAsync creates TcpListener, begins listening on configured address/port
3. **Accept Loop:** AcceptConnectionsAsync runs in background, continuously accepting TCP connections
4. **Connection Handling:** Each connection is offloaded to HandleConnectionSafe, which delegates to IConnectionHandler
5. **Event Propagation:** Flow events from connection handler are forwarded through FlowCaptured event
6. **Stop:** StopAsync cancels the accept loop and stops the TCP listener

**Synchronous vs asynchronous:** All public methods are async. Connection acceptance and handling are fully asynchronous with proper cancellation token support.

**Threading notes:** Uses Task.Run for background operations. Each connection is handled on its own task thread.

**Lifecycle:** Created → StartAsync → AcceptConnectionsAsync (running) → StopAsync → Disposed

## Public API / Surface Area
**Constructors:**
- `ProxyServer(IConnectionHandler connectionHandler, ILogger logger)` - Creates new instance with required dependencies

**Public Methods:**
- `Task StartAsync(ProxyOptions options, CancellationToken cancellationToken = default)` - Starts the proxy server
- `Task StopAsync(CancellationToken cancellationToken = default)` - Stops the proxy server

**Properties:**
- `bool IsRunning` - Indicates if the server is currently active

**Events:**
- `EventHandler<FlowEventArgs>? FlowCaptured` - Fired when a network flow is captured

**Expected input/output:**
- Input: ProxyOptions containing configuration (port, address, SSL settings)
- Output: Flow events containing captured HTTP request/response data

**Side effects:** Opens TCP port, accepts network connections, spawns background tasks

**Error behavior:** Throws InvalidOperationException if already running, ArgumentNullException for null parameters, propagates exceptions from network operations

## Internal Logic Breakdown
**Lines 19-26 (Constructor):**
- Validates and injects dependencies using null-checking pattern
- Subscribes to connection handler's FlowCaptured event for event forwarding

**Lines 34-61 (StartAsync):**
- Validates current state (not already running)
- Creates linked CancellationTokenSource for proper cancellation propagation
- Instantiates TcpListener with configured IP address and port
- Starts listener and sets IsRunning flag
- Logs successful start with address/port information
- Offloads connection acceptance to background task using Task.Run
- Returns immediately (non-blocking start)

**Lines 78-96 (AcceptConnectionsAsync):**
- Implements continuous accept loop with cancellation token checking
- Uses AcceptTcpClientAsync for non-blocking connection acceptance
- Offloads each connection to separate task for concurrent handling
- Catches OperationCanceledException for graceful shutdown
- Logs errors for failed connection acceptance

**Lines 98-112 (HandleConnectionSafe):**
- Wraps connection handling in try-catch for error isolation
- Delegates actual processing to IConnectionHandler
- Ensures TCP client is closed in finally block
- Logs errors with client endpoint information

**Lines 28-32, 114-115 (Event Handling):**
- Forwards flow events from connection handler to external subscribers
- Uses standard event invocation pattern with null-conditional operator

## Patterns & Principles Used
**Design Patterns:**
- **Observer Pattern:** FlowCaptured event for publish-subscribe notification
- **Dependency Injection:** Constructor injection of dependencies
- **Template Method:** Start/Stop lifecycle pattern

**Architectural Patterns:**
- **Proxy Pattern:** Acts as network proxy for traffic interception
- **Gateway Pattern:** Single entry point for proxy functionality

**Why these patterns were chosen:**
- Dependency injection enables testability and loose coupling
- Observer pattern allows multiple consumers of flow data without tight coupling
- Proxy pattern is inherent to the functionality (network proxy)

**Trade-offs:**
- Background task spawning may lead to thread pool pressure under high load
- Simple TCP listener approach limits advanced networking features

**Anti-patterns avoided:**
- No static state or singleton usage
- No blocking operations in async methods
- Proper resource cleanup with using patterns

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of IConnectionHandler and ILogger
- No service locator pattern used

**Configuration Sources:**
- ProxyOptions class contains all configuration parameters
- Default values provided for common scenarios (port 7865, localhost)

**Runtime Wiring:**
- Event subscription in constructor for flow event forwarding
- CancellationTokenSource linking for proper cancellation propagation

**Registration Points:**
- Designed to be registered as IProxyServer in DI container
- Singleton or scoped lifetime appropriate depending on usage

## Example Usage
**Minimal Example:**
```csharp
var connectionHandler = new ConnectionHandler(/* dependencies */);
var logger = new ConsoleLogger();
var proxyServer = new ProxyServer(connectionHandler, logger);

var options = new ProxyOptions { ListenPort = 8080 };
await proxyServer.StartAsync(options);

proxyServer.FlowCaptured += (sender, args) => 
    Console.WriteLine($"Captured: {args.Flow.Request.Url}");
```

**Realistic Example:**
```csharp
public class ProxyService
{
    private readonly IProxyServer _proxyServer;
    
    public ProxyService(IProxyServer proxyServer)
    {
        _proxyServer = proxyServer;
        _proxyServer.FlowCaptured += OnFlowCaptured;
    }
    
    public async Task StartMonitoringAsync()
    {
        var options = new ProxyOptions
        {
            ListenAddress = "0.0.0.0",
            ListenPort = 7865,
            InterceptHttps = true,
            CaCertificatePath = @"C:\certs\ca.pfx",
            CaPassword = "securepassword"
        };
        
        await _proxyServer.StartAsync(options);
    }
    
    private void OnFlowCaptured(object? sender, FlowEventArgs e)
    {
        // Process captured flow
        AnalyzeFlow(e.Flow);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Starting multiple times without stopping
await proxyServer.StartAsync(options);
await proxyServer.StartAsync(options); // Throws InvalidOperationException

// WRONG: Null dependencies
var server = new ProxyServer(null, null); // Throws ArgumentNullException

// WRONG: Not awaiting async operations
proxyServer.StartAsync(options); // Fire-and-forget, loses error handling
```

**How to test in isolation:**
```csharp
[Test]
public async Task StartStop_ShouldWorkCorrectly()
{
    // Arrange
    var mockHandler = new Mock<IConnectionHandler>();
    var mockLogger = new Mock<ILogger>();
    var server = new ProxyServer(mockHandler.Object, mockLogger.Object);
    var options = new ProxyOptions { ListenPort = 0 }; // Random port
    
    // Act & Assert
    Assert.That(server.IsRunning, Is.False);
    
    await server.StartAsync(options);
    Assert.That(server.IsRunning, Is.True);
    
    await server.StopAsync();
    Assert.That(server.IsRunning, Is.False);
}
```

**How to mock or replace it:**
```csharp
// Mock for testing
public class MockProxyServer : IProxyServer
{
    public bool IsRunning { get; private set; }
    public event EventHandler<FlowEventArgs>? FlowCaptured;
    
    public Task StartAsync(ProxyOptions options, CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }
    
    public void SimulateFlowCapture(Flow flow) => 
        FlowCaptured?.Invoke(this, new FlowEventArgs(flow));
}
```

## Extension & Modification Guide
**How to add new features:**
- **New configuration options:** Add properties to ProxyOptions class
- **Custom connection handling:** Implement IConnectionHandler with custom logic
- **Additional events:** Add new events following the same pattern as FlowCaptured

**Where NOT to add logic:**
- Don't add HTTP parsing logic to ProxyServer (delegate to IConnectionHandler)
- Don't add SSL/TLS handling (delegate to specialized components)
- Don't add business logic for flow processing (use event subscribers)

**Safe extension points:**
- Event handlers for FlowCaptured
- Custom IConnectionHandler implementations
- Inheritance for specialized proxy server types

**Common mistakes:**
- Adding blocking operations in StartAsync (prevents quick return)
- Not handling cancellation tokens properly
- Forgetting to dispose TCP resources
- Ignoring exception handling in background tasks

**Refactoring warnings:**
- Changing the async signature of StartAsync may break existing callers
- Removing FlowCaptured event will break all flow monitoring functionality
- Changing constructor dependencies requires DI container updates

## Failure Modes & Debugging
**Common runtime errors:**
- `InvalidOperationException`: Starting when already running
- `SocketException`: Port already in use or network issues
- `ArgumentNullException`: Null dependencies passed to constructor
- `ObjectDisposedException`: Using after StopAsync called

**Null/reference risks:**
- IConnectionHandler dependency is validated in constructor
- ProxyOptions validated in StartAsync
- TcpClient properly closed in finally block

**Performance risks:**
- High connection rates may exhaust thread pool
- Large number of concurrent connections may consume memory
- Blocking operations in connection handling affect scalability

**Logging points:**
- Server start with address/port information
- Server stop confirmation
- Connection acceptance errors with client endpoint
- Connection handling errors

**How to debug step-by-step:**
1. Set breakpoint in StartAsync to verify options
2. Check IsRunning property state transitions
3. Monitor AcceptConnectionsAsync loop for connection acceptance
4. Verify FlowCaptured event subscription and firing
5. Use network tools (netstat) to verify port binding
6. Test with simple HTTP client to verify connectivity

## Cross-References
**Related classes:**
- `IConnectionHandler` - Handles individual TCP connections
- `ProxyOptions` - Configuration data structure
- `FlowEventArgs` - Event data for captured flows
- `Flow` - Model representing captured HTTP transaction

**Upstream callers:**
- Application layer services that start/stop proxy
- UI components for proxy control
- Test fixtures for integration testing

**Downstream dependencies:**
- Connection handling infrastructure
- HTTP parsing components
- SSL/TLS interception modules

**Documents that should be read before/after:**
- Before: IConnectionHandler documentation (understanding connection processing)
- After: Flow model documentation (understanding captured data structure)
- Related: Certificate management documentation (for HTTPS interception)

## Knowledge Transfer Notes
**Reusable concepts:**
- Async TCP server pattern with cancellation token support
- Event-driven architecture for real-time data streaming
- Dependency injection for testable networking components
- Background task management with proper error isolation

**Project-specific elements:**
- Flow model structure (specific to this sniffer application)
- Integration with Snitcher's certificate management system
- Specific port defaults and configuration patterns

**How to recreate this pattern from scratch elsewhere:**
1. Create interface for the server contract (Start/Stop/IsRunning/Events)
2. Implement TCP listener with async acceptance loop
3. Use cancellation tokens for graceful shutdown
4. Delegate connection processing to injected dependency
5. Implement event forwarding for publish-subscribe pattern
6. Add comprehensive error handling and logging
7. Ensure proper resource cleanup in all scenarios

**Key architectural insights:**
- Separation of concerns between server management and connection processing
- Non-blocking start operation for responsive UI applications
- Event-driven design enables multiple consumers without coupling
- Background task isolation prevents cascading failures
