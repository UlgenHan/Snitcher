# IRepository.cs

## Overview

`IRepository.cs` defines the generic repository interface that provides a contract for data access operations in the Snitcher application's clean architecture. This interface establishes a standardized API for CRUD operations and common queries, enabling the abstraction of data access implementation details and supporting different storage mechanisms (Entity Framework Core, Dapper, in-memory, etc.).

**Why it exists**: To provide a consistent abstraction layer for data access, enable testability through interface-based design, support multiple storage implementations, and establish clear boundaries between the domain and infrastructure layers.

**What problem it solves**: Eliminates direct dependencies on specific data access technologies, enables unit testing with mock repositories, provides a consistent API across all entity types, and facilitates switching between storage implementations without affecting business logic.

**What would break if removed**: All data access would become tightly coupled to specific implementations, unit testing would become difficult, and the clean architecture separation between domain and infrastructure would be violated.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure interface definition)

**Persistence**: Framework-agnostic abstraction layer

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Async/await support, generic type system

## Architectural Role

**Layer**: Domain Layer (Interface Definition)

**Responsibility Boundaries**:
- MUST define contracts for data access operations
- MUST remain framework-agnostic
- MUST support async operations
- MUST NOT contain implementation details
- MUST NOT depend on infrastructure concerns

**What it MUST do**:
- Define standard CRUD operations
- Provide query method signatures
- Support cancellation tokens
- Enable generic usage across entity types
- Support both generic and simplified interfaces

**What it MUST NOT do**:
- Implement actual data access logic
- Depend on specific frameworks like EF Core
- Handle connection management
- Include business logic

**Dependencies (Incoming)**: Repository implementations, service layer

**Dependencies (Outgoing**: Domain entity interfaces (IEntity, IEntity<TId>)

## Execution Flow

**Where execution starts**: Interface definition used at compile time for type checking and method resolution

**How control reaches this component**:
1. Service layer depends on IRepository interface
2. Dependency injection container provides concrete implementation
3. Service calls interface methods
4. Concrete repository executes actual operations
5. Results returned through interface contract

**Method Resolution Flow**:
1. Compile-time type checking against interface
2. Runtime method dispatch to concrete implementation
3. Async execution of data operations
4. Result marshaling back through interface

**Generic Constraint Flow**:
1. TEntity constrained to implement IEntity<TId>
2. Type safety ensured at compile time
3. Implementation can rely on interface contracts
4. Generic methods work with any compliant entity

**Synchronous vs asynchronous behavior**: All methods are async to prevent blocking and support modern async patterns

**Threading/Dispatcher notes**: No threading concerns - interface definition only

**Lifecycle**: Interface exists for application duration, implementations follow service lifetime patterns

## Public API / Surface Area

**Primary Interface**:
```csharp
public interface IRepository<TEntity, TId> where TEntity : IEntity<TId>
```

**Query Methods**:
- `Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)` - Get single entity
- `Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)` - Get all entities

**Modification Methods**:
- `Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)` - Add new entity
- `Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)` - Update existing entity
- `Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)` - Delete entity by ID

**Utility Methods**:
- `Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)` - Check existence
- `Task<int> CountAsync(CancellationToken cancellationToken = default)` - Get entity count

**Simplified Interface**:
```csharp
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IEntity
```

**Expected Input/Output**: Methods take entity instances or IDs and return entities, collections, or boolean results. All operations are async.

**Side Effects**: Interface defines contract for side effects in implementations (database modifications).

**Error Behavior**: Interface doesn't define error handling - implementations determine exception handling strategy.

## Internal Logic Breakdown

**Interface Design Pattern** (lines 10-65):
```csharp
public interface IRepository<TEntity, TId> where TEntity : IEntity<TId>
{
    // Method signatures with async pattern
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    // ... other methods
}
```
- Generic interface supports any entity type
- Constraints ensure type safety
- All methods async for non-blocking operations
- CancellationToken support for operation cancellation

**Method Signature Patterns**:
- **Query methods**: Return Task<T> or Task<IReadOnlyList<T>>
- **Modification methods**: Return Task<T> for entity operations, Task<bool> for delete operations
- **Utility methods**: Return Task<bool> or Task<int> for status information
- **Cancellation support**: All methods accept optional CancellationToken

**Simplified Interface Pattern** (lines 67-73):
```csharp
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IEntity
{
}
```
- Provides convenience interface for GUID-based entities
- Reduces type parameter verbosity for common case
- Maintains full functionality through inheritance

**Generic Constraints**:
- `where TEntity : IEntity<TId>` ensures entities have identity
- Simplified interface constrains to IEntity (GUID-based)
- Enables compile-time type checking and IntelliSense

**Return Type Patterns**:
- Single entities: `Task<TEntity?>` (nullable for not-found cases)
- Collections: `Task<IReadOnlyList<TEntity>>` (immutable collection)
- Status operations: `Task<bool>` or `Task<int>` (simple results)

## Patterns & Principles Used

**Repository Pattern**: Abstracts data access behind interface

**Generic Programming**: Type-safe operations across entity types

**Async/Await Pattern**: Non-blocking data operations

**Interface Segregation**: Clean separation of contract and implementation

**Dependency Inversion**: High-level modules depend on abstraction

**CancellationToken Pattern**: Support for operation cancellation

**Why these patterns were chosen**:
- Repository pattern for data access abstraction
- Generics for type safety and code reuse
- Async for responsive application behavior
- Interface segregation for clean architecture
- Dependency inversion for testability
- Cancellation for responsive UI and operations

**Trade-offs**:
- Generic complexity may impact readability
- Interface adds indirection layer
- Async pattern adds complexity to error handling
- Simplified interface may cause confusion

**Anti-patterns avoided**:
- No concrete dependencies in interface
- No synchronous blocking methods
- No framework-specific types
- No implementation details leaked

## Binding / Wiring / Configuration

**Data Binding**: Not applicable (interface definition)

**Configuration Sources**: No external configuration needed

**Runtime Wiring**:
- Implemented by concrete repository classes
- Registered in dependency injection container
- Resolved by service layer through constructor injection

**Registration Points**:
- Repository implementations registered in DI container
- Service layer depends on interface types
- Application startup configures implementations

## Example Usage

**Minimal Example**:
```csharp
// Service depends on interface
public class WorkspaceService
{
    private readonly IRepository<Workspace, Guid> _repository;
    
    public WorkspaceService(IRepository<Workspace, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<Workspace?> GetWorkspaceAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

**Realistic Example**:
```csharp
// Using simplified interface for GUID entities
public class ProjectService
{
    private readonly IRepository<Project> _repository;
    
    public ProjectService(IRepository<Project> repository)
    {
        _repository = repository;
    }
    
    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await _repository.GetAllAsync();
    }
    
    public async Task<Project> CreateProjectAsync(Project project)
    {
        return await _repository.AddAsync(project);
    }
}
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't use concrete implementation directly
public class Service
{
    private readonly EfRepository<Project> _repository; // Tight coupling
}

// BAD - Don't forget async/await
var project = repository.GetByIdAsync(id); // Returns Task, not Project
```

**How to test in isolation**:
```csharp
// Mock repository for testing
var mockRepository = new Mock<IRepository<Project, Guid>>();
mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync((Guid id) => new Project { Id = id, Name = "Test" });

var service = new ProjectService(mockRepository.Object);
var result = await service.GetProjectAsync(Guid.NewGuid());
Assert.NotNull(result);
```

**How to mock or replace**:
- Use mocking frameworks (Moq, NSubstitute)
- Create test implementations of interface
- Use in-memory collections for simple testing
- Implement fake repositories for integration testing

## Extension & Modification Guide

**How to add new repository operations**:
1. Add method signatures to interface
2. Update all implementations
3. Consider impact on existing code
4. Maintain async pattern and cancellation support

**Where NOT to add logic**:
- Don't add implementation details to interface
- Don't add framework-specific types
- Don't add business logic methods

**Safe extension points**:
- Additional query methods for specific use cases
- Batch operations for performance
- Specification pattern support
- Custom query methods with parameters

**Common mistakes**:
- Adding synchronous methods to async interface
- Forgetting CancellationToken parameters
- Making methods too specific to one use case
- Not updating all implementations

**Refactoring warnings**:
- Adding methods breaks existing implementations
- Changing method signatures affects all callers
- Consider backward compatibility
- Test all implementations after changes

## Failure Modes & Debugging

**Common runtime errors**:
- InvalidOperationException when implementation not registered
- NotImplementedException when methods not implemented
- TimeoutException when operations take too long

**Null/reference risks**:
- GetByIdAsync can return null for not-found entities
- Repository dependency may be null if DI fails
- Entity parameters may be null in implementations

**Performance risks**:
- GetAllAsync can return large collections
- No built-in pagination support
- Lack of query optimization hints

**Logging points**: None in interface - implementations handle logging

**How to debug step-by-step**:
1. Verify interface implementation is registered
2. Check that dependency injection resolves correctly
3. Monitor async method execution
4. Test cancellation behavior
5. Verify return types match expectations

## Cross-References

**Related classes**:
- `IEntity<TId>` (generic constraint)
- `IEntity` (simplified constraint)
- Concrete repository implementations
- Service layer classes

**Upstream callers**:
- Service layer (business logic)
- Application layer (DTO mapping)
- Other repositories (for relationships)

**Downstream dependencies**:
- Domain entity interfaces
- Concrete repository implementations

**Documents to read before/after**:
- Before: Entity interface definitions
- After: Concrete repository implementations
- After: Service layer documentation

## Knowledge Transfer Notes

**Reusable concepts**:
- Generic repository pattern
- Async data access interface
- Cancellation token support
- Interface segregation for testability
- Simplified interface pattern for common cases

**Project-specific elements**:
- Snitcher entity type constraints
- GUID-based identity simplification
- Clean architecture separation
- Dependency injection integration

**How to recreate pattern elsewhere**:
1. Define generic interface with entity constraints
2. Include standard CRUD operations
3. Make all methods async with cancellation support
4. Provide simplified interface for common cases
5. Use IReadOnlyList for query results
6. Support nullable returns for not-found cases

**Key insights**:
- Always use async for data access operations
- Include CancellationToken for responsive operations
- Use generic constraints for type safety
- Provide both generic and simplified interfaces
- Return IReadOnlyList for immutable query results
- Use nullable types for optional results
- Keep interface focused on data access only
- Avoid business logic in repository contracts
