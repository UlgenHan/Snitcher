# HeaderInjectorInterceptor

## Overview
The HeaderInjectorInterceptor class is a request interceptor that automatically adds specified HTTP headers to outgoing requests. It's designed to inject custom headers such as authentication tokens, user agents, or proxy identification headers into all HTTP traffic passing through the proxy.

**Why it exists:** To provide a simple, configurable way to add custom headers to HTTP requests without modifying the core proxy logic.

**Problem it solves:** Enables scenarios like API authentication, traffic identification, user agent spoofing, and custom header injection for testing or integration purposes.

**What would break if removed:** Users would lose the ability to automatically inject custom headers. Any systems relying on automatic header injection for authentication or identification would fail.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Collections.Generic (Dictionary for header storage)
  - Microsoft.Extensions.Logging.Abstractions (logging abstraction)
- **Persistence/communication:** In-memory header storage only
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** .NET 8.0 runtime
- **Version hints:** Uses modern async patterns, cancellation token support

## Architectural Role
- **Layer:** Application/Interceptor layer
- **Responsibility boundaries:**
  - MUST: Add specified headers to requests, avoid overwriting existing headers
  - MUST NOT: Remove headers, modify request body, handle responses
- **Dependencies:**
  - **Incoming:** Dictionary<string,string> of headers to inject, ILogger
  - **Outgoing:** Modified HTTP requests, log entries

## Execution Flow
1. InterceptAsync receives HTTP request and flow context
2. Iterates through configured headers dictionary
3. For each header, checks if it already exists in request
4. If header doesn't exist, adds it with configured value
5. Logs each header addition for debugging
6. Returns modified request (or original if no changes needed)

**Synchronous vs asynchronous:** Method is async but returns Task.FromResult for immediate completion.

**Threading notes:** Stateless design - safe for concurrent use. Header dictionary is read-only after construction.

**Lifecycle:** Created per dependency injection → InterceptAsync called for each request → No disposal needed

## Public API / Surface Area
**Constructors:**
- `HeaderInjectorInterceptor(Dictionary<string,string> headers, ILogger logger)` - Creates interceptor with header configuration

**Public Methods:**
- `Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)` - Applies header injection to request

**Properties:**
- `int Priority` - Returns 200 (moderate priority, runs after basic processing but before final modifications)

**Expected input/output:**
- Input: HTTP request message and flow context
- Output: HTTP request with additional headers (if they didn't already exist)

**Side effects:** Modifies request headers dictionary, generates log entries for header additions

**Error behavior:** Does not throw exceptions. Handles null header dictionary gracefully by treating it as empty.

## Internal Logic Breakdown
**Lines 16-20 (Constructor):**
- Validates and injects logger dependency
- Initializes headers dictionary with provided headers or empty dictionary if null
- No complex initialization - headers are stored as-is

**Line 22 (Priority Property):**
- Returns fixed priority value of 200
- Positions this interceptor in the middle of the execution pipeline
- Allows basic processing to run first, but runs before final modifications

**Lines 24-37 (InterceptAsync Method):**
- Iterates through configured headers dictionary
- For each header-key pair, checks if request already contains that header
- Only adds header if it doesn't already exist (prevents overwriting)
- Logs each successful header addition with header name, value, and request URL
- Returns Task.FromResult to complete async operation immediately
- No modification to request body or other properties

## Patterns & Principles Used
**Design Patterns:**
- **Decorator Pattern:** Decorates HTTP requests with additional headers
- **Strategy Pattern:** Implements specific header injection strategy

**Architectural Patterns:**
- **Interceptor Pattern:** Modifies requests in the processing pipeline
- **Configuration Pattern:** Behavior controlled by constructor parameters

**Why these patterns were chosen:**
- Decorator pattern allows clean addition of headers without modifying core request logic
- Strategy pattern enables different header injection approaches through different interceptors
- Interceptor pattern integrates seamlessly with the proxy's processing pipeline

**Trade-offs:**
- Fixed priority may not be suitable for all use cases
- No support for conditional header injection based on request properties
- Simple dictionary-based configuration limits complex injection scenarios

**Anti-patterns avoided:**
- No static global state (instance-based design)
- No blocking operations in async methods
- Proper null handling for configuration

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of logger and header configuration
- Headers provided as Dictionary<string,string> parameter

**Configuration Sources:**
- Header dictionary provided at construction time
- No external configuration files or runtime configuration
- Priority fixed in code

**Runtime Wiring:**
- Header configuration is immutable after construction
- No dynamic header addition/removal during operation
- Logger injection enables debugging and monitoring

**Registration Points:**
- Should be registered as IRequestInterceptor in DI container
- Singleton or scoped lifetime appropriate (stateless)
- Header configuration typically provided during DI registration

## Example Usage
**Minimal Example:**
```csharp
var headers = new Dictionary<string, string>
{
    ["X-Proxy-Name"] = "Snitcher",
    ["X-Request-ID"] = Guid.NewGuid().ToString()
};

var interceptor = new HeaderInjectorInterceptor(headers, logger);
var modifiedRequest = await interceptor.InterceptAsync(originalRequest, flow);
```

**Realistic Example with Authentication:**
```csharp
public class ApiAuthInterceptorSetup
{
    public static IRequestInterceptor CreateAuthInterceptor(string apiToken, ILogger logger)
    {
        var authHeaders = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {apiToken}",
            ["X-Client-Version"] = "1.0.0",
            ["X-Client-Name"] = "SnitcherProxy"
        };
        
        return new HeaderInjectorInterceptor(authHeaders, logger);
    }
}

// Usage in DI container
services.AddSingleton<IRequestInterceptor>(provider =>
    ApiAuthInterceptorSetup.CreateAuthInterceptor(
        configuration["ApiToken"], 
        provider.GetRequiredService<ILogger>()));
```

**Incorrect Usage Example:**
```csharp
// WRONG: Expecting headers to be overwritten
var headers = new Dictionary<string, string>
{
    ["User-Agent"] = "CustomAgent"
};
var interceptor = new HeaderInjectorInterceptor(headers, logger);

// If request already has User-Agent, it won't be replaced
// This interceptor only adds headers that don't exist

// WRONG: Passing null headers
var interceptor = new HeaderInjectorInterceptor(null, logger);
// This works but adds no headers

// WRONG: Expecting async operations
await interceptor.InterceptAsync(request, flow);
// This completes immediately, no actual async work
```

**How to test in isolation:**
```csharp
[Test]
public async Task InterceptAsync_ShouldAddMissingHeaders()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var headers = new Dictionary<string, string>
    {
        ["X-Test"] = "Value",
        ["X-Another"] = "AnotherValue"
    };
    
    var interceptor = new HeaderInjectorInterceptor(headers, logger.Object);
    var request = new HttpRequestMessage { Url = new Uri("http://example.com") };
    var flow = new Flow();
    
    // Act
    var result = await interceptor.InterceptAsync(request, flow);
    
    // Assert
    Assert.That(result.Headers["X-Test"], Is.EqualTo("Value"));
    Assert.That(result.Headers["X-Another"], Is.EqualTo("AnotherValue"));
}

[Test]
public async Task InterceptAsync_ShouldNotOverwriteExistingHeaders()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var headers = new Dictionary<string, string>
    {
        ["X-Existing"] = "NewValue"
    };
    
    var interceptor = new HeaderInjectorInterceptor(headers, logger.Object);
    var request = new HttpRequestMessage 
    { 
        Url = new Uri("http://example.com"),
        Headers = new Dictionary<string, string>
        {
            ["X-Existing"] = "OriginalValue"
        }
    };
    var flow = new Flow();
    
    // Act
    var result = await interceptor.InterceptAsync(request, flow);
    
    // Assert
    Assert.That(result.Headers["X-Existing"], Is.EqualTo("OriginalValue"));
}
```

**How to mock or replace it:**
```csharp
public class MockHeaderInjectorInterceptor : IRequestInterceptor
{
    public Dictionary<string, string> InjectedHeaders { get; } = new();
    public int Priority => 200;
    
    public Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        foreach (var header in InjectedHeaders)
        {
            if (!request.Headers.ContainsKey(header.Key))
            {
                request.Headers[header.Key] = header.Value;
            }
        }
        
        return Task.FromResult(request);
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Conditional injection:** Add logic to inject headers based on request properties
- **Header overwriting:** Add option to overwrite existing headers
- **Dynamic headers:** Add support for header value generation at runtime
- **Header removal:** Add capability to remove specific headers

**Where NOT to add logic:**
- Don't add request body modification (belongs in other interceptors)
- Don't add network connection handling (belongs in connection managers)
- Don't add response processing (belongs in response interceptors)

**Safe extension points:**
- Additional header processing logic in InterceptAsync method
- Configuration validation in constructor
- Custom logging and monitoring

**Common mistakes:**
- Adding blocking operations to InterceptAsync method
- Modifying headers dictionary after interceptor creation
- Assuming header injection order is guaranteed beyond priority

**Refactoring warnings:**
- Changing priority will affect interceptor execution order
- Modifying header injection logic may break existing configurations
- Removing null-safety may cause runtime errors

## Failure Modes & Debugging
**Common runtime errors:**
- `ArgumentNullException`: Null logger passed to constructor
- `KeyNotFoundException`: Accessing headers that don't exist (unlikely with current implementation)
- `ArgumentException`: Invalid header names or values

**Null/reference risks:**
- Logger validated in constructor
- Headers dictionary handled safely (null treated as empty)
- Request and flow objects assumed valid from upstream

**Performance risks:**
- Large number of headers may impact processing time slightly
- Dictionary lookups are O(1) and very efficient
- Logging adds minimal overhead per header added

**Logging points:**
- Header addition with header name, value, and request URL
- No logging for headers that already exist (to reduce noise)
- Error conditions handled by upstream components

**How to debug step-by-step:**
1. Enable debug logging to see header additions
2. Set breakpoint in InterceptAsync to trace header processing
3. Monitor header dictionary contents during construction
4. Check request headers before and after interceptor application
5. Verify priority ordering in interceptor chain
6. Test with various header combinations and existing headers

## Cross-References
**Related classes:**
- `IRequestInterceptor` - Interface defining request interceptor contract
- `InterceptorManager` - Coordinates interceptor execution
- `HttpRequestMessage` - Model representing HTTP requests
- `Flow` - Model representing HTTP transaction context

**Upstream callers:**
- `InterceptorManager` - Calls this interceptor as part of request processing
- Test fixtures - Direct calls for interceptor testing

**Downstream dependencies:**
- HTTP request model for header modification
- Logging infrastructure for debugging and monitoring

**Documents that should be read before/after:**
- Before: IRequestInterceptor interface documentation
- Before: InterceptorManager documentation (execution context)
- After: Other interceptor implementations for comparison
- Related: HTTP header specification documentation

## Knowledge Transfer Notes
**Reusable concepts:**
- HTTP header manipulation and injection
- Interceptor pattern implementation
- Configuration-driven behavior modification
- Non-overwriting header injection strategy
- Priority-based execution in processing pipelines

**Project-specific elements:**
- Snitcher's interceptor interface and priority system
- Integration with Snitcher's flow tracking
- Specific logging patterns for debugging
- Header injection strategy (add-only, no overwrite)

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for request interception with priority ordering
2. Create interceptor class with configuration parameters
3. Implement header injection logic with existence checking
4. Add comprehensive logging for debugging and monitoring
5. Ensure async/await patterns even for synchronous operations
6. Include proper null handling and validation
7. Design for dependency injection and testability
8. Add priority property for execution ordering

**Key architectural insights:**
- Simple header injection is a common requirement for HTTP proxies
- Non-overwriting strategy prevents accidental header replacement
- Priority-based execution enables predictable interceptor ordering
- Comprehensive logging is essential for debugging interceptor behavior
- Configuration-driven design enables flexible usage without code changes
