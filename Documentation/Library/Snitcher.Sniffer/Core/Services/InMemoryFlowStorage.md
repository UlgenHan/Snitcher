# InMemoryFlowStorage

## Overview
The InMemoryFlowStorage class is a thread-safe, in-memory repository for storing and retrieving HTTP flow data captured by the proxy. It provides CRUD operations for flow objects with support for querying by ID, filtering with predicates, and limiting result sets. All data is stored in memory and lost when the application restarts.

**Why it exists:** To provide a simple, fast storage solution for flow data during development, testing, and scenarios where persistence is not required.

**Problem it solves:** Enables immediate storage and retrieval of flow data without the complexity of database setup, file I/O, or external dependencies.

**What would break if removed:** The proxy would have nowhere to store flow data. All flow tracking and historical analysis capabilities would be lost unless another storage implementation is provided.

## Tech Stack Identification
- **Languages used:** C# (.NET 8.0)
- **Frameworks:** .NET 8.0 SDK
- **Libraries:** 
  - System.Collections.Generic (Dictionary for storage)
  - System.Linq (for querying operations)
- **Persistence/communication:** In-memory storage only
- **Build tools:** MSBuild with .NET SDK
- **Runtime assumptions:** .NET 8.0 runtime, sufficient memory for flow storage
- **Version hints:** Uses modern async patterns, cancellation token support, LINQ operations

## Architectural Role
- **Layer:** Infrastructure/Storage layer
- **Responsibility boundaries:**
  - MUST: Store flows, retrieve by ID, support filtering and limiting
  - MUST NOT: Provide persistence across restarts, handle complex queries, manage memory optimization
- **Dependencies:**
  - **Incoming:** None (pure storage implementation)
  - **Outgoing:** Stored flow objects, query results

## Execution Flow
**Storage Flow:**
1. StoreFlowAsync receives flow object
2. Acquires lock for thread safety
3. Stores flow in dictionary using flow.Id as key
4. Releases lock and returns completed task

**Retrieval Flow:**
1. GetFlowAsync receives flow ID
2. Acquires lock for thread safety
3. Looks up flow in dictionary by ID
4. Returns flow or null if not found

**Query Flow:**
1. GetFlowsAsync receives optional limit or predicate
2. Acquires lock for thread safety
3. Filters and orders flows by timestamp (descending)
4. Applies limit if specified
5. Returns filtered result set

**Clear Flow:**
1. ClearAsync acquires lock for thread safety
2. Removes all flows from dictionary
3. Releases lock and returns completed task

**Synchronous vs asynchronous:** Methods are async but return Task.FromResult for immediate completion since all operations are in-memory.

**Threading notes:** Uses lock statements to ensure thread safety for concurrent access. All operations are atomic.

**Lifecycle:** Created per dependency injection → Store/retrieve operations called repeatedly → Data lost on application restart

## Public API / Surface Area
**Constructors:**
- `InMemoryFlowStorage()` - Creates empty in-memory storage

**Public Methods:**
- `Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default)` - Stores a flow object
- `Task<Flow?> GetFlowAsync(Guid id, CancellationToken cancellationToken = default)` - Retrieves a specific flow by ID
- `Task<IEnumerable<Flow>> GetFlowsAsync(int? limit = null, CancellationToken cancellationToken = default)` - Retrieves flows with optional limit
- `Task<IEnumerable<Flow>> GetFlowsAsync(Func<Flow, bool> predicate, CancellationToken cancellationToken = default)` - Retrieves flows matching predicate
- `Task ClearAsync(CancellationToken cancellationToken = default)` - Removes all stored flows

**Expected input/output:**
- Input: Flow objects for storage, GUID IDs for retrieval, predicates for filtering
- Output: Stored flows, query results, null for missing flows

**Side effects:** Modifies internal dictionary state, consumes memory for stored flows

**Error behavior:** Does not throw exceptions for normal operations. Null flow handling depends on usage patterns.

## Internal Logic Breakdown
**Lines 8-9 (Field Initialization):**
- Creates Dictionary<Guid,Flow> for flow storage
- Creates lock object for thread synchronization

**Lines 11-18 (StoreFlowAsync):**
- Acquires lock to ensure thread safety
- Stores flow using flow.Id as dictionary key
- Overwrites existing flow with same ID (upsert behavior)
- Returns Task.CompletedTask for async compliance

**Lines 20-26 (GetFlowAsync):**
- Acquires lock for thread-safe read access
- Uses TryGetValue for safe dictionary lookup
- Returns Task.FromResult with flow or null

**Lines 28-38 (GetFlowsAsync with limit):**
- Acquires lock for thread-safe read access
- Orders flows by timestamp in descending order (newest first)
- Applies Take() operator if limit is specified
- Returns Task.FromResult with filtered result

**Lines 41-48 (GetFlowsAsync with predicate):**
- Acquires lock for thread-safe read access
- Applies Where() filter with provided predicate
- Orders results by timestamp descending
- Returns Task.FromResult with filtered result

**Lines 50-57 (ClearAsync):**
- Acquires lock for thread-safe modification
- Calls Clear() to remove all dictionary entries
- Returns Task.CompletedTask for async compliance

## Patterns & Principles Used
**Design Patterns:**
- **Repository Pattern:** Provides collection-like interface for flow storage
- **Null Object Pattern:** Returns null for missing flows instead of exceptions

**Architectural Patterns:**
- **In-Memory Storage Pattern:** Volatile storage for testing and development
- **Thread-Safe Pattern:** Uses locks for concurrent access

**Why these patterns were chosen:**
- Repository pattern provides consistent interface across different storage implementations
- In-memory storage eliminates external dependencies for simple scenarios
- Thread safety is essential for concurrent proxy operations

**Trade-offs:**
- Data is lost on application restart
- Memory usage grows indefinitely without cleanup
- No complex querying capabilities beyond basic filtering
- Lock contention may impact performance under high concurrency

**Anti-patterns avoided:**
- No static global state (instance-based design)
- No memory leaks (flows can be cleared)
- Proper exception handling with lock usage

## Binding / Wiring / Configuration
**Dependency Injection:**
- No constructor dependencies (pure storage implementation)
- Simple registration as IFlowStorage implementation

**Configuration Sources:**
- No external configuration
- Behavior controlled entirely by method parameters
- No runtime configuration changes

**Runtime Wiring:**
- No dynamic configuration
- Storage behavior is consistent and predictable
- Memory usage depends on flow storage patterns

**Registration Points:**
- Should be registered as IFlowStorage in DI container
- Singleton lifetime appropriate (shared storage instance)
- Can be replaced with database implementation for production

## Example Usage
**Minimal Example:**
```csharp
var storage = new InMemoryFlowStorage();
var flow = new Flow { Id = Guid.NewGuid(), ClientAddress = "127.0.0.1" };

// Store flow
await storage.StoreFlowAsync(flow);

// Retrieve flow
var retrieved = await storage.GetFlowAsync(flow.Id);

// Get all flows
var allFlows = await storage.GetFlowsAsync();
```

**Realistic Example with Querying:**
```csharp
public class FlowAnalysisService
{
    private readonly IFlowStorage _storage;
    
    public FlowAnalysisService(IFlowStorage storage)
    {
        _storage = storage;
    }
    
    public async Task<List<Flow>> GetRecentErrorsAsync()
    {
        var errorFlows = await _storage.GetFlowsAsync(f => 
            f.Status == FlowStatus.Failed && 
            f.Timestamp > DateTime.UtcNow.AddHours(-1));
        
        return errorFlows.ToList();
    }
    
    public async Task<List<Flow>> GetFlowsForClientAsync(string clientAddress)
    {
        var clientFlows = await _storage.GetFlowsAsync(f => 
            f.ClientAddress == clientAddress);
        
        return clientFlows.OrderByDescending(f => f.Timestamp).ToList();
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Expecting persistence across restarts
var storage = new InMemoryFlowStorage();
await storage.StoreFlowAsync(flow);
// Data will be lost when application restarts

// WRONG: Assuming thread safety without locks
// The implementation uses locks, but callers might
// assume operations are atomic across multiple calls

// WRONG: Memory management concerns
var storage = new InMemoryFlowStorage();
for (int i = 0; i < 1000000; i++)
{
    await storage.StoreFlowAsync(CreateLargeFlow());
}
// This may consume excessive memory
```

**How to test in isolation:**
```csharp
[Test]
public async Task StoreAndGetFlow_ShouldReturnSameFlow()
{
    // Arrange
    var storage = new InMemoryFlowStorage();
    var flow = new Flow 
    { 
        Id = Guid.NewGuid(),
        ClientAddress = "test-client",
        Request = new HttpRequestMessage { Method = HttpMethod.GET }
    };
    
    // Act
    await storage.StoreFlowAsync(flow);
    var retrieved = await storage.GetFlowAsync(flow.Id);
    
    // Assert
    Assert.That(retrieved, Is.Not.Null);
    Assert.That(retrieved.Id, Is.EqualTo(flow.Id));
    Assert.That(retrieved.ClientAddress, Is.EqualTo(flow.ClientAddress));
}

[Test]
public async Task GetFlowsWithPredicate_ShouldFilterCorrectly()
{
    // Arrange
    var storage = new InMemoryFlowStorage();
    var flow1 = new Flow { ClientAddress = "client1", Status = FlowStatus.Completed };
    var flow2 = new Flow { ClientAddress = "client2", Status = FlowStatus.Failed };
    
    await storage.StoreFlowAsync(flow1);
    await storage.StoreFlowAsync(flow2);
    
    // Act
    var failedFlows = await storage.GetFlowsAsync(f => f.Status == FlowStatus.Failed);
    
    // Assert
    Assert.That(failedFlows.Count(), Is.EqualTo(1));
    Assert.That(failedFlows.First().ClientAddress, Is.EqualTo("client2"));
}
```

**How to mock or replace it:**
```csharp
public class MockFlowStorage : IFlowStorage
{
    private readonly Dictionary<Guid, Flow> _flows = new();
    
    public Task StoreFlowAsync(Flow flow, CancellationToken cancellationToken = default)
    {
        _flows[flow.Id] = flow;
        return Task.CompletedTask;
    }
    
    public Task<Flow?> GetFlowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_flows.TryGetValue(id, out var flow) ? flow : null);
    }
    
    public Task<IEnumerable<Flow>> GetFlowsAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var flows = _flows.Values.OrderByDescending(f => f.Timestamp);
        if (limit.HasValue)
            flows = flows.Take(limit.Value);
        return Task.FromResult(flows);
    }
    
    public Task<IEnumerable<Flow>> GetFlowsAsync(Func<Flow, bool> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_flows.Values.Where(predicate).OrderByDescending(f => f.Timestamp));
    }
    
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _flows.Clear();
        return Task.CompletedTask;
    }
    
    // Test helper methods
    public int Count => _flows.Count;
    public bool Contains(Guid id) => _flows.ContainsKey(id);
}
```

## Extension & Modification Guide
**How to add new features:**
- **Memory limits:** Add maximum flow count with automatic cleanup
- **Time-based expiration:** Add automatic removal of old flows
- **Complex queries:** Add more sophisticated filtering and sorting options
- **Statistics:** Add methods for flow analytics and summaries

**Where NOT to add logic:**
- Don't add persistence to disk or database (belongs in different implementation)
- Don't add network operations (belongs in service layer)
- Don't add business logic for flow analysis

**Safe extension points:**
- Additional query methods following existing patterns
- Memory management and cleanup features
- Performance optimization for large datasets

**Common mistakes:**
- Adding blocking operations to async methods
- Not considering memory growth over time
- Ignoring thread safety in new methods
- Adding complex queries that impact performance

**Refactoring warnings:**
- Changing thread safety model may break concurrent usage
- Modifying dictionary structure may affect all query methods
- Removing lock statements will cause race conditions

## Failure Modes & Debugging
**Common runtime errors:**
- `OutOfMemoryException`: Storing too many large flow objects
- `InvalidOperationException`: Dictionary modification during enumeration (unlikely with locks)
- `ArgumentNullException`: Null flow objects passed to storage methods

**Null/reference risks:**
- Flow objects assumed valid from callers
- Dictionary operations safe with null checks
- Lock object always initialized

**Performance risks:**
- Memory usage grows linearly with stored flows
- Lock contention under high concurrent access
- LINQ operations may be slow with large datasets
- No indexing for efficient querying

**Logging points:**
- No built-in logging (pure storage implementation)
- Relies on upstream components for logging storage operations

**How to debug step-by-step:**
1. Monitor flow storage count and memory usage
2. Check thread contention on lock object
3. Verify flow IDs are unique when storing
4. Test concurrent access with multiple threads
5. Monitor query performance with large datasets
6. Check predicate filtering logic for correctness

## Cross-References
**Related classes:**
- `IFlowStorage` - Interface defining storage contract
- `Flow` - Model representing HTTP transaction data
- `ConnectionHandler` - Uses storage to persist completed flows
- `ProxyServer` - May use storage for flow monitoring

**Upstream callers:**
- `ConnectionHandler` - Stores flows after processing
- Flow analysis services - Query stored flows for insights
- Test fixtures - Direct calls for storage testing

**Downstream dependencies:**
- Flow model objects for data representation
- Dictionary collection for in-memory storage

**Documents that should be read before/after:**
- Before: IFlowStorage interface documentation
- Before: Flow model documentation
- After: ConnectionHandler documentation (storage usage)
- Related: Database storage implementations for production

## Knowledge Transfer Notes
**Reusable concepts:**
- Thread-safe in-memory repository pattern
- Dictionary-based storage with GUID keys
- LINQ-based querying and filtering
- Async interface implementation with synchronous operations
- Lock-based thread synchronization

**Project-specific elements:**
- Snitcher's flow model structure
- Integration with Snitcher's proxy workflow
- Specific query patterns for flow analysis
- Timestamp-based ordering for recent data access

**How to recreate this pattern from scratch elsewhere:**
1. Define interface for storage operations with async methods
2. Create thread-safe implementation using Dictionary and locks
3. Implement CRUD operations (Create, Read, Update, Delete)
4. Add querying capabilities with LINQ support
5. Include filtering and limiting options
6. Ensure async compliance even for synchronous operations
7. Add proper null handling and validation
8. Design for dependency injection and testability

**Key architectural insights:**
- In-memory storage provides simplicity and performance for development
- Thread safety is essential for concurrent proxy operations
- Dictionary with GUID keys provides efficient lookups
- LINQ enables flexible querying without complex query languages
- Async interface design allows future persistence implementations
