# EfRepository

## Overview

EfRepository is a generic Entity Framework Core implementation of the Repository pattern that provides standard CRUD (Create, Read, Update, Delete) operations for any entity type in the Snitcher application. It serves as the foundational data access abstraction, enabling type-safe database operations while maintaining separation between business logic and data access concerns. The repository comes in both generic and specialized forms to support different identifier types.

This repository exists to provide a consistent, testable, and maintainable way to access data while abstracting away the complexity of Entity Framework Core operations. Without EfRepository, the service layer would need to work directly with DbContext, leading to code duplication, scattered data access logic, and difficult testing scenarios. The repository centralizes data access patterns, enables easy mocking for unit tests, and provides a clean API for database operations.

If EfRepository were removed, the system would lose:
- Standardized data access patterns across all entities
- Type-safe generic repository functionality
- Testable data access layer through interface abstraction
- Centralized CRUD operation implementation
- Consistent error handling and validation
- Foundation for specialized repository implementations

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- Entity Framework Core 8.0
- Microsoft.EntityFrameworkCore
- .NET 8.0 Base Class Library

**Libraries:**
- Microsoft.EntityFrameworkCore for ORM functionality
- System.Linq.Expressions for expression tree support
- System.Threading.Tasks for async operations
- Snitcher.Core.Interfaces for repository contracts

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- Entity Framework Core for ORM operations
- SQLite provider (inherited from DbContext)
- Async/await patterns for non-blocking operations

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- Entity Framework Core tools for development

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires Entity Framework Core runtime
- Async operation support throughout
- Generic type system support

**Version Hints:**
- Entity Framework Core 8.0 async patterns
- Modern generic constraint usage
- Expression tree support for flexible querying

## Architectural Role

**Layer:** Infrastructure Layer (Clean Architecture - Repository)

**Responsibility Boundaries:**
- MUST provide standard CRUD operations for entities
- MUST maintain type safety through generic constraints
- MUST support both synchronous and asynchronous operations
- MUST NOT contain business logic or validation rules
- MUST NOT depend on service layer or UI components

**What it MUST do:**
- Implement IRepository interface contract
- Provide type-safe database operations
- Handle entity lifecycle management (Add, Update, Delete)
- Support querying with predicates and expressions
- Manage DbSet operations through Entity Framework Core

**What it MUST NOT do:**
- Implement business validation or rules
- Handle complex domain operations
- Perform file system or external service operations
- Contain UI-specific logic or concerns
- Implement caching or performance optimizations beyond basic operations

**Dependencies (Incoming):**
- Service layer for business operations requiring data access
- Specialized repositories inheriting from this base
- Unit of Work implementations for transaction coordination
- Testing frameworks for mocking and unit testing

**Dependencies (Outgoing):**
- SnitcherDbContext for Entity Framework Core operations
- IRepository interfaces for contract implementation
- Domain entities through generic type constraints
- Entity Framework Core infrastructure for ORM operations

## Execution Flow

**Where execution starts:**
- Service layer calls repository methods for data operations
- Specialized repositories inherit and extend functionality
- Unit of Work coordinates multiple repository operations

**How control reaches this component:**
1. Service layer needs data → DI container injects repository
2. Repository method called → EfRepository executes operation
3. Entity Framework Core operations → Database operations performed
4. Results returned → Service layer receives processed data

**Call Sequence (Entity Creation):**
1. Service layer creates entity instance
2. Repository.AddAsync(entity) called
3. EfRepository validates entity and calls _dbSet.AddAsync()
4. Entity tracked by DbContext in Added state
5. Entity returned to caller for further operations

**Call Sequence (Query Operation):**
1. Service layer calls repository query method
2. EfRepository builds LINQ query with predicates
3. Entity Framework Core translates to SQL
4. Database executes query and returns results
5. Results materialized as entity objects

**Synchronous vs Asynchronous Behavior:**
- All database operations are asynchronous (async/await pattern)
- Method signatures support cancellation tokens
- No synchronous database operations exposed

**Threading/Dispatcher Notes:**
- Repository instances are thread-safe for read operations
- Write operations should be serialized through proper scoping
- Depends on DbContext thread safety (not thread-safe)
- Designed for per-operation or per-unit-of-work usage

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Instantiated by DI container with scoped lifetime
2. **Usage:** Multiple operations within single unit of work
3. **Entity Tracking:** Entities tracked through DbContext lifecycle
4. **Disposal:** Automatically disposed with DbContext by DI container

## Public API / Surface Area

**Generic Classes:**
- `EfRepository<TEntity, TId>`: Generic repository for entities with specific ID types
- `EfRepository<TEntity>`: Simplified repository for entities with Guid IDs

**Constructors:**
- `EfRepository(SnitcherDbContext context)`: Initializes repository with database context

**CRUD Methods:**
- `Task<TEntity?> GetByIdAsync(TId id, CancellationToken)`: Retrieves entity by ID
- `Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken)`: Retrieves all entities
- `Task<TEntity> AddAsync(TEntity entity, CancellationToken)`: Adds new entity
- `Task<TEntity> UpdateAsync(TEntity entity, CancellationToken)`: Updates existing entity
- `Task<bool> DeleteAsync(TId id, CancellationToken)`: Deletes entity by ID

**Query Methods:**
- `Task<bool> ExistsAsync(TId id, CancellationToken)`: Checks entity existence
- `Task<int> CountAsync(CancellationToken)`: Gets entity count
- `Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>>, CancellationToken)`: Finds entities by predicate
- `Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>, CancellationToken)`: Gets first matching entity

**Expected Input/Output:**
- **Input:** Entity instances, ID values, LINQ expressions, cancellation tokens
- **Output:** Entity objects, collections, boolean results, counts

**Side Effects:**
- Entity tracking through DbContext change tracker
- Database state modifications through Entity Framework operations
- Context state changes affecting subsequent operations

**Error Behavior:**
- ArgumentNullException for null entity parameters
- Entity Framework Core exceptions for database errors
- OperationCanceledException for cancellation requests
- InvalidOperationException for invalid entity states

## Internal Logic Breakdown

**Constructor Logic (Lines 25-29):**
```csharp
public EfRepository(SnitcherDbContext context)
{
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _dbSet = context.Set<TEntity>();
}
```
- Validates context dependency injection
- Initializes DbSet for entity type operations
- Stores context reference for all operations
- Throws ArgumentNullException for null context (fail-fast)

**GetByIdAsync Logic (Lines 37-40):**
```csharp
public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
{
    return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
}
```
- Uses Entity Framework Core FindAsync for optimal performance
- Supports primary key lookup with change tracker integration
- Wraps ID in object array for Entity Framework compatibility
- Supports cancellation for long-running operations

**AddAsync Logic (Lines 58-65):**
```csharp
public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
{
    if (entity == null)
        throw new ArgumentNullException(nameof(entity));

    var entry = await _dbSet.AddAsync(entity, cancellationToken);
    return entry.Entity;
}
```
- Validates entity parameter before operation
- Uses DbSet.AddAsync for proper async handling
- Returns tracked entity from EntityEntry
- Supports cancellation for async operations

**UpdateAsync Logic (Lines 73-80):**
```csharp
public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
{
    if (entity == null)
        throw new ArgumentNullException(nameof(entity));

    var entry = _dbSet.Update(entity);
    return Task.FromResult(entry.Entity);
}
```
- Validates entity parameter before operation
- Uses DbSet.Update for synchronous operation (no async needed)
- Returns updated entity from EntityEntry
- Wrapped in Task for interface consistency

**DeleteAsync Logic (Lines 88-96):**
```csharp
public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
{
    var entity = await GetByIdAsync(id, cancellationToken);
    if (entity == null)
        return false;

    _dbSet.Remove(entity);
    return true;
}
```
- Retrieves entity first to ensure existence
- Returns false if entity not found (soft delete pattern)
- Uses DbSet.Remove for deletion tracking
- Returns true for successful deletion

**FindAsync Logic (Lines 125-130):**
```csharp
public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
    System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, 
    CancellationToken cancellationToken = default)
{
    return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
}
```
- Uses LINQ Where clause for predicate filtering
- Supports complex expression trees for flexible querying
- Returns IReadOnlyList for immutable result contract
- Materializes results to list for immediate execution

**Generic Constraint Logic:**
```csharp
where TEntity : class, IEntity<TId>
where TId : IEquatable<TId>
```
- Ensures TEntity is a reference type implementing IEntity
- Requires TId to support equality comparison
- Enables type-safe ID operations throughout repository
- Provides compile-time guarantees for proper usage

## Patterns & Principles Used

**Design Patterns:**
- **Repository Pattern:** Abstracts data access behind interface
- **Generic Programming:** Type-safe operations across entity types
- **Template Method Pattern:** Virtual methods allow specialization
- **Strategy Pattern:** Different ID types supported through generics

**Architectural Patterns:**
- **Clean Architecture:** Infrastructure layer with clear separation
- **Dependency Inversion:** Depends on abstractions, not concretions
- **SOLID Principles:** Single responsibility, open/closed, dependency inversion
- **CQRS Foundation:** Supports command/query separation

**Why These Patterns Were Chosen:**
- **Repository Pattern:** Provides clean abstraction over Entity Framework complexity
- **Generic Programming:** Reduces code duplication while maintaining type safety
- **Template Method:** Allows specialization without breaking base functionality
- **Clean Architecture:** Maintains testability and separation of concerns

**Trade-offs:**
- **Pros:** Type safety, code reuse, testability, clean abstraction
- **Cons:** Generic complexity, Entity Framework dependency
- **Decision:** Benefits of abstraction and type safety outweigh complexity

**Anti-patterns Avoided:**
- **Repository Overkill:** Focused on essential CRUD operations only
- **God Repository:** Split into specialized repositories for complex operations
- **Anemic Repository:** Includes meaningful query operations beyond basic CRUD

## Binding / Wiring / Configuration

**Dependency Injection:**
- Registered as scoped service in ServiceCollectionExtensions
- Generic registration supports any entity type
- Scoped lifetime aligns with DbContext lifecycle

**Data Binding:**
- DbSet automatically bound to entity database tables
- Generic type parameters ensure correct table mapping
- Entity Framework Core handles all object-relational mapping

**Configuration Sources:**
- Entity Framework Core configuration in DbContext
- Generic constraints enforce type safety at compile time
- Interface contracts ensure consistent API across implementations

**Runtime Wiring:**
- DbSet resolved through DbContext.Set<TEntity>()
- Entity tracking managed through DbContext change tracker
- Query execution through Entity Framework Core query pipeline

**Registration Points:**
- ServiceCollectionExtensions.AddRepositoryLayer() for production
- Generic registration: `services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))`
- Specialized registration: `services.AddScoped<IWorkspaceRepository, WorkspaceRepository>()`

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Register in DI container
services.AddRepositoryLayer();

// Use in service
public class ProjectService
{
    private readonly IRepository<Project> _projectRepository;
    
    public ProjectService(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }
    
    public async Task<Project> CreateProjectAsync(string name, string path)
    {
        var project = new Project { Name = name, Path = path };
        return await _projectRepository.AddAsync(project);
    }
}
```

**Realistic Example:**
```csharp
public class WorkspaceManagementService
{
    private readonly IRepository<Workspace> _workspaceRepository;
    private readonly IRepository<Project> _projectRepository;
    
    public async Task<Workspace> CreateWorkspaceWithProjectsAsync(
        CreateWorkspaceDto dto)
    {
        // Create workspace
        var workspace = new Workspace
        {
            Name = dto.Name,
            Path = dto.Path,
            IsDefault = dto.IsDefault
        };
        
        var createdWorkspace = await _workspaceRepository.AddAsync(workspace);
        
        // Add projects
        foreach (var projectDto in dto.Projects)
        {
            var project = new ProjectEntity
            {
                Name = projectDto.Name,
                Path = projectDto.Path,
                WorkspaceId = createdWorkspace.Id
            };
            await _projectRepository.AddAsync(project);
        }
        
        return createdWorkspace;
    }
    
    public async Task<List<Workspace>> SearchWorkspacesAsync(string searchTerm)
    {
        return (await _workspaceRepository.FindAsync(w => 
            w.Name.Contains(searchTerm) || 
            (w.Description?.Contains(searchTerm) ?? false)))
            .ToList();
    }
    
    public async Task<bool> DeleteWorkspaceAsync(Guid id)
    {
        // Check if workspace has projects
        var hasProjects = await _projectRepository.ExistsAsync(p => p.WorkspaceId == id);
        if (hasProjects)
            throw new InvalidOperationException("Cannot delete workspace with projects");
        
        return await _workspaceRepository.DeleteAsync(id);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Using repository after disposal
public class BadService
{
    public async Task BadOperation()
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetService<IRepository<Project>>();
        
        await repository.AddAsync(new Project()); // OK
        
        scope.Dispose(); // Repository disposed here
        
        await repository.GetAllAsync(); // WRONG - ObjectDisposedException
    }
}

// WRONG: Sharing repository instances across threads
public async Task BadConcurrentOperation(IRepository<Project> repository)
{
    var task1 = repository.AddAsync(new Project { Name = "1" });
    var task2 = repository.AddAsync(new Project { Name = "2" });
    
    await Task.WhenAll(task1, task2); // WRONG - DbContext not thread-safe
}

// WRONG: Complex business logic in repository
public class BadRepository : EfRepository<Project>
{
    public async Task<bool> ValidateProjectAsync(Project project)
    {
        // WRONG - Business logic belongs in service layer
        return !string.IsNullOrWhiteSpace(project.Name) && 
               Directory.Exists(project.Path);
    }
}
```

**How to Test in Isolation:**
```csharp
[Test]
public async Task Repository_ShouldPerformCrudOperations()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SnitcherDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;
    
    using var context = new SnitcherDbContext(options);
    var repository = new EfRepository<Workspace>(context);
    var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    
    // Act - Create
    var created = await repository.AddAsync(workspace);
    
    // Act - Read
    var retrieved = await repository.GetByIdAsync(created.Id);
    
    // Act - Update
    retrieved.Description = "Updated";
    var updated = await repository.UpdateAsync(retrieved);
    
    // Act - Delete
    var deleted = await repository.DeleteAsync(created.Id);
    
    // Assert
    Assert.That(created, Is.Not.Null);
    Assert.That(retrieved, Is.Not.Null);
    Assert.That(updated.Description, Is.EqualTo("Updated"));
    Assert.That(deleted, Is.True);
    Assert.That(await repository.ExistsAsync(created.Id), Is.False);
}

[Test]
public async Task Repository_ShouldSupportQuerying()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SnitcherDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;
    
    using var context = new SnitcherDbContext(options);
    var repository = new EfRepository<Workspace>(context);
    
    context.Workspaces.AddRange(
        new Workspace { Name = "Active", Path = @"C:\Active" },
        new Workspace { Name = "Inactive", Path = @"C:\Inactive" },
        new Workspace { Name = "Active2", Path = @"C:\Active2" }
    );
    await context.SaveChangesAsync();
    
    // Act
    var activeWorkspaces = await repository.FindAsync(w => w.Name.Contains("Active"));
    var firstWorkspace = await repository.FirstOrDefaultAsync(w => w.Name.StartsWith("Active"));
    var allCount = await repository.CountAsync();
    
    // Assert
    Assert.That(activeWorkspaces.Count, Is.EqualTo(2));
    Assert.That(firstWorkspace, Is.Not.Null);
    Assert.That(allCount, Is.EqualTo(3));
}
```

**How to Mock or Replace:**
```csharp
// Mock repository for testing services
public class MockRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly Dictionary<Guid, T> _items = new();
    
    public async Task<T> AddAsync(T entity)
    {
        _items[entity.Id] = entity;
        return entity;
    }
    
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
    
    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return _items.Values.ToList();
    }
    
    public async Task<T> UpdateAsync(T entity)
    {
        _items[entity.Id] = entity;
        return entity;
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        return _items.Remove(id);
    }
    
    // ... other methods
}

// Usage in test
[Test]
public async Task Service_ShouldWorkWithMockRepository()
{
    // Arrange
    var mockRepository = new MockRepository<Workspace>();
    var service = new WorkspaceService(mockRepository);
    
    // Act
    var workspace = await service.CreateWorkspaceAsync("Test", @"C:\Test");
    
    // Assert
    Assert.That(workspace, Is.Not.Null);
    Assert.That(await mockRepository.ExistsAsync(workspace.Id), Is.True);
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **Specialized Repositories:** Inherit from EfRepository for domain-specific operations
2. **Additional Query Methods:** Add virtual methods for common query patterns
3. **Performance Optimizations:** Add methods for bulk operations or specific optimizations
4. **Caching Support:** Extend with caching decorators or base class modifications

**Where NOT to Add Logic:**
- **Business Validation:** Belongs in service layer or domain entities
- **File System Operations:** Belongs in dedicated file services
- **Complex Domain Operations:** Belong in domain services or specialized repositories
- **User Interface Logic:** Belongs in presentation layer

**Safe Extension Points:**
- Virtual methods for overriding in specialized repositories
- Additional query methods for common patterns
- Extension methods for reusable query logic
- Decorator pattern for cross-cutting concerns

**Common Mistakes:**
1. **Adding Business Logic:** Keep repository focused on data access only
2. **Complex Queries:** Move complex queries to specifications or query objects
3. **Synchronous Operations:** Maintain async pattern throughout
4. **Ignoring Cancellation Tokens:** Support cancellation in all async operations

**Refactoring Warnings:**
- Changing method signatures affects all repository implementations
- Adding new generic constraints may break existing usage
- Removing methods breaks interface contract
- Modifying async patterns affects calling code

## Failure Modes & Debugging

**Common Runtime Errors:**
- **DbContext Disposal:** Operations after context disposal
- **Entity Tracking Conflicts:** Multiple instances of same entity tracked
- **Query Translation Issues:** LINQ expressions not translatable to SQL
- **Constraint Violations:** Database constraint failures during save

**Null/Reference Risks:**
- **Null Context:** Constructor validates but runtime issues possible
- **Null Entities:** Validated in methods but may occur in complex scenarios
- **Null Query Results:** Methods return null for not-found scenarios

**Performance Risks:**
- **N+1 Query Problems:** Missing Include() calls in specialized repositories
- **Large Result Sets:** Unbounded queries may cause memory issues
- **Change Tracker Overhead:** Tracking many entities impacts performance
- **Async/Await Overhead:** Unnecessary async for simple operations

**Logging Points:**
- Entity CRUD operations (create, read, update, delete)
- Query execution and performance metrics
- Error conditions and exception handling
- Transaction boundaries and commit/rollback operations

**How to Debug Step-by-Step:**
1. **Entity Tracking:** Inspect DbContext change tracker for entity states
2. **Query Generation:** Enable EF Core logging to see generated SQL
3. **Connection Issues:** Monitor database connection and transaction state
4. **Performance Problems:** Profile query execution times and patterns

**Common Debugging Scenarios:**
```csharp
// Debug generated SQL
services.AddDbContext<SnitcherDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.LogTo(Console.WriteLine, LogLevel.Information);
});

// Debug entity tracking
var entries = context.ChangeTracker.Entries();
foreach (var entry in entries)
{
    Debug.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
}

// Debug query performance
var stopwatch = Stopwatch.StartNew();
var results = await repository.FindAsync(predicate);
stopwatch.Stop();
Debug.WriteLine($"Query took {stopwatch.ElapsedMilliseconds}ms");

// Debug repository state
Debug.WriteLine($"DbSet type: {_dbSet.GetType().Name}");
Debug.WriteLine($"Context tracking: {_context.ChangeTracker.HasChanges()}");
```

## Cross-References

**Related Classes:**
- `IRepository<TEntity>`: Interface contract implemented by this repository
- `SnitcherDbContext`: Database context used for all operations
- `WorkspaceRepository`: Specialized repository inheriting from this base
- `ProjectRepository`: Specialized repository inheriting from this base

**Upstream Callers:**
- `Snitcher.Service.Services.*`: All service layer implementations
- `UnitOfWork`: Coordinates multiple repository operations
- Application layer components requiring data access

**Downstream Dependencies:**
- `SnitcherDbContext` for Entity Framework Core operations
- Domain entities through generic type parameters
- Entity Framework Core infrastructure for ORM operations

**Documents That Should Be Read Before/After:**
- **Before:** `IRepository.md`, `SnitcherDbContext.md`
- **After:** `WorkspaceRepository.md`, `ProjectRepository.md`, `UnitOfWork.md`
- **Related:** `ServiceCollectionExtensions.md` (for DI configuration)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Generic Repository Pattern:** Type-safe data access abstraction
- **Async/Aawait Patterns:** Modern asynchronous data access
- **Entity Framework Integration:** Proper ORM usage patterns
- **Testability Through Interfaces:** Mock-friendly data access design
- **CRUD Operation Standardization:** Consistent data access patterns

**What Is Project-Specific:**
- **SnitcherDbContext Dependency:** Specific to this application's context
- **Entity Types:** Specific to this application's domain model

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Generic Repository Interface:** Specify standard CRUD operations
2. **Implement Generic Repository Class:** Use Entity Framework Core for operations
3. **Add Generic Constraints:** Ensure type safety and proper entity contracts
4. **Implement Async Operations:** Use async/await throughout for performance
5. **Handle Error Cases:** Validate inputs and handle Entity Framework exceptions
6. **Support Querying:** Add flexible query methods with expressions
7. **Register in DI Container:** Configure proper lifetime and registration
8. **Enable Testing:** Ensure interface-based design for mocking

**Key Architectural Insights:**
- **Abstraction Over Complexity:** Repository hides Entity Framework complexity
- **Type Safety Through Generics:** Compile-time guarantees prevent runtime errors
- **Async by Default:** Modern applications need non-blocking data access
- **Interface-Based Design:** Enables testing and flexibility
- **Focused Responsibility:** Repository handles data access, not business logic

**Implementation Checklist for New Projects:**
- [ ] Define generic repository interface with standard CRUD operations
- [ ] Implement generic repository using Entity Framework Core
- [ ] Add proper generic constraints for type safety
- [ ] Implement all operations asynchronously with cancellation support
- [ ] Add comprehensive error handling and validation
- [ ] Include flexible querying capabilities with expressions
- [ ] Configure proper dependency injection registration
- [ ] Add comprehensive unit tests with mocking support
- [ ] Document usage patterns and best practices
