# InterceptorManager.cs

## Overview

`InterceptorManager.cs` is the central orchestrator for HTTP request and response interceptors in the Snitcher proxy system. This class manages the execution pipeline of interceptors, applying them in priority order to HTTP flows while providing error isolation and logging. It implements the Chain of Responsibility pattern to enable flexible, composable HTTP message processing.

**Why it exists**: To provide a centralized, extensible mechanism for applying multiple cross-cutting concerns (logging, header injection, modification, etc.) to HTTP requests and responses in a consistent, ordered, and fault-tolerant manner.

**What problem it solves**: Eliminates scattered interceptor logic, provides consistent execution order based on priority, ensures that a single interceptor failure doesn't break the entire pipeline, and enables easy addition of new cross-cutting behaviors.

**What would break if removed**: HTTP interception would become uncoordinated, new cross-cutting features would require code changes throughout the proxy, error handling would be inconsistent, and the extensible interceptor architecture would collapse.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure orchestration logic)

**Persistence**: Not applicable (in-memory processing)

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Async/await support, dependency injection container

## Architectural Role

**Layer**: Library Layer (Interceptor Orchestration)

**Responsibility Boundaries**:
- MUST coordinate interceptor execution
- MUST maintain execution order based on priority
- MUST provide error isolation between interceptors
- MUST NOT implement specific interception logic
- MUST NOT handle HTTP transport concerns

**What it MUST do**:
- Apply request interceptors in priority order
- Apply response interceptors in priority order
- Log interceptor execution and errors
- Continue pipeline despite individual interceptor failures
- Provide cancellation support

**What it MUST NOT do**:
- Implement specific interception behaviors
- Handle HTTP protocol details
- Store persistent state
- Modify interceptor implementations

**Dependencies (Incoming)**: Proxy server components, HTTP flow handlers

**Dependencies (Outgoing**: IRequestInterceptor, IResponseInterceptor, ILogger

## Execution Flow

**Where execution starts**: Called by proxy server components when HTTP requests and responses need processing

**How control reaches this component**:
1. HTTP request received by proxy server
2. Proxy server calls ApplyRequestInterceptorsAsync
3. Manager applies all request interceptors
4. Request sent to target server
5. Response received from target server
6. Proxy server calls ApplyResponseInterceptorsAsync
7. Manager applies all response interceptors
8. Processed response returned to client

**Request Interceptor Flow** (lines 27-49):
1. Start with original request
2. Sort interceptors by Priority (ascending)
3. For each interceptor:
   - Log interceptor application
   - Call interceptor.InterceptAsync
   - Handle exceptions without failing pipeline
   - Use modified request for next interceptor
4. Return final processed request

**Response Interceptor Flow** (lines 51-73):
1. Start with original response
2. Sort interceptors by Priority (ascending)
3. For each interceptor:
   - Log interceptor application
   - Call interceptor.InterceptAsync
   - Handle exceptions without failing pipeline
   - Use modified response for next interceptor
4. Return final processed response

**Synchronous vs asynchronous behavior**: All operations are async to support non-blocking HTTP processing

**Threading/Dispatcher notes**: No specific threading requirements - relies on async/await pattern

**Lifecycle**: Singleton service - lives for application duration

## Public API / Surface Area

**Constructors**:
```csharp
public InterceptorManager(
    IEnumerable<IRequestInterceptor> requestInterceptors,
    IEnumerable<IResponseInterceptor> responseInterceptors,
    ILogger logger)
```

**Public Methods**:
- `Task<HttpRequestMessage> ApplyRequestInterceptorsAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)` - Apply request interceptors
- `Task<HttpResponseMessage> ApplyResponseInterceptorsAsync(HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)` - Apply response interceptors

**Expected Input/Output**: Methods take HTTP messages and flow context, return processed HTTP messages after all interceptors applied.

**Side Effects**:
- Modifies HTTP messages through interceptor pipeline
- Logs interceptor execution and errors
- May change request/response content, headers, etc.

**Error Behavior**: Individual interceptor exceptions are caught and logged, pipeline continues with remaining interceptors.

## Internal Logic Breakdown

**Constructor Pattern** (lines 17-25):
```csharp
public InterceptorManager(
    IEnumerable<IRequestInterceptor> requestInterceptors,
    IEnumerable<IResponseInterceptor> responseInterceptors,
    ILogger logger)
{
    _requestInterceptors = requestInterceptors ?? throw new ArgumentNullException(nameof(requestInterceptors));
    _responseInterceptors = responseInterceptors ?? throw new ArgumentNullException(nameof(responseInterceptors));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```
- Dependency injection of all registered interceptors
- Null validation for all dependencies
- Stores interceptor collections for later execution

**Request Interceptor Execution** (lines 27-49):
```csharp
public async Task<Core.Models.HttpRequestMessage> ApplyRequestInterceptorsAsync(
    Core.Models.HttpRequestMessage request,
    Flow flow,
    CancellationToken cancellationToken = default)
{
    var currentRequest = request;

    foreach (var interceptor in _requestInterceptors.OrderBy(x => x.Priority))
    {
        try
        {
            _logger.LogInfo("Applying request interceptor: {0}", interceptor.GetType().Name);
            currentRequest = await interceptor.InterceptAsync(currentRequest, flow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request interceptor {0} failed", interceptor.GetType().Name);
            // Continue with other interceptors
        }
    }

    return currentRequest;
}
```
- Chains interceptors in priority order
- Each interceptor receives output of previous interceptor
- Exceptions caught and logged but don't stop pipeline
- Final processed request returned to caller

**Response Interceptor Execution** (lines 51-73):
- Identical pattern to request interceptors
- Applies to HTTP responses instead of requests
- Same error isolation and logging strategy

**Error Isolation Strategy**:
- Try-catch around each interceptor execution
- Errors logged but don't propagate to caller
- Pipeline continues with remaining interceptors
- Ensures robustness against individual interceptor failures

**Priority-Based Ordering**:
- Interceptors sorted by Priority property (ascending)
- Lower numbers = higher priority = execute first
- Enables predictable execution order
- Allows interceptors to depend on each other

## Patterns & Principles Used

**Chain of Responsibility Pattern**: Each interceptor processes the message and passes it to the next

**Pipeline Pattern**: Sequential processing through multiple stages

**Error Isolation Pattern**: Individual component failures don't break entire pipeline

**Dependency Injection Pattern**: All interceptors injected and managed by container

**Strategy Pattern**: Different interceptors implement different processing strategies

**Open/Closed Principle**: Easy to add new interceptors without changing existing code

**Why these patterns were chosen**:
- Chain of Responsibility for flexible message processing
- Pipeline for ordered, composable operations
- Error isolation for robustness
- Dependency injection for testability and flexibility
- Strategy for varied interceptor behaviors
- Open/Closed for extensibility

**Trade-offs**:
- Performance overhead from multiple async calls
- Complexity in debugging multi-step processing
- Memory allocation from intermediate message copies
- Potential for interceptor interactions

**Anti-patterns avoided**:
- No hardcoded interceptor dependencies
- No synchronous blocking operations
- No failure cascade between interceptors
- No direct HTTP protocol handling

## Binding / Wiring / Configuration

**Dependency Injection**:
- All IRequestInterceptor implementations injected
- All IResponseInterceptor implementations injected
- ILogger injected for diagnostic logging

**Configuration Sources**: Interceptor registration in DI container

**Runtime Wiring**:
- Interceptors discovered and registered at startup
- Manager created as singleton with all interceptors
- Priority values defined in interceptor implementations

**Registration Points**:
- Interceptor implementations registered in DI container
- Manager registered with interceptor dependencies
- Priority values set in interceptor constructors

## Example Usage

**Minimal Example**:
```csharp
// Manager created by DI with all interceptors
var manager = serviceProvider.GetRequiredService<InterceptorManager>();

// Apply to HTTP request
var processedRequest = await manager.ApplyRequestInterceptorsAsync(request, flow);

// Apply to HTTP response
var processedResponse = await manager.ApplyResponseInterceptorsAsync(response, flow);
```

**Realistic Example**:
```csharp
// In proxy server request handling
public async Task HandleRequestAsync(HttpRequestMessage request, Flow flow)
{
    // Apply request interceptors (add headers, logging, etc.)
    var processedRequest = await _interceptorManager.ApplyRequestInterceptorsAsync(request, flow);
    
    // Send processed request to target
    var response = await SendToTargetAsync(processedRequest);
    
    // Apply response interceptors (logging, modification, etc.)
    var processedResponse = await _interceptorManager.ApplyResponseInterceptorsAsync(response, flow);
    
    return processedResponse;
}
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't create interceptors manually
var manager = new InterceptorManager(
    new List<IRequestInterceptor> { new MyInterceptor() }, // Wrong way
    new List<IResponseInterceptor>(),
    logger);

// BAD - Don't depend on specific interceptor order
// Interceptors should be independent and order-agnostic where possible
```

**How to test in isolation**:
```csharp
// Mock interceptors for testing
var mockRequestInterceptor = new Mock<IRequestInterceptor>();
mockRequestInterceptor.Setup(x => x.Priority).Returns(100);
mockRequestInterceptor.Setup(x => x.InterceptAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Flow>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((HttpRequestMessage req, Flow flow, CancellationToken ct) => req);

var manager = new InterceptorManager(
    new[] { mockRequestInterceptor.Object },
    Enumerable.Empty<IResponseInterceptor>(),
    mockLogger.Object);

var result = await manager.ApplyRequestInterceptorsAsync(request, flow);
mockRequestInterceptor.Verify(x => x.InterceptAsync(request, flow, It.IsAny<CancellationToken>()), Times.Once);
```

**How to mock or replace**:
- Mock individual interceptor interfaces
- Create test manager with mock interceptors
- Use in-memory collections for interceptor registration
- Mock logger for testing logging behavior

## Extension & Modification Guide

**How to add new interceptor type**:
1. Implement IRequestInterceptor or IResponseInterceptor interface
2. Set appropriate Priority value
3. Register in DI container
4. Interceptor automatically included in pipeline

**Where NOT to add logic**:
- Don't add HTTP protocol handling
- Don't add business logic specific to applications
- Don't add persistent state management

**Safe extension points**:
- New interceptor implementations
- Custom priority ordering strategies
- Additional logging and monitoring
- Performance metrics collection

**Common mistakes**:
- Setting wrong priority values causing unexpected order
- Throwing exceptions that break the pipeline
- Creating interceptors with side effects
- Not handling cancellation tokens properly

**Refactoring warnings**:
- Changing priority values affects execution order
- Adding many interceptors can impact performance
- Consider async overhead for simple operations
- Monitor memory usage with large message pipelines

## Failure Modes & Debugging

**Common runtime errors**:
- ArgumentNullException if dependencies not injected
- InvalidOperationException if interceptor collection modified during execution
- TaskCanceledException if cancellation requested

**Null/reference risks**:
- Interceptor collections validated in constructor
- HTTP messages validated by interceptors
- Logger dependency required for operation

**Performance risks**:
- Many interceptors can slow request processing
- Large HTTP messages copied between interceptors
- Async overhead for simple operations
- Memory allocation from intermediate objects

**Logging points**:
- Interceptor application start/completion
- Interceptor failures with full exception details
- Performance timing could be added

**How to debug step-by-step**:
1. Enable debug logging to see interceptor execution order
2. Set breakpoints in individual interceptors
3. Monitor message transformations through pipeline
4. Test with single interceptor to isolate issues
5. Verify priority values are correct

## Cross-References

**Related classes**:
- `IRequestInterceptor` (request interceptor interface)
- `IResponseInterceptor` (response interceptor interface)
- `HeaderInjectorInterceptor` (example interceptor)
- `ResponseLoggerInterceptor` (example interceptor)
- `Flow` (HTTP flow context)

**Upstream callers**:
- Proxy server components
- HTTP flow handlers
- Request/response processing pipeline

**Downstream dependencies**:
- Individual interceptor implementations
- Logging infrastructure
- HTTP message models

**Documents to read before/after**:
- Before: Interceptor interface definitions
- After: Specific interceptor implementations
- After: HTTP flow model documentation

## Knowledge Transfer Notes

**Reusable concepts**:
- Chain of Responsibility implementation
- Pipeline pattern with async support
- Error isolation in processing chains
- Priority-based execution ordering
- Dependency injection for extensibility

**Project-specific elements**:
- HTTP request/response interception
- Snitcher proxy flow context
- Interceptor priority system
- Error logging and isolation strategy

**How to recreate pattern elsewhere**:
1. Define interceptor interfaces with Priority property
2. Create manager class with dependency injection
3. Implement pipeline execution with priority ordering
4. Add error isolation with try-catch blocks
5. Include logging for debugging and monitoring
6. Support cancellation tokens throughout
7. Use async/await for non-blocking processing

**Key insights**:
- Always isolate errors to prevent pipeline failures
- Use priority-based ordering for predictable execution
- Log interceptor execution for debugging
- Support cancellation for responsive operations
- Keep interceptors independent and composable
- Consider performance impact of interceptor chains
- Use dependency injection for testability and flexibility
- Implement both request and response pipelines symmetrically
