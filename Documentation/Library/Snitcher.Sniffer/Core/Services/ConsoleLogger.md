# ConsoleLogger

## Overview
The ConsoleLogger class is a simple logging implementation that outputs log messages to the system console with timestamps and log levels. It provides basic logging functionality including informational messages, warnings, errors with exceptions, and specialized flow logging for HTTP transaction tracking.

**Why it exists:** To provide a straightforward, dependency-free logging solution for development, testing, and simple deployment scenarios.

**Problem it solves:** Enables immediate visibility into proxy operations without requiring complex logging configuration or external logging frameworks.

**What would break if removed:** Applications using this specific logger would lose console output. Debugging and monitoring capabilities would be reduced unless another logger implementation is provided.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Console (for console output)
- **Persistence/communication:** Console output only
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** Console application or application with console access
- **Version hints:** Uses simple string formatting and console operations

## Architectural Role
- **Layer:** Infrastructure/Logging layer
- **Responsibility boundaries:**
  - MUST: Output formatted log messages to console, include timestamps
  - MUST NOT: Handle file logging, structured logging, log rotation
- **Dependencies:**
  - **Incoming:** None (pure logging implementation)
  - **Outgoing:** Console output

## Execution Flow
**Logging Flow:**
1. Log method called with message and optional parameters
2. Current timestamp formatted as "yyyy-MM-dd HH:mm:ss"
3. Log level prefix added ([INFO], [WARN], [ERROR])
4. Message formatted with parameters using string.Format
5. Complete message written to Console.WriteLine
6. For errors, exception details written on separate line

**Flow Logging Flow:**
1. LogFlow called with Flow object
2. Status indicator determined (but appears incomplete in current code)
3. Formatted message created with client address, method, URL, status code, and duration
4. Message written to console with [FLOW] prefix

**Synchronous vs asynchronous:** All methods are synchronous (console operations are inherently synchronous).

**Threading notes:** Console.WriteLine is thread-safe for concurrent access from multiple threads.

**Lifecycle:** Created per dependency injection → Logging methods called throughout application lifetime → No disposal needed

## Public API / Surface Area
**Constructors:**
- `ConsoleLogger()` - Creates console logger with default settings

**Public Methods:**
- `void LogInfo(string message, params object[] args)` - Logs informational message
- `void LogWarning(string message, params object[] args)` - Logs warning message
- `void LogError(Exception exception, string message, params object[] args)` - Logs error with exception details
- `void LogFlow(Flow flow)` - Logs HTTP flow information

**Expected input/output:**
- Input: Log messages, parameters, exceptions, flow objects
- Output: Formatted text written to system console

**Side effects:** Writes to system console, may affect console appearance and performance.

**Error behavior:** Console operations may throw exceptions if console is unavailable, but these are typically handled by the runtime.

## Internal Logic Breakdown
**Lines 8-11 (LogInfo):**
- Formats current timestamp as "yyyy-MM-dd HH:mm:ss"
- Adds [INFO] prefix to message
- Uses string.Format to substitute parameters
- Writes complete message to Console.WriteLine

**Lines 13-16 (LogWarning):**
- Identical to LogInfo but uses [WARN] prefix
- Same timestamp formatting and parameter substitution
- Writes to Console.WriteLine

**Lines 18-22 (LogError):**
- Formats message with [ERROR] prefix and timestamp
- Writes formatted message to Console.WriteLine
- Writes exception details on separate line using simple string conversion
- No exception handling for console write operations

**Lines 24-28 (LogFlow):**
- **Note:** Status indicator logic appears incomplete (hardcoded "?")
- Formats flow information with client address, HTTP method, URL, response status, and duration
- Uses [FLOW] prefix for easy identification
- Includes duration formatted with 0 decimal places

## Patterns & Principles Used
**Design Patterns:**
- **Adapter Pattern:** Adapts console output to logging interface
- **Simple Factory Pattern:** Creates formatted log messages

**Architectural Patterns:**
- **Console Logging Pattern:** Direct console output for simple logging needs
- **Synchronous Logging Pattern:** Immediate output without buffering

**Why these patterns were chosen:**
- Adapter pattern allows console output to implement logging interface
- Console logging provides immediate feedback during development
- Synchronous approach ensures log messages appear immediately

**Trade-offs:**
- No log level filtering (all messages are displayed)
- No structured logging or search capabilities
- Console output may not be available in all deployment scenarios
- Performance impact from console I/O operations

**Anti-patterns avoided:**
- No static global state (instance-based design)
- No complex configuration requirements
- No external dependencies

## Binding / Wiring / Configuration
**Dependency Injection:**
- No constructor dependencies (pure logging implementation)
- Simple registration as ILogger implementation

**Configuration Sources:**
- No external configuration
- Fixed timestamp format and message structure
- No runtime configuration changes

**Runtime Wiring:**
- No dynamic configuration
- Behavior is consistent and predictable
- Console output depends on application type

**Registration Points:**
- Should be registered as ILogger in DI container
- Singleton lifetime appropriate (shared logger instance)
- Can be replaced with more sophisticated loggers for production

## Example Usage
**Minimal Example:**
```csharp
var logger = new ConsoleLogger();

logger.LogInfo("Proxy server started on port {0}", 8080);
logger.LogWarning("Certificate not found, generating new one");
logger.LogError(ex, "Failed to start proxy server");
```

**Realistic Example in Proxy Context:**
```csharp
public class ProxyService
{
    private readonly ILogger _logger;
    
    public ProxyService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task StartAsync()
    {
        _logger.LogInfo("Initializing proxy service...");
        
        try
        {
            await StartProxyServer();
            _logger.LogInfo("Proxy server started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy server");
            throw;
        }
    }
    
    private void OnFlowCaptured(Flow flow)
    {
        _logger.LogFlow(flow);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Expecting log level filtering
var logger = new ConsoleLogger();
// All log levels will be displayed, no filtering available

// WRONG: Expecting structured logging
logger.LogInfo("User {userId} performed {action}", userId, action);
// Output will be formatted but not structured for analysis

// WRONG: Expecting async operations
await logger.LogInfo("Message"); // Compile error - methods are synchronous
```

**How to test in isolation:**
```csharp
[Test]
public void LogInfo_ShouldWriteFormattedMessageToConsole()
{
    // Arrange
    var originalOut = Console.Out;
    var stringWriter = new StringWriter();
    Console.SetOut(stringWriter);
    
    var logger = new ConsoleLogger();
    
    // Act
    logger.LogInfo("Test message with {0}", "parameter");
    
    // Assert
    var output = stringWriter.ToString();
    Assert.That(output, Does.Contain("[INFO]"));
    Assert.That(output, Does.Contain("Test message with parameter"));
    
    // Cleanup
    Console.SetOut(originalOut);
}
```

**How to mock or replace it:**
```csharp
public class MockLogger : ILogger
{
    public List<string> InfoMessages { get; } = new();
    public List<string> WarningMessages { get; } = new();
    public List<string> ErrorMessages { get; } = new();
    public List<Flow> LoggedFlows { get; } = new();
    
    public void LogInfo(string message, params object[] args)
    {
        InfoMessages.Add(string.Format(message, args));
    }
    
    public void LogWarning(string message, params object[] args)
    {
        WarningMessages.Add(string.Format(message, args));
    }
    
    public void LogError(Exception exception, string message, params object[] args)
    {
        ErrorMessages.Add($"{string.Format(message, args)} - {exception.Message}");
    }
    
    public void LogFlow(Flow flow)
    {
        LoggedFlows.Add(flow);
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Log levels:** Add different log levels (Debug, Trace, Critical)
- **Output formatting:** Add customizable timestamp formats and message templates
- **Color coding:** Add console colors for different log levels
- **File output:** Add optional file logging alongside console output

**Where NOT to add logic:**
- Don't add complex filtering logic (belongs in logging framework)
- Don't add network operations for remote logging
- Don't add business logic for log analysis

**Safe extension points:**
- Additional log methods for different message types
- Custom formatting options in existing methods
- Output destination configuration

**Common mistakes:**
- Adding blocking operations that slow down the application
- Not handling console availability issues
- Creating excessive console output that affects performance
- Ignoring thread safety considerations

**Refactoring warnings:**
- Changing method signatures will break ILogger interface compliance
- Removing timestamp formatting will reduce log usefulness
- Adding async operations will break existing caller expectations

## Failure Modes & Debugging
**Common runtime errors:**
- `IOException`: Console not available or redirected
- `UnauthorizedAccessException`: Insufficient permissions for console access
- `FormatException`: Invalid format strings or parameters

**Null/reference risks:**
- Message string assumed valid from callers
- Exception parameter assumed valid in LogError
- Flow object assumed valid in LogFlow

**Performance risks:**
- Console I/O operations can be slow under high message volume
- String formatting overhead for complex messages
- No buffering - each message written immediately

**Logging points:**
- All log levels output to console with timestamps
- Exception details logged separately from error messages
- Flow information includes timing and status details

**How to debug step-by-step:**
1. Verify console output is visible and not redirected
2. Check timestamp formatting for correctness
3. Test parameter substitution in formatted messages
4. Verify exception details are properly displayed
5. Check flow logging format and content
6. Test concurrent logging from multiple threads

## Cross-References
**Related classes:**
- `ILogger` - Interface defining logging contract
- `Flow` - Model representing HTTP transaction data
- All components using logging throughout the application

**Upstream callers:**
- All proxy components (ProxyServer, ConnectionHandler, etc.)
- Certificate management components
- Interceptor implementations
- Test fixtures for debugging

**Downstream dependencies:**
- System console for output
- String formatting infrastructure

**Documents that should be read before/after:**
- Before: ILogger interface documentation
- After: Component documentation showing logger usage
- Related: Advanced logging framework documentation for production

## Knowledge Transfer Notes
**Reusable concepts:**
- Simple console logging implementation
- Timestamp formatting for log messages
- Exception logging with details
- Parameterized message formatting
- Interface-based logging design

**Project-specific elements:**
- Snitcher's specific flow logging format
- Integration with Snitcher's component logging needs
- Specific timestamp and message formatting choices
- Console output as default logging mechanism

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for logging operations with different log levels
2. Create console logger implementation with timestamp formatting
3. Implement parameterized message formatting using string.Format
4. Add exception handling for error logging
5. Include specialized logging methods for domain-specific objects
6. Ensure thread safety for concurrent access
7. Design for dependency injection and easy replacement
8. Keep implementation simple for development and testing scenarios

**Key architectural insights:**
- Console logging provides immediate feedback during development
- Interface-based design allows easy replacement with production loggers
- Timestamp formatting is essential for debugging timing issues
- Parameterized messages enable flexible log content
- Simple implementations are valuable for testing and development
