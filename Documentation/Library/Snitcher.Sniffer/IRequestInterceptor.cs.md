# IRequestInterceptor.cs

## Overview

`IRequestInterceptor.cs` defines the contract for HTTP request interceptors in the Snitcher proxy system. This interface establishes the standard pattern for implementing cross-cutting concerns that operate on outgoing HTTP requests, enabling modular, composable request processing through a unified interface.

**Why it exists**: To provide a standardized contract for request interception, enable pluggable request processing components, support the Chain of Responsibility pattern, and allow consistent implementation of cross-cutting concerns like logging, header injection, and request modification.

**What problem it solves**: Eliminates the need for scattered request processing code, provides a consistent interface for all request modifications, enables easy addition of new cross-cutting behaviors, and supports the interceptor pipeline architecture.

**What would break if removed**: The entire request interception system would collapse, requiring direct modifications to request handling code and losing the ability to compose multiple request processing behaviors.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure interface definition)

**Persistence**: Not applicable (processing contract)

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Async/await support, HTTP message model

## Architectural Role

**Layer**: Library Layer (Interface Definition)

**Responsibility Boundaries**:
- MUST define contract for request interception
- MUST support asynchronous operations
- MUST provide priority-based ordering
- MUST NOT contain implementation details
- MUST NOT depend on specific frameworks

**What it MUST do**:
- Define method signature for request interception
- Provide priority property for execution ordering
- Support cancellation tokens
- Enable async processing
- Accept flow context for request information

**What it MUST NOT do**:
- Implement actual interception logic
- Define specific request modifications
- Handle HTTP protocol details
- Include business logic

**Dependencies (Incoming)**: Interceptor implementations, InterceptorManager

**Dependencies (Outgoing**: HTTP message models, Flow context

## Execution Flow

**Where execution starts**: Interface used by InterceptorManager to call concrete implementations

**How control reaches this component**:
1. HTTP request enters proxy pipeline
2. InterceptorManager discovers all IRequestInterceptor implementations
3. Interceptors sorted by Priority property
4. InterceptAsync method called on each interceptor
5. Modified request passed to next interceptor

**Method Resolution Flow**:
1. Compile-time type checking against interface
2. Runtime method dispatch to concrete implementation
3. Async execution of interception logic
4. Result passed back through interface contract

**Priority Resolution**:
1. Each interceptor provides Priority value
2. Lower values indicate higher priority
3. Interceptors execute in ascending priority order
4. Enables predictable pipeline behavior

**Synchronous vs asynchronous behavior**: Interface requires async method to support non-blocking request processing

**Threading/Dispatcher notes**: No threading concerns - interface definition only

**Lifecycle**: Interface exists for application duration, implementations follow service lifetime patterns

## Public API / Surface Area

**Interface Definition**:
```csharp
public interface IRequestInterceptor
{
    Task<Models.HttpRequestMessage> InterceptAsync(Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default);
    int Priority { get; } // Lower = higher priority
}
```

**Methods**:
- `Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)` - Intercept and potentially modify request

**Properties**:
- `int Priority` - Execution priority (lower values execute first)

**Expected Input/Output**: Method takes HTTP request and flow context, returns potentially modified request after interception.

**Side Effects**: Interface defines contract for side effects in implementations (request modifications, logging, etc.).

**Error Behavior**: Interface doesn't define error handling - implementations determine exception handling strategy.

## Internal Logic Breakdown

**Interface Design Pattern** (lines 5-9):
```csharp
public interface IRequestInterceptor
{
    Task<Models.HttpRequestMessage> InterceptAsync(Models.HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default);
    int Priority { get; } // Lower = higher priority
}
```
- Minimal interface with single method and property
- Async method signature for non-blocking operations
- Priority property for execution ordering
- CancellationToken support for responsive operations

**Method Signature Analysis**:
- **Return Type**: `Task<HttpRequestMessage>` - Async operation returning modified request
- **Parameters**: Request message, flow context, cancellation token
- **Async Pattern**: Enables non-blocking request processing
- **Flow Context**: Provides request metadata and context

**Priority Property**:
- **Type**: `int` for simple numeric ordering
- **Semantics**: Lower values = higher priority
- **Usage**: Sorted by InterceptorManager for execution order
- **Flexibility**: Allows fine-grained control over execution sequence

**Parameter Design**:
- **Request**: The HTTP message to be intercepted
- **Flow**: Context information about the request flow
- **CancellationToken**: Supports operation cancellation
- **Default Parameters**: Optional cancellation token for flexibility

**Return Type Design**:
- **Task**: Async operation support
- **HttpRequestMessage**: Modified request message
- **Same Type**: Input and output types match for chaining
- **Null Safety**: Implementations should not return null

## Patterns & Principles Used

**Interceptor Pattern**: Defines contract for request processing interception

**Strategy Pattern**: Different implementations provide different interception strategies

**Chain of Responsibility**: Enables sequential processing through multiple interceptors

**Interface Segregation**: Clean, focused interface for single responsibility

**Async Pattern**: Supports non-blocking operations

**Priority Pattern**: Enables ordered execution of components

**Why these patterns were chosen**:
- Interceptor for cross-cutting concern handling
- Strategy for varied interception behaviors
- Chain of Responsibility for composable processing
- Interface Segregation for clean contracts
- Async for responsive request handling
- Priority for predictable execution order

**Trade-offs**:
- Simple interface may limit complex scenarios
- Priority system requires coordination between implementations
- Async pattern adds complexity to error handling
- No built-in error handling contract

**Anti-patterns avoided**:
- No implementation details in interface
- No framework-specific types
- No synchronous method signatures
- No complex parameter structures

## Binding / Wiring / Configuration

**Data Binding**: Not applicable (interface definition)

**Configuration Sources**: Priority values set in implementations

**Runtime Wiring**:
- Implemented by concrete interceptor classes
- Discovered by dependency injection container
- Used by InterceptorManager for pipeline execution

**Registration Points**:
- Concrete implementations registered in DI container
- Priority values defined in implementers
- Interface used for type resolution in DI

## Example Usage

**Minimal Example**:
```csharp
// Implementation example
public class LoggingInterceptor : IRequestInterceptor
{
    public int Priority => 100;
    
    public async Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Request: {request.Url}");
        return request;
    }
}
```

**Realistic Example**:
```csharp
// Complex interceptor with modification
public class AuthInterceptor : IRequestInterceptor
{
    public int Priority => 50; // High priority
    
    public async Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        if (request.Headers.ContainsKey("Authorization"))
            return request;
            
        request.Headers["Authorization"] = $"Bearer {GetToken()}";
        return request;
    }
}
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't implement synchronous version
public class BadInterceptor : IRequestInterceptor
{
    public HttpRequestMessage InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default) // Wrong signature
    {
        return request;
    }
}

// BAD - Don't forget priority property
public class IncompleteInterceptor : IRequestInterceptor
{
    public async Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        return request;
    }
    // Missing Priority property
}
```

**How to test in isolation**:
```csharp
// Test implementation
public class TestInterceptor : IRequestInterceptor
{
    public int Priority => 100;
    
    public async Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        request.Headers["X-Test"] = "Intercepted";
        return request;
    }
}

// Test usage
[Test]
public async Task Interceptor_ShouldModifyRequest()
{
    var interceptor = new TestInterceptor();
    var request = new HttpRequestMessage { Url = "http://example.com" };
    var flow = new Flow();
    
    var result = await interceptor.InterceptAsync(request, flow);
    
    Assert.AreEqual("Intercepted", result.Headers["X-Test"]);
}
```

**How to mock or replace**:
- Mock interface for testing InterceptorManager
- Create test implementations for specific scenarios
- Use mocking frameworks (Moq, NSubstitute)
- Implement fake interceptors for integration testing

## Extension & Modification Guide

**How to extend interface**:
1. Consider impact on existing implementations
2. Add new methods with default implementations if possible
3. Maintain backward compatibility
4. Update all implementations accordingly

**Where NOT to add logic**:
- Don't add implementation details to interface
- Don't add framework-specific dependencies
- Don't include business logic

**Safe extension points**:
- Additional methods for specific interception scenarios
- Properties for interceptor metadata
- Events for interception lifecycle
- Configuration interfaces

**Common mistakes**:
- Adding synchronous methods to async interface
- Changing method signatures without backward compatibility
- Forgetting to update all implementations
- Making priority requirements too complex

**Refactoring warnings**:
- Adding methods breaks all existing implementations
- Changing priority semantics affects execution order
- Consider versioning for interface evolution
- Test all implementations after changes

## Failure Modes & Debugging

**Common runtime errors**:
- NotImplementedException when methods not implemented
- InvalidOperationException when interceptors have conflicting priorities
- TaskCanceledException when cancellation requested

**Null/reference risks**:
- Request parameter may be null in some contexts
- Flow parameter may be null for simple requests
- Implementations should handle null gracefully

**Performance risks**:
- Many interceptors can slow request processing
- Complex interception logic adds overhead
- Async overhead for simple operations

**Logging points**: None in interface - implementations handle logging

**How to debug step-by-step**:
1. Verify interface implementation is correct
2. Check priority values for expected execution order
3. Test async method execution
4. Monitor request modifications through pipeline
5. Verify cancellation token handling

## Cross-References

**Related classes**:
- `IResponseInterceptor` (response counterpart)
- `InterceptorManager` (orchestrates execution)
- `HttpRequestMessage` (message processed)
- `Flow` (context provided)

**Upstream callers**:
- InterceptorManager (pipeline execution)
- HTTP request processing components

**Downstream dependencies**:
- Concrete interceptor implementations
- HTTP message model
- Flow context model

**Documents to read before/after**:
- Before: HTTP message model documentation
- After: Concrete interceptor implementations
- After: InterceptorManager orchestration logic

## Knowledge Transfer Notes

**Reusable concepts**:
- Interceptor pattern for cross-cutting concerns
- Async interface design for non-blocking operations
- Priority-based execution ordering
- Chain of Responsibility implementation
- Interface segregation for clean contracts

**Project-specific elements**:
- Snitcher HTTP message model
- Flow context for request metadata
- Priority system for interceptor coordination
- Cancellation token support for responsive operations

**How to recreate pattern elsewhere**:
1. Define interface with async method signature
2. Include priority property for execution ordering
3. Add cancellation token support
4. Keep interface minimal and focused
5. Use generic types appropriate to domain
6. Consider both input and output types for chaining
7. Document priority semantics clearly

**Key insights**:
- Always use async for network-related operations
- Include cancellation tokens for responsive behavior
- Use priority systems for predictable execution order
- Keep interfaces minimal and focused
- Consider both input and output for method signatures
- Design for composability and chaining
- Document execution order and priority semantics
- Provide clear contracts for implementers
