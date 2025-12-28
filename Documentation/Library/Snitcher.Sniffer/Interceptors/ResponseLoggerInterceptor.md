# ResponseLoggerInterceptor

## Overview
The ResponseLoggerInterceptor class is a response interceptor that logs detailed information about HTTP responses for debugging and monitoring purposes. It captures response metadata, headers, and selectively logs response bodies for text-based content, providing comprehensive visibility into proxy traffic.

**Why it exists:** To provide detailed logging of HTTP responses for debugging, monitoring, and audit purposes without requiring external tools.

**Problem it solves:** Enables real-time visibility into HTTP response traffic, including status codes, headers, and content details, which is essential for debugging proxy issues and monitoring application behavior.

**What would break if removed:** Users would lose detailed response logging capabilities. Debugging HTTP response issues would become more difficult, and monitoring capabilities would be reduced.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Text (Encoding for body text conversion)
  - Microsoft.Extensions.Logging.Abstractions (logging abstraction)
- **Persistence/communication:** Logging infrastructure only
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** .NET 8.0 runtime
- **Version hints:** Uses modern async patterns, cancellation token support

## Architectural Role
- **Layer:** Application/Interceptor layer
- **Responsibility boundaries:**
  - MUST: Log response information, conditionally log response bodies
  - MUST NOT: Modify responses, affect request processing, handle network connections
- **Dependencies:**
  - **Incoming:** ILogger (for output)
  - **Outgoing:** Log entries through logging infrastructure

## Execution Flow
1. InterceptAsync receives HTTP response and flow context
2. Logs basic response information (status code, reason phrase, key headers)
3. Checks if response body should be logged based on size and content type
4. If conditions are met, converts response body to text and logs it
5. Returns unmodified response (this interceptor is read-only)

**Synchronous vs asynchronous:** Method is async but returns Task.FromResult for immediate completion.

**Threading notes:** Stateless design - safe for concurrent use. No shared state between method calls.

**Lifecycle:** Created per dependency injection → InterceptAsync called for each response → No disposal needed

## Public API / Surface Area
**Constructors:**
- `ResponseLoggerInterceptor(ILogger logger)` - Creates interceptor with logging capability

**Public Methods:**
- `Task<HttpResponseMessage> InterceptAsync(HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)` - Logs response information

**Properties:**
- `int Priority` - Returns 1000 (low priority, runs last to capture final response state)

**Expected input/output:**
- Input: HTTP response message and flow context
- Output: Unmodified HTTP response (this interceptor only logs)

**Side effects:** Generates log entries, may convert response body bytes to text for logging

**Error behavior:** Does not throw exceptions. Handles encoding issues gracefully. All errors are contained within the interceptor.

## Internal Logic Breakdown
**Lines 15-18 (Constructor):**
- Validates and injects logger dependency
- No complex initialization - all work is done per response

**Line 20 (Priority Property):**
- Returns fixed priority value of 1000
- Ensures this interceptor runs last in the response processing pipeline
- Allows other interceptors to modify response before logging

**Lines 22-42 (InterceptAsync Method):**
- Logs basic response metadata: status code, reason phrase, content type, content length
- Uses GetValueOrDefault for safe header access with "unknown" fallback
- Implements conditional body logging with size and content type checks
- Only logs bodies smaller than 10KB to prevent log spam
- Only logs text-based content (text/, json, xml) to avoid binary data
- Uses UTF-8 encoding for body text conversion
- Returns Task.FromResult to complete async operation immediately
- Does not modify the response object

## Patterns & Principles Used
**Design Patterns:**
- **Observer Pattern:** Observes and logs response information without modification
- **Strategy Pattern:** Implements specific logging strategy for responses

**Architectural Patterns:**
- **Interceptor Pattern:** Processes responses in the pipeline without modification
- **Cross-cutting Concern:** Logging applied across all responses

**Why these patterns were chosen:**
- Observer pattern allows non-intrusive monitoring of response data
- Strategy pattern enables different logging approaches through different interceptors
- Interceptor pattern integrates seamlessly with the proxy's processing pipeline

**Trade-offs:**
- Fixed size limit (10KB) may not be suitable for all use cases
- UTF-8 encoding assumption may not work for all text content
- Late priority (1000) may miss some response modifications

**Anti-patterns avoided:**
- No modification of response data (read-only interceptor)
- No blocking operations in async methods
- Proper handling of missing headers and null values

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of logger only
- No other external dependencies

**Configuration Sources:**
- No external configuration
- Behavior controlled by hardcoded constants (10KB size limit, content type filters)
- Priority fixed in code

**Runtime Wiring:**
- No dynamic configuration changes during operation
- Logger injection enables flexible output destinations
- Behavior is consistent across all responses

**Registration Points:**
- Should be registered as IResponseInterceptor in DI container
- Singleton lifetime appropriate (stateless)
- Typically included by default in interceptor collections

## Example Usage
**Minimal Example:**
```csharp
var interceptor = new ResponseLoggerInterceptor(logger);
var loggedResponse = await interceptor.InterceptAsync(response, flow);
// Response is unchanged, but information is logged
```

**Realistic Example with Custom Logging:**
```csharp
public class ResponseLoggingSetup
{
    public static IResponseInterceptor CreateResponseLogger(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ResponseLoggerInterceptor>();
        return new ResponseLoggerInterceptor(logger);
    }
}

// Usage in DI container
services.AddSingleton<IResponseInterceptor, ResponseLoggerInterceptor>();
```

**Incorrect Usage Example:**
```csharp
// WRONG: Expecting response modification
var interceptor = new ResponseLoggerInterceptor(logger);
var modifiedResponse = await interceptor.InterceptAsync(response, flow);
// modifiedResponse will be identical to response

// WRONG: Expecting binary data to be logged
var binaryResponse = new HttpResponseMessage
{
    Body = new byte[] { 0x89, 0x50, 0x4E, 0x47 }, // PNG header
    Headers = new Dictionary<string, string>
    {
        ["Content-Type"] = "image/png"
    }
};
await interceptor.InterceptAsync(binaryResponse, flow);
// Binary body will not be logged due to content type filter

// WRONG: Expecting large bodies to be logged
var largeResponse = new HttpResponseMessage
{
    Body = new byte[1024 * 20], // 20KB
    Headers = new Dictionary<string, string>
    {
        ["Content-Type"] = "text/plain"
    }
};
await interceptor.InterceptAsync(largeResponse, flow);
// Large body will not be logged due to size limit
```

**How to test in isolation:**
```csharp
[Test]
public async Task InterceptAsync_ShouldLogResponseMetadata()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var interceptor = new ResponseLoggerInterceptor(logger.Object);
    
    var response = new HttpResponseMessage
    {
        StatusCode = 200,
        ReasonPhrase = "OK",
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Content-Length"] = "123"
        }
    };
    var flow = new Flow();
    
    // Act
    var result = await interceptor.InterceptAsync(response, flow);
    
    // Assert
    Assert.That(result, Is.EqualTo(response)); // Should be unchanged
    logger.Verify(x => x.LogInfo(
        "Response: {0} {1} - Content-Type: {2}, Content-Length: {3}",
        200, "OK", "application/json", "123"), Times.Once);
}

[Test]
public async Task InterceptAsync_ShouldLogSmallTextBody()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var interceptor = new ResponseLoggerInterceptor(logger.Object);
    
    var textBody = Encoding.UTF8.GetBytes("{\"message\": \"Hello World\"}");
    var response = new HttpResponseMessage
    {
        StatusCode = 200,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        },
        Body = textBody
    };
    var flow = new Flow();
    
    // Act
    var result = await interceptor.InterceptAsync(response, flow);
    
    // Assert
    logger.Verify(x => x.LogInfo("Response body: {0}", "{\"message\": \"Hello World\"}"), Times.Once);
}
```

**How to mock or replace it:**
```csharp
public class MockResponseLoggerInterceptor : IResponseInterceptor
{
    public List<HttpResponseMessage> LoggedResponses { get; } = new();
    public int Priority => 1000;
    
    public Task<HttpResponseMessage> InterceptAsync(HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)
    {
        LoggedResponses.Add(response);
        
        // Simulate logging (in real implementation, would use logger)
        Console.WriteLine($"Logged response: {response.StatusCode} {response.ReasonPhrase}");
        
        return Task.FromResult(response);
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Configurable size limits:** Add constructor parameter for maximum body size
- **Custom content type filters:** Add configurable content type patterns
- **Different encoding support:** Add encoding detection or configuration
- **Structured logging:** Add structured data logging instead of text
- **Response filtering:** Add conditional logging based on response properties

**Where NOT to add logic:**
- Don't add response modification (this should remain read-only)
- Don't add request processing (belongs in request interceptors)
- Don't add network connection handling (belongs in connection managers)

**Safe extension points:**
- Additional logging logic in InterceptAsync method
- Configuration parameters in constructor
- Custom content type and size filtering logic

**Common mistakes:**
- Adding response modification that breaks the read-only contract
- Logging large binary bodies that can fill log files
- Using blocking operations for body processing
- Not handling encoding exceptions properly

**Refactoring warnings:**
- Changing priority will affect when logging occurs in the pipeline
- Modifying size limits may impact log file sizes
- Removing content type filtering may log sensitive binary data

## Failure Modes & Debugging
**Common runtime errors:**
- `ArgumentNullException`: Null logger passed to constructor
- `EncoderFallbackException`: Invalid byte sequences in UTF-8 conversion
- `ArgumentException`: Invalid header values or content types

**Null/reference risks:**
- Logger validated in constructor
- Response and flow objects assumed valid from upstream
- Headers accessed safely with GetValueOrDefault

**Performance risks:**
- Large response body text conversion may consume memory and CPU
- String operations for logging may impact performance under high load
- Logging infrastructure may become bottleneck with verbose logging

**Logging points:**
- Basic response metadata (status, headers, content info)
- Response body content for text-based responses under size limit
- No logging for binary content or large bodies to prevent log spam

**How to debug step-by-step:**
1. Enable debug logging to see all response information
2. Set breakpoint in InterceptAsync to trace response processing
3. Monitor content type filtering logic
4. Check size limit enforcement for body logging
5. Verify encoding conversion for different character sets
6. Test with various response types and sizes

## Cross-References
**Related classes:**
- `IResponseInterceptor` - Interface defining response interceptor contract
- `InterceptorManager` - Coordinates interceptor execution
- `HttpResponseMessage` - Model representing HTTP responses
- `Flow` - Model representing HTTP transaction context

**Upstream callers:**
- `InterceptorManager` - Calls this interceptor as part of response processing
- Test fixtures - Direct calls for interceptor testing

**Downstream dependencies:**
- Logging infrastructure for output
- HTTP response model for reading response data

**Documents that should be read before/after:**
- Before: IResponseInterceptor interface documentation
- Before: InterceptorManager documentation (execution context)
- After: Other interceptor implementations for comparison
- Related: HTTP response specification documentation

## Knowledge Transfer Notes
**Reusable concepts:**
- HTTP response logging and monitoring
- Content type filtering for selective logging
- Size-based filtering to prevent log spam
- Read-only interceptor pattern for monitoring
- Priority-based execution for final-stage processing

**Project-specific elements:**
- Snitcher's interceptor interface and priority system
- Integration with Snitcher's logging infrastructure
- Specific content type and size filtering logic
- UTF-8 encoding assumption for text content

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for response interception with priority ordering
2. Create interceptor class with logging dependency
3. Implement response metadata logging with safe header access
4. Add conditional body logging with size and content type filters
5. Use appropriate encoding for text conversion
6. Ensure read-only behavior (no response modification)
7. Include comprehensive error handling and validation
8. Design for dependency injection and testability

**Key architectural insights:**
- Response logging is essential for debugging HTTP proxy issues
- Content type and size filtering prevents log spam from binary data
- Read-only interceptors provide monitoring without side effects
- Late priority execution ensures logging captures final response state
- UTF-8 encoding is a reasonable default for text content but has limitations
