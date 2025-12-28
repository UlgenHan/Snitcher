# IRepository

## Overview

IRepository is a generic repository interface that defines a contract for data access operations in the Snitcher application. It provides a standardized abstraction for CRUD (Create, Read, Update, Delete) operations and common queries, enabling the application to work with different storage implementations without being tightly coupled to specific data access technologies.

**Why it exists**: To provide a clean abstraction layer between business logic and data access, enabling testability, maintainability, and the ability to swap storage implementations (Entity Framework, Dapper, in-memory, etc.) without changing business logic.

**Problem it solves**: Without this abstraction, business logic would be tightly coupled to specific data access technologies, making testing difficult and code hard to maintain. It eliminates code duplication across different repository implementations.

**What would break if removed**: All repository implementations would lose their contract, breaking dependency injection configuration. Business services would need to directly depend on concrete implementations, destroying testability and flexibility.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0

**Libraries**: None (pure interface definition)

**UI frameworks**: N/A (infrastructure layer)

**Persistence / communication technologies**: Generic abstraction supporting Entity Framework, Dapper, in-memory providers

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, async/await support

**Version hints**: Uses modern C# features including generic constraints, nullable reference types, and async patterns

## Architectural Role

**Layer**: Domain Layer (Core) - Infrastructure Abstraction

**Responsibility boundaries**:
- MUST define contract for data access operations
- MUST be technology-agnostic
- MUST support async operations
- MUST NOT contain implementation details
- MUST NOT depend on external frameworks

**What it MUST do**:
- Define CRUD operation contracts
- Provide query method signatures
- Support cancellation tokens for async operations
- Enable generic repository pattern

**What it MUST NOT do**:
- Implement actual data access logic
- Depend on specific data access technologies
- Handle business logic or validation
- Access external resources directly

**Dependencies (incoming)**: Repository implementations, Service layer

**Dependencies (outgoing**: IEntity, IEntity<TId> interfaces

## Execution Flow

**Where execution starts**: IRepository is not executed directly - it's a contract that concrete implementations fulfill.

**How control reaches this component**:
1. Service layer receives business operation request
2. Service layer calls repository interface methods
3. Dependency injection provides concrete implementation
4. Concrete repository performs actual data access

**Call sequence (step-by-step)**:
1. Service layer calls IRepository method (e.g., GetByIdAsync)
2. DI container resolves concrete implementation
3. Concrete repository executes data access operation
4. Result is returned through interface contract
5. Service layer processes result

**Synchronous vs asynchronous behavior**: All operations are asynchronous by design

**Threading / dispatcher / event loop notes**: Thread-safe through async operations, concrete implementations handle thread safety

**Lifecycle**: Interface exists for entire application lifetime, implementations are typically scoped services

## Public API / Surface Area

**Generic interface**:
- `IRepository<TEntity, TId>`: Main repository interface with typed ID
- `IRepository<TEntity>`: Simplified interface for Guid-based entities

**CRUD Operations**:
- `Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)`: Retrieve entity by ID
- `Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)`: Retrieve all entities
- `Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)`: Add new entity
- `Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)`: Update existing entity
- `Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)`: Delete entity by ID

**Query Operations**:
- `Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)`: Check entity existence
- `Task<int> CountAsync(CancellationToken cancellationToken = default)`: Get entity count

**Expected input/output**:
- Input: Entity objects, IDs, and cancellation tokens
- Output: Entity objects, collections, boolean results, or counts

**Side effects**: None in interface - side effects occur in implementations

**Error behavior**: Interface doesn't define error handling - implementations determine exception types and handling

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Generic constraints (lines 10-11)**:
```csharp
public interface IRepository<TEntity, TId> where TEntity : IEntity<TId>
```
- Constrains TEntity to implement IEntity<TId>
- Ensures type safety between entity and ID types
- Enables compile-time type checking

**GetByIdAsync method (lines 18-19)**:
```csharp
Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
```
- Returns nullable entity (may not exist)
- Includes cancellation token for async operation cancellation
- Uses Task for async operation

**GetAllAsync method (lines 25-26)**:
```csharp
Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
```
- Returns read-only collection (prevents modification)
- Includes cancellation token support
- Returns all entities in repository

**AddAsync method (lines 33-34)**:
```csharp
Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
```
- Takes entity to add as parameter
- Returns the added entity (potentially with generated values)
- Non-nullable return type expects successful operation

**UpdateAsync method (lines 41-42)**:
```csharp
Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
```
- Takes entity to update as parameter
- Returns the updated entity
- Assumes entity exists in repository

**DeleteAsync method (lines 49-50)**:
```csharp
Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);
```
- Takes entity ID for deletion
- Returns boolean indicating success
- Allows for graceful handling of non-existent entities

**ExistsAsync method (lines 57-58)**:
```csharp
Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
```
- Checks if entity exists without retrieving full entity
- Returns boolean result
- More efficient than GetByIdAsync for existence checks

**CountAsync method (lines 64-65)**:
```csharp
Task<int> CountAsync(CancellationToken cancellationToken = default);
```
- Returns total count of entities
- Useful for pagination and analytics
- Includes cancellation token support

**Simplified interface (lines 71-73)**:
```csharp
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : IEntity
```
- Inherits from main interface with Guid ID type
- Simplifies usage for most common entity types
- Constrains to IEntity (Guid-based entities)

**Algorithms used**: No algorithms - pure interface definition

**Conditional logic**: None - interface defines contracts only

**State transitions**: Not applicable - interface has no state

**Important invariants**: 
- All methods are async
- All methods support cancellation
- Return types are consistent with async patterns
- Generic constraints ensure type safety

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Repository Pattern**: Abstracts data access logic
- **Generic Repository Pattern**: Provides type-safe operations for any entity type
- **Interface Segregation Principle**: Focused interface with specific responsibilities

**Architectural patterns**:
- **Clean Architecture**: Interface in domain layer, implementation in infrastructure
- **Dependency Inversion Principle**: High-level modules depend on abstraction
- **Command Query Separation**: Clear separation between read and write operations

**Why these patterns were chosen (inferred)**:
- Repository pattern enables testability and loose coupling
- Generic repository reduces code duplication
- Interface segregation keeps contract focused
- Clean architecture maintains dependency direction

**Trade-offs**:
- Generic repository vs specific repositories: More flexible but potentially less expressive
- Interface abstraction vs direct dependency: More testable but adds complexity
- Async-only vs sync/async: Better performance but requires async throughout

**Anti-patterns avoided or possibly introduced**:
- Avoided: Concrete dependency coupling
- Avoided: Synchronous operations in async world
- Possible risk: Generic repository becoming too generic

## Binding / Wiring / Configuration

**Dependency injection**: Interface registered with concrete implementations in DI container

**Data binding (if UI)**: N/A - infrastructure layer

**Configuration sources**: DI configuration in application startup

**Runtime wiring**: Dependency injection container resolves implementations

**Registration points**: ServiceCollectionExtensions in infrastructure layer

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
public class ProjectService
{
    private readonly IRepository<Project> _projectRepository;
    
    public ProjectService(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }
    
    public async Task<Project?> GetProjectAsync(Guid id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }
}
```

**Realistic example**:
```csharp
public class WorkspaceService
{
    private readonly IRepository<Workspace, Guid> _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public WorkspaceService(
        IRepository<Workspace, Guid> workspaceRepository,
        IUnitOfWork unitOfWork)
    {
        _workspaceRepository = workspaceRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Workspace> CreateWorkspaceAsync(string name, string path)
    {
        var workspace = new Workspace { Name = name, Path = path };
        
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var created = await _workspaceRepository.AddAsync(workspace);
            await _unitOfWork.CommitAsync();
            return created;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
    
    public async Task<bool> DeleteWorkspaceAsync(Guid id)
    {
        var exists = await _workspaceRepository.ExistsAsync(id);
        if (!exists) return false;
        
        return await _workspaceRepository.DeleteAsync(id);
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Assuming synchronous operations
var project = _projectRepository.GetByIdAsync(id).Result; // Blocking call, can cause deadlocks

// WRONG: Not handling null returns
var project = await _projectRepository.GetByIdAsync(id);
project.Name = "Updated"; // NullReferenceException if project doesn't exist

// WRONG: Not using cancellation tokens properly
public async Task<IEnumerable<Project>> GetAllProjects()
{
    var cts = new CancellationTokenSource();
    return await _projectRepository.GetAllAsync(cts.Token); // Token never cancelled
}

// WRONG: Mixing interface types
IRepository<Project, Guid> repo = GetRepository(); // OK
IRepository<Project> repo2 = repo; // OK - inheritance
IRepository<Workspace> workspaceRepo = repo; // WRONG - type mismatch
```

**How to test this in isolation**:
```csharp
// Mock implementation for testing
public class MockRepository<T> : IRepository<T> where T : class, IEntity, new()
{
    private readonly Dictionary<Guid, T> _storage = new();
    
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _storage.TryGetValue(id, out var entity) ? entity : null;
    }
    
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _storage.Values.ToList().AsReadOnly();
    }
    
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _storage[entity.Id] = entity;
        return entity;
    }
    
    // ... other methods
}

[Test]
public async Task Service_ShouldUseRepositoryCorrectly()
{
    // Arrange
    var mockRepo = new MockRepository<Project>();
    var service = new ProjectService(mockRepo);
    var project = new Project { Name = "Test", Path = @"C:\Test" };
    await mockRepo.AddAsync(project);
    
    // Act
    var result = await service.GetProjectAsync(project.Id);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Name, Is.EqualTo("Test"));
}
```

**How to mock or replace it**:
```csharp
// Using Moq framework
var mockRepository = new Mock<IRepository<Project>>();
mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync((Guid id) => new Project { Id = id, Name = "Mocked" });

// Using manual mock
public class TestRepository : IRepository<Project>
{
    private readonly List<Project> _projects = new();
    
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _projects.FirstOrDefault(p => p.Id == id);
    }
    
    // ... implement other methods for testing
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new method signatures to interface for additional operations
2. Consider adding new generic interfaces for specific patterns
3. Add overloads for existing methods with different parameters
4. Ensure all new methods follow async and cancellation patterns

**Where NOT to add logic**:
- Don't add implementation details to interface
- Don't add business logic or validation
- Don't add technology-specific concerns
- Don't add synchronous method overloads

**Safe extension points**:
- New method signatures for additional operations
- New generic interfaces for specialized repositories
- Overloads with additional parameters
- Covariant/contravariant interface definitions

**Common mistakes**:
- Adding too many methods (interface bloat)
- Mixing synchronous and asynchronous patterns
- Adding implementation-specific concerns
- Forgetting cancellation token parameters

**Refactoring warnings**:
- Removing methods breaks all implementations
- Changing method signatures breaks calling code
- Adding generic constraints affects usage
- Changing return types affects dependent code

## Failure Modes & Debugging

**Common runtime errors**:
- **InvalidOperationException**: From implementations when operations fail
- **ArgumentNullException**: When null parameters passed to implementations
- **KeyNotFoundException**: When implementations use dictionaries internally
- **TimeoutException**: From database operations in implementations

**Null/reference risks**:
- GetByIdAsync can return null (handled by nullable return type)
- Entity parameters can be null (implementations should validate)
- Cancellation tokens can be null (default parameter handles)

**Performance risks**:
- GetAllAsync can return large collections
- CountAsync may be expensive on large datasets
- Async overhead for simple operations

**Logging points**:
- Interface has no logging - implementations should log
- Method entry/exit should be logged in implementations
- Performance metrics should be collected in implementations

**How to debug step-by-step**:
1. Set breakpoints in repository implementation methods
2. Monitor method parameters and return values
3. Check async operation completion
4. Verify cancellation token behavior
5. Watch for exceptions in implementations

## Cross-References

**Related classes**:
- IEntity, IEntity<TId> interfaces (generic constraints)
- EfRepository (concrete implementation)
- IUnitOfWork (transaction management)
- Repository implementations for specific entities

**Upstream callers**:
- Service layer classes use repository interfaces
- Application layer orchestrates through services
- Test classes mock repository interfaces

**Downstream dependencies**:
- Concrete repository implementations
- Database contexts and data access technologies
- Entity Framework or other ORM implementations

**Documents that should be read before/after**:
- Read: IEntity, IEntity<TId> documentation (constraints)
- Read: EfRepository documentation (implementation)
- Read: IUnitOfWork documentation (transactions)
- Read: Specific repository interface documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Repository pattern abstraction
- Generic repository pattern
- Async-first API design
- Cancellation token support
- Interface segregation principles

**What is project-specific**:
- Specific method signatures and parameter types
- Generic constraint usage
- Cancellation token parameter patterns
- Return type choices (IReadOnlyList vs List)

**How to recreate this pattern from scratch elsewhere**:
1. Define generic repository interface with constraints
2. Include CRUD operation signatures
3. Add query method signatures
4. Support async operations throughout
5. Include cancellation token parameters
6. Provide simplified interface for common cases
7. Follow dependency inversion principles

**Key insights for implementation**:
- Always use async operations for I/O bound work
- Include cancellation tokens for all async operations
- Use read-only collections for query results
- Leverage generic constraints for type safety
- Keep interfaces focused and cohesive
- Consider both generic and specific repository needs
