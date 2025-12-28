# InterceptorManager

## Overview
The InterceptorManager class is a centralized coordinator for applying HTTP request and response interceptors in a prioritized, fault-tolerant manner. It manages the execution pipeline for interceptor chains, ensuring that each interceptor gets a chance to modify HTTP traffic while isolating failures to prevent system-wide issues.

**Why it exists:** To provide a robust, ordered execution framework for HTTP traffic modification that can handle interceptor failures gracefully.

**Problem it solves:** Enables flexible, composable HTTP traffic modification through a chain of interceptors while ensuring that a single interceptor failure doesn't break the entire proxy operation.

**What would break if removed:** The interceptor system would become unmanaged. Individual interceptors would need to be manually coordinated, and there would be no centralized error handling or ordering guarantees.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Linq (for ordering operations)
  - Microsoft.Extensions.Logging.Abstractions (logging abstraction)
- **Persistence/communication:** In-memory processing only
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** .NET 8.0 runtime
- **Version hints:** Uses modern async patterns, cancellation token support, LINQ operations

## Architectural Role
- **Layer:** Application/Middleware layer
- **Responsibility boundaries:**
  - MUST: Apply interceptors in priority order, handle interceptor failures gracefully
  - MUST NOT: Implement specific interception logic, modify HTTP traffic directly
- **Dependencies:**
  - **Incoming:** IEnumerable<IRequestInterceptor>, IEnumerable<IResponseInterceptor>, ILogger
  - **Outgoing:** Modified HTTP messages, log entries

## Execution Flow
**Request Interception Flow:**
1. ApplyRequestInterceptorsAsync receives initial HTTP request
2. Orders request interceptors by Priority property (ascending)
3. Iterates through each interceptor in order
4. Calls InterceptAsync on each interceptor with current request and flow
5. Catches and logs exceptions from individual interceptors
6. Continues with remaining interceptors even if one fails
7. Returns final modified request

**Response Interception Flow:**
1. ApplyResponseInterceptorsAsync receives HTTP response
2. Orders response interceptors by Priority property (ascending)
3. Iterates through each interceptor in order
4. Calls InterceptAsync on each interceptor with current response and flow
5. Catches and logs exceptions from individual interceptors
6. Continues with remaining interceptors even if one fails
7. Returns final modified response

**Synchronous vs asynchronous:** All methods are async with proper cancellation token support.

**Threading notes:** Stateless design - safe for concurrent use. No shared state between method calls.

**Lifecycle:** Created per dependency injection → Apply methods called repeatedly → No disposal needed

## Public API / Surface Area
**Constructors:**
- `InterceptorManager(IEnumerable<IRequestInterceptor> requestInterceptors, IEnumerable<IResponseInterceptor> responseInterceptors, ILogger logger)` - Creates manager with interceptor collections and logging

**Public Methods:**
- `Task<HttpRequestMessage> ApplyRequestInterceptorsAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)` - Applies all request interceptors in priority order
- `Task<HttpResponseMessage> ApplyResponseInterceptorsAsync(HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)` - Applies all response interceptors in priority order

**Expected input/output:**
- Input: Original HTTP request/response messages and associated flow context
- Output: Modified HTTP request/response messages after interceptor processing

**Side effects:** Logs interceptor execution and failures, modifies HTTP messages through interceptor chain

**Error behavior:** Catches and logs exceptions from individual interceptors but continues processing remaining interceptors. Does not propagate interceptor failures to callers.

## Internal Logic Breakdown
**Lines 17-25 (Constructor):**
- Validates and injects all dependencies using null-checking pattern
- Stores interceptor collections for later processing
- No complex initialization - all work is done per request/response

**Lines 27-49 (ApplyRequestInterceptorsAsync):**
- Takes initial request as starting point for interceptor chain
- Uses LINQ OrderBy to sort interceptors by Priority property (ascending)
- Iterates through each interceptor in priority order
- Wraps each interceptor call in try-catch for fault isolation
- Logs interceptor application before execution
- Logs interceptor failures with full exception details
- Continues processing remaining interceptors even after failures
- Returns final request after all interceptors have been applied

**Lines 51-73 (ApplyResponseInterceptorsAsync):**
- Mirrors the request interceptor pattern for response processing
- Takes initial response as starting point for interceptor chain
- Orders response interceptors by Priority property
- Applies identical error handling and logging pattern
- Ensures response interceptor failures don't stop processing
- Returns final response after all interceptors have been applied

## Patterns & Principles Used
**Design Patterns:**
- **Chain of Responsibility:** Each interceptor gets a chance to process the message
- **Pipeline Pattern:** Sequential processing through ordered stages
- **Fail-Safe Pattern:** Continues processing despite individual component failures

**Architectural Patterns:**
- **Mediator Pattern:** Coordinates between multiple interceptor implementations
- **Decorator Pattern:** Each interceptor decorates the HTTP message

**Why these patterns were chosen:**
- Chain of Responsibility enables flexible composition of processing logic
- Pipeline pattern ensures predictable, ordered execution
- Fail-safe pattern prevents system failures from individual interceptor issues
- Mediator pattern centralizes coordination logic

**Trade-offs:**
- Priority-based ordering may be less flexible than dependency graphs
- Exception swallowing may hide serious interceptor bugs
- Sequential processing may impact performance with many interceptors

**Anti-patterns avoided:**
- No static global state (instance-based design)
- No tight coupling to specific interceptor implementations
- No blocking operations in async methods

## Binding / Wiring / Configuration
**Dependency Injection:**
- Constructor injection of interceptor collections and logger
- Interceptors registered separately and injected as collections
- No service locator pattern used

**Configuration Sources:**
- No external configuration
- Interceptor ordering controlled by Priority property
- Interceptor selection controlled by DI container registration

**Runtime Wiring:**
- Interceptor collections built at injection time
- No dynamic interceptor addition/removal during operation
- Priority ordering calculated on each call (cheap operation)

**Registration Points:**
- Should be registered as InterceptorManager in DI container
- Singleton lifetime appropriate (stateless)
- Interceptors registered as implementations of respective interfaces

## Example Usage
**Minimal Example:**
```csharp
var requestInterceptors = new List<IRequestInterceptor>
{
    new HeaderInjectorInterceptor(new Dictionary<string, string>
    {
        ["X-Proxy"] = "Snitcher"
    }, logger)
};

var responseInterceptors = new List<IResponseInterceptor>
{
    new ResponseLoggerInterceptor(logger)
};

var manager = new InterceptorManager(requestInterceptors, responseInterceptors, logger);

// Apply to request
var modifiedRequest = await manager.ApplyRequestInterceptorsAsync(originalRequest, flow);

// Apply to response
var modifiedResponse = await manager.ApplyResponseInterceptorsAsync(originalResponse, flow);
```

**Realistic Example with Multiple Interceptors:**
```csharp
public class ProxyProcessingPipeline
{
    private readonly InterceptorManager _interceptorManager;
    
    public ProxyProcessingPipeline(InterceptorManager interceptorManager)
    {
        _interceptorManager = interceptorManager;
    }
    
    public async Task<HttpRequestMessage> ProcessRequestAsync(HttpRequestMessage request, Flow flow)
    {
        // Apply all registered request interceptors
        return await _interceptorManager.ApplyRequestInterceptorsAsync(request, flow);
    }
    
    public async Task<HttpResponseMessage> ProcessResponseAsync(HttpResponseMessage response, Flow flow)
    {
        // Apply all registered response interceptors
        return await _interceptorManager.ApplyResponseInterceptorsAsync(response, flow);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Passing null interceptor collections
var manager = new InterceptorManager(null, null, logger);

// WRONG: Modifying interceptor collections after creation
var interceptors = new List<IRequestInterceptor>();
var manager = new InterceptorManager(interceptors, responseInterceptors, logger);
interceptors.Add(new CustomInterceptor()); // Won't be used by manager

// WRONG: Assuming exceptions are propagated
try
{
    var result = await manager.ApplyRequestInterceptorsAsync(request, flow);
}
catch (InterceptorException ex) // This will never be caught
{
    // InterceptorManager swallows interceptor exceptions
}
```

**How to test in isolation:**
```csharp
[Test]
public async Task ApplyRequestInterceptors_ShouldApplyInPriorityOrder()
{
    // Arrange
    var logger = new Mock<ILogger>();
    var interceptor1 = new Mock<IRequestInterceptor>();
    var interceptor2 = new Mock<IRequestInterceptor>();
    
    interceptor1.Setup(x => x.Priority).Returns(100);
    interceptor2.Setup(x => x.Priority).Returns(50); // Higher priority
    
    var manager = new InterceptorManager(
        new[] { interceptor1.Object, interceptor2.Object },
        Enumerable.Empty<IResponseInterceptor>(),
        logger.Object);
    
    var request = new HttpRequestMessage();
    var flow = new Flow();
    
    // Act
    var result = await manager.ApplyRequestInterceptorsAsync(request, flow);
    
    // Assert
    interceptor2.Verify(x => x.InterceptAsync(It.IsAny<HttpRequestMessage>(), flow, It.IsAny<CancellationToken>()), Times.Once);
    interceptor1.Verify(x => x.InterceptAsync(It.IsAny<HttpRequestMessage>(), flow, It.IsAny<CancellationToken>()), Times.Once);
}
```

**How to mock or replace it:**
```csharp
public class MockInterceptorManager
{
    public List<HttpRequestMessage> ProcessedRequests { get; } = new();
    public List<HttpResponseMessage> ProcessedResponses { get; } = new();
    
    public async Task<HttpRequestMessage> ApplyRequestInterceptorsAsync(
        HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        ProcessedRequests.Add(request);
        await Task.Delay(1, cancellationToken); // Simulate processing
        return request; // Return unmodified
    }
    
    public async Task<HttpResponseMessage> ApplyResponseInterceptorsAsync(
        HttpResponseMessage response, Flow flow, CancellationToken cancellationToken = default)
    {
        ProcessedResponses.Add(response);
        await Task.Delay(1, cancellationToken); // Simulate processing
        return response; // Return unmodified
    }
}
```

## Extension & Modification Guide
**How to add new features:**
- **Conditional interceptor execution:** Add filtering logic before interceptor calls
- **Parallel interceptor execution:** Add parallel processing for independent interceptors
- **Interceptor metrics:** Add timing and performance tracking
- **Dynamic interceptor registration:** Add runtime interceptor addition/removal

**Where NOT to add logic:**
- Don't add specific HTTP modification logic (belongs in interceptors)
- Don't add network connection handling (belongs in connection managers)
- Don't add business logic for traffic analysis

**Safe extension points:**
- Pre/post processing around interceptor calls
- Additional logging and monitoring
- Custom error handling strategies
- Performance measurement and optimization

**Common mistakes:**
- Modifying interceptor collections during processing (causes enumeration issues)
- Assuming interceptors are stateless (some may maintain state)
- Blocking operations in interceptor processing
- Ignoring cancellation tokens in long-running operations

**Refactoring warnings:**
- Changing priority ordering logic will break interceptor execution order
- Removing exception handling will make system fragile to interceptor failures
- Modifying async patterns may break caller expectations

## Failure Modes & Debugging
**Common runtime errors:**
- `ArgumentNullException`: Null interceptor collections or logger
- `InvalidOperationException`: Issues with interceptor state or dependencies
- Propagated exceptions from interceptor implementations (caught but logged)

**Null/reference risks:**
- All dependencies validated in constructor
- Request/response objects assumed valid from upstream
- Flow object passed through without null checks

**Performance risks:**
- Many interceptors may create processing overhead
- Sequential processing limits parallelism
- Exception handling adds slight overhead to each interceptor call
- LINQ ordering on every call (though cheap for small collections)

**Logging points:**
- Interceptor application before each execution
- Interceptor failures with full exception details
- No logging for successful completion (relies on interceptor logging)

**How to debug step-by-step:**
1. Enable debug logging to see interceptor execution order
2. Set breakpoints in ApplyRequestInterceptorsAsync to trace processing
3. Monitor interceptor priority ordering with LINQ debugging
4. Check exception handling for failing interceptors
5. Verify interceptor collections are properly populated
6. Test with various interceptor combinations and priorities

## Cross-References
**Related classes:**
- `IRequestInterceptor` - Interface for request modification interceptors
- `IResponseInterceptor` - Interface for response modification interceptors
- `HeaderInjectorInterceptor` - Example request interceptor implementation
- `ResponseLoggerInterceptor` - Example response interceptor implementation

**Upstream callers:**
- `ConnectionHandler` - Uses interceptor manager for traffic processing
- Test fixtures - Direct calls for interceptor chain testing

**Downstream dependencies:**
- Individual interceptor implementations for specific traffic modifications
- Logging infrastructure for debugging and monitoring

**Documents that should be read before/after:**
- Before: IRequestInterceptor/IResponseInterceptor interface documentation
- Before: Individual interceptor implementation documentation
- After: ConnectionHandler documentation (interceptor manager usage)
- Related: Interceptor design patterns documentation

## Knowledge Transfer Notes
**Reusable concepts:**
- Chain of Responsibility pattern implementation
- Fault-tolerant pipeline processing
- Priority-based execution ordering
- Graceful error handling in processing pipelines
- Dependency injection for extensible component composition

**Project-specific elements:**
- Snitcher's interceptor interface definitions
- Integration with Snitcher's flow tracking system
- Specific logging patterns for interceptor debugging
- Priority system for interceptor execution order

**How to recreate this pattern from scratch elsewhere:**
1. Define interfaces for interceptor components with priority ordering
2. Create manager class to coordinate interceptor execution
3. Implement priority-based ordering using LINQ or custom sorting
4. Add comprehensive error handling with fault isolation
5. Include detailed logging for debugging and monitoring
6. Ensure async/await patterns throughout the pipeline
7. Add cancellation token support for long-running operations
8. Design for dependency injection and testability

**Key architectural insights:**
- Pipeline pattern enables flexible, composable processing logic
- Fault isolation prevents individual component failures from breaking the system
- Priority-based ordering provides predictable execution sequence
- Comprehensive logging is essential for debugging complex interceptor chains
- Async patterns prevent blocking in network processing scenarios
