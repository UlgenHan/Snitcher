# Flow

## Overview

Flow is a core domain model in the Snitcher.Sniffer library that represents a complete HTTP/HTTPS transaction from client to server and back. It encapsulates all relevant information about a network request/response pair, including timing data, participant addresses, and the actual HTTP messages. This model serves as the fundamental unit of network traffic analysis and monitoring.

**Why it exists**: To provide a standardized, comprehensive representation of network flows that can be captured, stored, analyzed, and displayed throughout the Snitcher application. It enables consistent handling of HTTP traffic data across all components.

**Problem it solves**: Without this model, network traffic data would be scattered across different formats and structures, making it impossible to provide consistent analysis, storage, or visualization of HTTP transactions.

**What would break if removed**: All network monitoring and analysis functionality would fail. The proxy server couldn't capture traffic, storage systems couldn't persist data, and UI components couldn't display network information.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0

**Libraries**: None (pure domain model)

**UI frameworks**: N/A (domain layer)

**Persistence / communication technologies**: Entity Framework Core (implicit), serialization frameworks

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, serialization support

**Version hints**: Uses modern C# features, nullable reference types, record-like structure

## Architectural Role

**Layer**: Domain Layer (Core)

**Responsibility boundaries**:
- MUST represent complete HTTP transaction data
- MUST provide timing and status information
- MUST contain request and response messages
- MUST NOT contain network connection details
- MUST NOT implement processing logic

**What it MUST do**:
- Store unique identifier for the flow
- Capture timing information (timestamp, duration)
- Record client and server addresses
- Contain HTTP request and response messages
- Track flow status (pending, completed, failed)

**What it MUST NOT do**:
- Parse or modify HTTP messages
- Handle network connections
- Implement business logic for analysis
- Access external resources

**Dependencies (incoming)**: Proxy server, connection handlers, storage systems, UI components

**Dependencies (outgoing)**: HttpRequestMessage, HttpResponseMessage, FlowStatus enum

## Execution Flow

**Where execution starts**: Flow objects are created when network transactions are captured by the proxy infrastructure.

**How control reaches this component**:
1. Proxy server captures network connection
2. Connection handler processes HTTP transaction
3. Flow object created with transaction data
4. Flow passed to storage and analysis components
5. UI components display flow information

**Call sequence (step-by-step)**:
1. Network request intercepted
2. Request data extracted and stored in Flow
3. Network response received
4. Response data stored in Flow
5. Timing calculated and status set
6. Flow emitted via events or stored

**Synchronous vs asynchronous behavior**: Synchronous object creation and manipulation

**Threading / dispatcher / event loop notes**: Thread-safe for read operations, but instances should not be modified concurrently without synchronization

**Lifecycle**: Created → Populated with Request → Populated with Response → Completed → Stored/Displayed

## Public API / Surface Area

**Constructors**:
- `public Flow()`: Default constructor with automatic ID and timestamp generation

**Properties**:
- `Guid Id`: Unique identifier for the flow
- `DateTime Timestamp`: When the flow was created
- `string ClientAddress`: IP address of the client
- `HttpRequestMessage Request`: HTTP request message
- `HttpResponseMessage Response`: HTTP response message
- `TimeSpan Duration`: Total transaction duration
- `FlowStatus Status`: Current status of the flow

**Expected input/output**:
- Input: HTTP message data, client address, timing information
- Output: Complete flow representation for analysis and storage

**Side effects**: None - pure data model

**Error behavior**: No exceptions thrown - data model only stores information

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Class definition and properties (lines 3-11)**:
```csharp
public class Flow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ClientAddress { get; set; } = string.Empty;
    public HttpRequestMessage Request { get; set; } = new();
    public HttpResponseMessage Response { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public FlowStatus Status { get; set; }
}
```
- Automatic ID generation using GUID for uniqueness
- Timestamp set to UTC for consistent timezone handling
- Client address stored as string for flexibility
- Request and Response objects initialized to prevent nulls
- Duration calculated externally and stored
- Status tracks transaction lifecycle

**FlowStatus enum (lines 14-19)**:
```csharp
public enum FlowStatus
{
    Pending,
    Completed,
    Failed
}
```
- Pending: Request received, response pending
- Completed: Full transaction completed successfully
- Failed: Transaction failed or was interrupted

**Algorithms used**: None - pure data model with automatic initialization

**Conditional logic**: None - simple property storage

**State transitions**:
- Created → Pending (when request received)
- Pending → Completed (when response received successfully)
- Pending → Failed (when error occurs)
- Any state can be set directly for flexibility

**Important invariants**:
- Id is always unique and never changes
- Timestamp always reflects creation time in UTC
- Request and Response are never null
- Status reflects actual transaction state

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Data Transfer Object Pattern**: Carries data between layers
- **Value Object Pattern**: Immutable identity through GUID
- **Domain Model Pattern**: Rich domain representation

**Architectural patterns**:
- **Domain-Driven Design**: Ubiquitous language for network flows
- **Clean Architecture**: Domain model with no external dependencies

**Why these patterns were chosen (inferred)**:
- DTO pattern enables clean data transfer between components
- Value object pattern ensures consistent identity
- Domain model provides rich, meaningful representation

**Trade-offs**:
- Rich domain model vs anemic DTO: More expressive but heavier
- Automatic initialization vs lazy loading: Convenient but less memory efficient
- String address vs structured type: Simpler but less type-safe

**Anti-patterns avoided or possibly introduced**:
- Avoided: Anemic data model
- Avoided: Primitive obsession for complex data
- Possible risk: Model becoming too large over time

## Binding / Wiring / Configuration

**Dependency injection**: Not registered in DI container - data model

**Data binding (if UI)**: UI components bind to Flow properties for display

**Configuration sources**: None - pure data model

**Runtime wiring**: Created by infrastructure components, passed via events or method calls

**Registration points**: Created dynamically by connection handlers and proxy components

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Create a new flow
var flow = new Flow
{
    ClientAddress = "192.168.1.100",
    Status = FlowStatus.Pending
};
```

**Realistic example**:
```csharp
public class ConnectionHandler
{
    public Flow CreateFlowFromRequest(HttpRequestMessage request, string clientAddress)
    {
        return new Flow
        {
            ClientAddress = clientAddress,
            Request = request,
            Status = FlowStatus.Pending
        };
    }
    
    public void CompleteFlow(Flow flow, HttpResponseMessage response)
    {
        flow.Response = response;
        flow.Duration = DateTime.UtcNow - flow.Timestamp;
        flow.Status = FlowStatus.Completed;
    }
    
    public void FailFlow(Flow flow, Exception error)
    {
        flow.Status = FlowStatus.Failed;
        flow.Duration = DateTime.UtcNow - flow.Timestamp;
    }
}
```

**Storage example**:
```csharp
public class FlowStorage
{
    public async Task StoreFlowAsync(Flow flow)
    {
        // Serialize and store flow
        var json = JsonSerializer.Serialize(flow);
        await File.WriteAllTextAsync($"flows/{flow.Id}.json", json);
    }
    
    public async Task<Flow?> LoadFlowAsync(Guid id)
    {
        var json = await File.ReadAllTextAsync($"flows/{id}.json");
        return JsonSerializer.Deserialize<Flow>(json);
    }
}
```

**UI display example**:
```csharp
public class FlowViewModel
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Method { get; set; }
    public int StatusCode { get; set; }
    public string Duration { get; set; }
    public string Status { get; set; }
    
    public static FlowViewModel FromFlow(Flow flow)
    {
        return new FlowViewModel
        {
            Id = flow.Id.ToString(),
            Url = flow.Request.RequestUri?.ToString() ?? "",
            Method = flow.Request.Method.ToString(),
            StatusCode = (int)flow.Response.StatusCode,
            Duration = $"{flow.Duration.TotalMilliseconds:F0}ms",
            Status = flow.Status.ToString()
        };
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Modifying flow after completion
var flow = new Flow();
// ... flow completed and stored
flow.Status = FlowStatus.Pending; // Should not modify stored flows

// WRONG: Assuming thread safety
Parallel.ForEach(flows, flow =>
{
    flow.Duration = TimeSpan.FromSeconds(1); // Race condition if accessed concurrently
});

// WRONG: Null references
var flow = new Flow();
var url = flow.Request.RequestUri?.ToString(); // Request is empty but not null
// Should check if request has meaningful data

// WRONG: Changing ID
flow.Id = Guid.NewGuid(); // Should never change identity
```

**How to test this in isolation**:
```csharp
[Test]
public void Flow_ShouldInitializeWithCorrectValues()
{
    // Arrange & Act
    var flow = new Flow();
    
    // Assert
    Assert.That(flow.Id, Is.Not.EqualTo(Guid.Empty));
    Assert.That(flow.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
    Assert.That(flow.ClientAddress, Is.EqualTo(string.Empty));
    Assert.That(flow.Request, Is.Not.Null);
    Assert.That(flow.Response, Is.Not.Null);
    Assert.That(flow.Status, Is.EqualTo(default(FlowStatus)));
}

[Test]
public void Flow_ShouldStoreDataCorrectly()
{
    // Arrange
    var flow = new Flow();
    var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
    var response = new HttpResponseMessage(HttpStatusCode.OK);
    
    // Act
    flow.ClientAddress = "192.168.1.1";
    flow.Request = request;
    flow.Response = response;
    flow.Status = FlowStatus.Completed;
    flow.Duration = TimeSpan.FromMilliseconds(150);
    
    // Assert
    Assert.That(flow.ClientAddress, Is.EqualTo("192.168.1.1"));
    Assert.That(flow.Request.Method, Is.EqualTo(HttpMethod.Get));
    Assert.That(flow.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(flow.Status, Is.EqualTo(FlowStatus.Completed));
    Assert.That(flow.Duration.TotalMilliseconds, Is.EqualTo(150));
}
```

**How to mock or replace it**:
```csharp
// Create test flows with builder pattern
public class FlowBuilder
{
    private readonly Flow _flow = new();
    
    public FlowBuilder WithClient(string address)
    {
        _flow.ClientAddress = address;
        return this;
    }
    
    public FlowBuilder WithRequest(HttpMethod method, string url)
    {
        _flow.Request = new HttpRequestMessage(method, url);
        return this;
    }
    
    public FlowBuilder Completed()
    {
        _flow.Status = FlowStatus.Completed;
        _flow.Duration = TimeSpan.FromMilliseconds(100);
        return this;
    }
    
    public Flow Build() => _flow;
}

// Usage in tests
var testFlow = new FlowBuilder()
    .WithClient("127.0.0.1")
    .WithRequest(HttpMethod.Get, "http://test.com")
    .Completed()
    .Build();
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new properties for additional flow data
2. Add new status values to FlowStatus enum
3. Add validation methods if needed
4. Consider serialization implications
5. Update UI models accordingly

**Where NOT to add logic**:
- Don't add HTTP processing logic
- Don't add network communication code
- Don't add business rule validation
- Don't add persistence logic

**Safe extension points**:
- New properties for additional metadata
- Enhanced status tracking
- Validation helper methods
- Computed properties for derived data

**Common mistakes**:
- Adding too many properties (violates SRP)
- Adding behavior that belongs in services
- Making properties that should be computed
- Forgetting serialization implications

**Refactoring warnings**:
- Changing property types breaks serialization
- Removing properties affects existing data
- Modifying enum values affects status handling
- Changing ID generation affects uniqueness

## Failure Modes & Debugging

**Common runtime errors**:
- **SerializationException**: From serialization frameworks
- **ArgumentException**: From invalid property assignments
- **NullReferenceException**: If Request/Response not properly initialized

**Null/reference risks**:
- Request and Response initialized to prevent nulls
- ClientAddress can be empty but not null
- ID never null due to automatic initialization
- Timestamp always set in constructor

**Performance risks**:
- Large HTTP message bodies in memory
- Too many flows causing memory pressure
- Serialization overhead for storage
- String operations in UI binding

**Logging points**:
- No built-in logging (domain model separation)
- Flow creation should be logged by creating components
- Status changes should be logged by services

**How to debug step-by-step**:
1. Monitor flow creation in connection handlers
2. Check property values during population
3. Verify status transitions during processing
4. Test serialization/deserialization
5. Validate UI binding with flow data

## Cross-References

**Related classes**:
- HttpRequestMessage (HTTP request data)
- HttpResponseMessage (HTTP response data)
- FlowStatus (transaction status)
- FlowEventArgs (event wrapper)

**Upstream callers**:
- Connection handlers create flows
- Proxy server coordinates flow creation
- Storage components persist flows

**Downstream dependencies**:
- UI components display flows
- Analysis services process flows
- Storage systems serialize flows

**Documents that should be read before/after**:
- Read: HttpRequestMessage documentation
- Read: HttpResponseMessage documentation
- Read: FlowStatus documentation
- Read: Connection handler documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Domain model pattern for network data
- Data transfer object patterns
- Automatic ID generation patterns
- Status tracking with enums
- UTC timestamp handling

**What is project-specific**:
- Specific HTTP message structure
- FlowStatus enum values
- Property naming conventions
- Integration with Snitcher proxy system

**How to recreate this pattern from scratch elsewhere**:
1. Define domain model class for transaction data
2. Add unique identifier with automatic generation
3. Include timestamp for audit trail
4. Add status tracking with enum
5. Include related data objects as properties
6. Initialize objects to prevent null references
7. Consider serialization and storage requirements

**Key insights for implementation**:
- Always initialize objects to prevent null references
- Use UTC timestamps for consistent timezone handling
- Generate IDs automatically to ensure uniqueness
- Keep domain models focused on data representation
- Consider serialization implications when designing
- Use enums for status tracking for type safety
