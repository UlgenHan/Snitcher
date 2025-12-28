# HeaderInjectorInterceptor.cs

## Overview

`HeaderInjectorInterceptor.cs` is a concrete implementation of the IRequestInterceptor interface that automatically injects HTTP headers into outgoing requests. This interceptor enables cross-cutting concerns like authentication, user agent specification, tracking, and custom header injection without modifying request handling logic throughout the application.

**Why it exists**: To provide a centralized, configurable mechanism for adding HTTP headers to all outgoing requests, supporting scenarios like API authentication, request tracking, user agent specification, and custom protocol requirements.

**What problem it solves**: Eliminates scattered header injection code, provides consistent header management across all requests, enables dynamic header configuration, and separates cross-cutting header concerns from business logic.

**What would break if removed**: Applications would lose automatic header injection capability, requiring manual header management in each request handler, potentially missing required headers for authentication or tracking.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure interceptor logic)

**Persistence**: Not applicable (in-memory configuration)

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Async/await support, HTTP message model

## Architectural Role

**Layer**: Library Layer (HTTP Interceptor)

**Responsibility Boundaries**:
- MUST inject headers into HTTP requests
- MUST avoid overwriting existing headers
- MUST operate within interceptor pipeline
- MUST NOT handle HTTP transport
- MUST NOT implement business logic

**What it MUST do**:
- Add configured headers to requests
- Respect existing header values
- Log header injection operations
- Execute efficiently with minimal overhead
- Support cancellation tokens

**What it MUST NOT do**:
- Remove or modify existing headers
- Handle HTTP response processing
- Access external services during interception
- Implement authentication logic

**Dependencies (Incoming)**: InterceptorManager (pipeline orchestration)

**Dependencies (Outgoing**: ILogger (diagnostic logging)

## Execution Flow

**Where execution starts**: Called by InterceptorManager during request processing pipeline

**How control reaches this component**:
1. HTTP request enters proxy pipeline
2. InterceptorManager sorts interceptors by priority
3. HeaderInjectorInterceptor.InterceptAsync called
4. Headers injected into request
5. Modified request returned to pipeline

**Header Injection Flow** (lines 24-37):
1. Iterate through configured headers
2. Check if header already exists in request
3. Add header if not present
4. Log successful injection
5. Return modified request

**Priority Considerations**:
- Priority set to 200 (medium priority)
- Executes after high-priority interceptors
- Allows other interceptors to set headers first
- Ensures this interceptor doesn't override critical headers

**Synchronous vs asynchronous behavior**: Method is async but completes synchronously using Task.FromResult for consistency with interceptor interface

**Threading/Dispatcher notes**: No threading concerns - operates on single request message

**Lifecycle**: Singleton service - lives for application duration, maintains header configuration

## Public API / Surface Area

**Interface Implementation**: `class HeaderInjectorInterceptor : IRequestInterceptor`

**Constructors**:
```csharp
public HeaderInjectorInterceptor(Dictionary<string, string> headers, ILogger logger)
```

**Properties**:
- `int Priority` - Returns 200 (medium execution priority)

**Methods**:
- `Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)` - Inject headers into request

**Expected Input/Output**: Takes HTTP request and flow context, returns modified request with injected headers.

**Side Effects**:
- Adds headers to HTTP request message
- Logs header injection operations
- Does not modify existing headers

**Error Behavior**: No explicit error handling - relies on HTTP message model validation and logger error handling.

## Internal Logic Breakdown

**Constructor Pattern** (lines 16-20):
```csharp
public HeaderInjectorInterceptor(Dictionary<string, string> headers, ILogger logger)
{
    _headers = headers ?? new Dictionary<string, string>();
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```
- Accepts dictionary of headers to inject
- Null-safe header collection initialization
- Logger required for diagnostic output
- Headers stored for injection in all requests

**Priority Setting** (line 22):
```csharp
public int Priority => 200;
```
- Medium priority value
- Executes after critical interceptors
- Allows other interceptors to set headers first
- Prevents overwriting of important headers

**Header Injection Logic** (lines 24-37):
```csharp
public async Task<Core.Models.HttpRequestMessage> InterceptAsync(Core.Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
{
    foreach (var header in _headers)
    {
        if (!request.Headers.ContainsKey(header.Key))
        {
            request.Headers[header.Key] = header.Value;
            _logger.LogInfo("Added header '{0}: {1}' to request {2}",
                header.Key, header.Value, request.Url);
        }
    }

    return await Task.FromResult(request);
}
```
- Iterates through configured header dictionary
- Checks for existing header to avoid overwriting
- Adds header only if not already present
- Logs successful injection for debugging
- Returns modified request via Task.FromResult

**Non-Overwrite Strategy**:
- Only adds headers that don't exist
- Preserves headers set by other interceptors
- Respects higher-priority interceptor decisions
- Prevents accidental header replacement

**Logging Strategy**:
- Logs each successful header injection
- Includes header name, value, and request URL
- Useful for debugging and audit trails
- Info level for operational visibility

## Patterns & Principles Used

**Interceptor Pattern**: Implements cross-cutting concern without modifying core logic

**Strategy Pattern**: Encapsulates header injection algorithm

**Dependency Injection**: Headers and logger injected via constructor

**Non-Overwrite Principle**: Respects existing header values

**Single Responsibility**: Focused solely on header injection

**Why these patterns were chosen**:
- Interceptor for clean separation of concerns
- Strategy for encapsulated injection logic
- DI for testability and configuration
- Non-overwrite for cooperative behavior
- Single responsibility for maintainability

**Trade-offs**:
- Fixed priority may not suit all use cases
- Dictionary-based headers limited to string values
- No conditional header injection logic
- Synchronous completion may seem inconsistent

**Anti-patterns avoided**:
- No hardcoded header values
- No modification of existing headers
- No business logic in interceptor
- No external service dependencies

## Binding / Wiring / Configuration

**Dependency Injection**:
- Header dictionary injected from configuration
- Logger injected for diagnostic output
- Registered as IRequestInterceptor in DI container

**Configuration Sources**:
- Header values from application configuration
- Could be loaded from appsettings.json, environment variables, etc.

**Runtime Wiring**:
- Discovered by InterceptorManager via DI
- Automatically included in request pipeline
- Priority determines execution order

**Registration Points**:
- DI container registration
- Header configuration during application startup

## Example Usage

**Minimal Example**:
```csharp
// Configure headers
var headers = new Dictionary<string, string>
{
    ["User-Agent"] = "Snitcher/1.0",
    ["X-API-Key"] = "your-api-key"
};

// Create interceptor
var interceptor = new HeaderInjectorInterceptor(headers, logger);

// Apply to request
var modifiedRequest = await interceptor.InterceptAsync(request, flow);
```

**Realistic Example**:
```csharp
// In DI container setup
services.AddSingleton<IRequestInterceptor>(provider =>
{
    var headers = new Dictionary<string, string>
    {
        ["User-Agent"] = configuration["UserAgent"],
        ["X-Request-ID"] => Guid.NewGuid().ToString(),
        ["Authorization"] = $"Bearer {configuration["ApiToken"]}"
    };
    
    return new HeaderInjectorInterceptor(headers, provider.GetRequiredService<ILogger<HeaderInjectorInterceptor>>());
});
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't create with null headers
var interceptor = new HeaderInjectorInterceptor(null, logger); // Creates empty headers

// BAD - Don't expect headers to be overwritten
request.Headers["Existing"] = "Original";
var headers = new Dictionary<string, string> { ["Existing"] = "New" };
// After interceptor, Existing header still equals "Original"
```

**How to test in isolation**:
```csharp
[Test]
public void HeaderInjector_ShouldAddMissingHeaders()
{
    var headers = new Dictionary<string, string> { ["X-Test"] = "Value" };
    var mockLogger = new Mock<ILogger>();
    var interceptor = new HeaderInjectorInterceptor(headers, mockLogger.Object);
    
    var request = new HttpRequestMessage { Url = "http://example.com" };
    var flow = new Flow();
    
    var result = interceptor.InterceptAsync(request, flow).Result;
    
    Assert.AreEqual("Value", result.Headers["X-Test"]);
    mockLogger.Verify(x => x.LogInfo("Added header '{0}: {1}' to request {2}", "X-Test", "Value", "http://example.com"));
}

[Test]
public void HeaderInjector_ShouldNotOverwriteExistingHeaders()
{
    var headers = new Dictionary<string, string> { ["X-Test"] = "New" };
    var interceptor = new HeaderInjectorInterceptor(headers, logger);
    
    var request = new HttpRequestMessage { Url = "http://example.com" };
    request.Headers["X-Test"] = "Original";
    
    var result = interceptor.InterceptAsync(request, flow).Result;
    
    Assert.AreEqual("Original", result.Headers["X-Test"]);
}
```

**How to mock or replace**:
- Create test instances with mock headers
- Mock logger for testing logging behavior
- Replace with different interceptor implementations
- Use dictionary with test header values

## Extension & Modification Guide

**How to add conditional header injection**:
1. Add configuration properties for conditional logic
2. Modify InterceptAsync to check conditions
3. Consider flow context for request-specific decisions
4. Add logging for conditional decisions

**Where NOT to add logic**:
- Don't add authentication token generation
- Don't add external service calls
- Don't add complex business rules

**Safe extension points**:
- Header value templating (placeholders, variables)
- Conditional injection based on request properties
- Header validation and normalization
- Dynamic header updates from configuration

**Common mistakes**:
- Setting priority too high (overwrites critical headers)
- Adding headers that cause request failures
- Not handling case-insensitive header names
- Forgetting to log injection operations

**Refactoring warnings**:
- Changing priority affects execution order
- Adding complex logic may impact performance
- Consider thread safety for dynamic header updates
- Monitor header size limits for HTTP requests

## Failure Modes & Debugging

**Common runtime errors**:
- ArgumentException for invalid header names/values
- KeyNotFoundException if header dictionary modified during iteration
- NullReferenceException if request.Headers is null

**Null/reference risks**:
- Headers dictionary null-safe in constructor
- Request headers validated by HTTP message model
- Logger required but validated in constructor

**Performance risks**:
- Large header dictionaries increase processing time
- String operations for header value formatting
- Logging overhead for high-volume requests

**Logging points**:
- Each successful header injection
- Could add failure logging for invalid headers
- Performance timing for header processing

**How to debug step-by-step**:
1. Enable debug logging to see header injection
2. Verify header dictionary contents at startup
3. Check request headers before and after interception
4. Test with various header combinations
5. Monitor execution order with other interceptors

## Cross-References

**Related classes**:
- `IRequestInterceptor` (interface implemented)
- `InterceptorManager` (orchestrates execution)
- `HttpRequestMessage` (message modified)
- `Flow` (context provided)

**Upstream callers**:
- InterceptorManager (pipeline execution)
- HTTP request processing pipeline

**Downstream dependencies**:
- HTTP message model
- Logging infrastructure

**Documents to read before/after**:
- Before: IRequestInterceptor interface definition
- After: InterceptorManager orchestration logic
- After: Other interceptor implementations

## Knowledge Transfer Notes

**Reusable concepts**:
- Interceptor pattern for cross-cutting concerns
- Header injection strategy for HTTP clients
- Priority-based execution ordering
- Non-destructive modification approach
- Configuration-driven behavior

**Project-specific elements**:
- Snitcher HTTP message model
- Flow context for request information
- Priority system for interceptor ordering
- Logging integration for debugging

**How to recreate pattern elsewhere**:
1. Implement interceptor interface with Priority property
2. Accept configuration via constructor injection
3. Implement non-destructive modification logic
4. Add logging for debugging and audit
5. Use Task.FromResult for synchronous operations
6. Respect existing data to avoid conflicts
7. Consider execution order in priority setting

**Key insights**:
- Always respect existing data to avoid conflicts
- Use priority to control execution order
- Log operations for debugging and audit trails
- Keep interceptors focused and single-purpose
- Use dependency injection for testability
- Consider performance for high-volume scenarios
- Handle edge cases like null values gracefully
- Provide clear configuration interfaces
