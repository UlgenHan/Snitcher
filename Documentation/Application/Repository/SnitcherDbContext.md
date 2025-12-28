# SnitcherDbContext

## Overview

SnitcherDbContext is the central Entity Framework Core database context that manages the connection to the SQLite database and orchestrates all data persistence operations for the Snitcher application. It serves as the primary gateway between the domain model and the database, implementing the Unit of Work pattern and providing automatic audit trail management, soft delete filtering, and transaction coordination.

This context exists to provide a unified, type-safe interface for all database operations while enforcing architectural constraints and cross-cutting concerns. Without SnitcherDbContext, the application would lack centralized data access management, automatic timestamp handling, soft delete enforcement, and transaction coordination. The context ensures data integrity, implements performance optimizations, and provides a clean separation between business logic and data access concerns.

If SnitcherDbContext were removed, the system would lose:
- Centralized database access management
- Automatic audit trail timestamp updates
- Soft delete query filtering
- Transaction coordination and rollback capabilities
- Entity Framework Core configuration and mapping
- Database connection and connection string management
- Change tracking and entity lifecycle management

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- Entity Framework Core 8.0
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Sqlite
- .NET 8.0 Base Class Library

**Libraries:**
- Microsoft.EntityFrameworkCore for ORM functionality
- Microsoft.EntityFrameworkCore.Sqlite for SQLite provider
- System.IO for file path operations
- System for environment and directory operations

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- SQLite database engine for local storage
- Entity Framework Core for ORM and change tracking
- Connection string management for database configuration

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- Entity Framework Core tools for migrations

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires SQLite provider availability
- File system access for database file management
- Environment variable access for AppData directory

**Version Hints:**
- Entity Framework Core 8.0 features and conventions
- Modern async/await patterns throughout
- SQLite-specific optimizations and configurations

## Architectural Role

**Layer:** Infrastructure Layer (Clean Architecture - Repository)

**Responsibility Boundaries:**
- MUST manage database connections and transactions
- MUST enforce soft delete query filters globally
- MUST automatically update audit timestamps
- MUST coordinate Unit of Work pattern implementation
- MUST NOT contain business logic or validation rules
- MUST NOT depend on service layer or UI components

**What it MUST do:**
- Configure Entity Framework Core mappings and relationships
- Manage database connection lifecycle
- Implement automatic audit trail functionality
- Enforce soft delete filtering across all queries
- Coordinate transaction management and rollback
- Handle database schema and migration support

**What it MUST NOT do:**
- Implement business validation or rules
- Perform file system operations beyond database management
- Handle user interface concerns
- Depend on application services or business logic
- Implement complex domain operations

**Dependencies (Incoming):**
- Repository layer implementations for data access
- Service layer for business operations requiring persistence
- Unit of Work implementations for transaction coordination
- Application startup and configuration systems

**Dependencies (Outgoing):**
- Entity Framework Core for ORM functionality
- SQLite provider for database connectivity
- Domain entities for mapping and configuration
- Configuration classes for entity-specific mappings

## Execution Flow

**Where execution starts:**
- Application startup during dependency injection registration
- Repository instantiation through DI container
- Service layer operations requiring database access

**How control reaches this component:**
1. Application configures services → ServiceCollectionExtensions.AddRepositoryLayer()
2. Repository instantiated → DI container injects SnitcherDbContext
3. Service operations → Repository methods use context for database operations
4. Transaction management → Unit of Work uses context for coordination

**Call Sequence (Entity Creation):**
1. Service layer creates new entity instance
2. Repository calls context.Set<T>().AddAsync(entity)
3. Context tracks entity in Added state
4. SaveChangesAsync() called → UpdateTimestamps() executes
5. Entity inserted into database with audit timestamps

**Call Sequence (Query with Soft Delete Filter):**
1. Repository executes query (e.g., GetAllAsync())
2. Entity Framework applies global query filter (!e.IsDeleted)
3. Only non-deleted entities returned in results
4. Soft-deleted entities automatically excluded

**Synchronous vs Asynchronous Behavior:**
- All database operations are asynchronous (async/await pattern)
- Context configuration and setup is synchronous
- Change tracking and timestamp updates are synchronous

**Threading/Dispatcher Notes:**
- DbContext instances are NOT thread-safe
- Each operation should use a separate context instance
- Scoped lifetime in DI container ensures per-request context
- Concurrent operations require separate context instances

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Instantiated by DI container with scoped lifetime
2. **Usage:** Multiple repository operations within single unit of work
3. **Transaction:** Optional transaction coordination for multi-operation units
4. **Save Changes:** Automatic timestamp updates and database persistence
5. **Disposal:** Automatically disposed by DI container at scope end

## Public API / Surface Area

**DbSet Properties:**
- `DbSet<Workspace> Workspaces`: Workspace entity collection
- `DbSet<ProjectEntity> Projects`: Project entity collection

**Constructors:**
- `SnitcherDbContext(DbContextOptions<SnitcherDbContext> options)`: Initializes context with options

**Public Methods:**
- `Task<int> SaveChangesAsync(CancellationToken)`: Saves changes with automatic timestamp updates
- `Task BeginTransactionAsync(CancellationToken)`: Begins new database transaction
- `Task CommitAsync(CancellationToken)`: Commits current transaction with rollback on error
- `Task RollbackAsync(CancellationToken)`: Rolls back current transaction

**Protected Methods:**
- `override void OnModelCreating(ModelBuilder)`: Configures entity mappings and filters
- `override void OnConfiguring(DbContextOptionsBuilder)`: Configures database connection

**Expected Input/Output:**
- **Input:** Entity instances for persistence, transaction control parameters
- **Output:** Persisted entities with generated IDs, operation success indicators

**Side Effects:**
- Automatic timestamp updates on save operations
- Global query filtering for soft delete entities
- Transaction management affects database state
- Change tracking modifies internal context state

**Error Behavior:**
- Throws Entity Framework Core exceptions for database errors
- Transaction operations automatically rollback on exceptions
- Validation exceptions thrown for constraint violations
- Null reference exceptions for invalid entity states

## Internal Logic Breakdown

**Database Configuration Logic (Lines 56-73):**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        var databasePath = GetDefaultDatabasePath();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }
}
```
- Provides fallback configuration for development scenarios
- Automatically creates database in user's AppData directory
- Enables detailed logging in debug builds for development support
- Uses SQLite as default provider for desktop application scenarios

**Entity Configuration Logic (Lines 37-50):**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
    modelBuilder.ApplyConfiguration(new ProjectEntityConfiguration());
    
    ConfigureSoftDeleteFilters(modelBuilder);
    ConfigureIndexes(modelBuilder);
}
```
- Applies Fluent API configurations for each entity type
- Configures global soft delete filters for all entities
- Sets up performance-optimizing indexes
- Separates concerns by delegating to specific configuration classes

**Automatic Timestamp Update Logic (Lines 152-171):**
```csharp
private void UpdateTimestamps()
{
    var entries = ChangeTracker.Entries<BaseEntity>();
    
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Property(e => e.CreatedAt).CurrentValue = DateTime.UtcNow;
            entry.Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
            entry.Property(e => e.CreatedAt).IsModified = false;
        }
    }
}
```
- Automatically updates timestamps for all BaseEntity-derived entities
- Sets both CreatedAt and UpdatedAt for new entities
- Only updates UpdatedAt for modified entities, preserving CreatedAt
- Uses UTC timestamps for consistency across timezones

**Soft Delete Filter Configuration (Lines 132-136):**
```csharp
private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<ProjectEntity>().HasQueryFilter(e => !e.IsDeleted);
}
```
- Applies global query filters to automatically exclude soft-deleted entities
- Ensures all queries automatically respect soft delete semantics
- Prevents accidental access to deleted entities
- Can be bypassed with IgnoreQueryFilters() when needed

**Transaction Management Logic (Lines 90-126):**
```csharp
public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
{
    if (Database.CurrentTransaction == null)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }
}

public async Task CommitAsync(CancellationToken cancellationToken = default)
{
    try
    {
        await SaveChangesAsync(cancellationToken);
        await Database.CommitTransactionAsync(cancellationToken);
    }
    catch
    {
        await RollbackAsync(cancellationToken);
        throw;
    }
}
```
- Implements safe transaction management with automatic rollback
- Prevents nested transaction creation
- Ensures atomicity of multi-entity operations
- Provides exception safety with automatic cleanup

**Database Path Management (Lines 178-188):**
```csharp
private static string GetDefaultDatabasePath()
{
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var appFolder = Path.Combine(appDataPath, "Snitcher");
    var databasePath = Path.Combine(appFolder, "snitcher.db");
    
    Directory.CreateDirectory(appFolder);
    
    return databasePath;
}
```
- Locates database in appropriate user directory
- Automatically creates directory structure if needed
- Follows Windows application data storage conventions
- Ensures proper permissions and user isolation

## Patterns & Principles Used

**Design Patterns:**
- **Unit of Work Pattern:** Context coordinates multiple repository operations
- **Repository Pattern Support:** Provides foundation for repository implementations
- **Active Record Pattern Integration:** Works with entity self-contained operations
- **Configuration Pattern:** Separates entity configuration into dedicated classes

**Architectural Patterns:**
- **Clean Architecture:** Infrastructure layer with clear separation from domain
- **Domain-Driven Design:** Supports domain entities without contaminating them
- **CQRS Support:** Can be extended for command/query separation
- **Cross-Cutting Concerns:** Centralizes audit trails and soft delete logic

**Why These Patterns Were Chosen:**
- **Unit of Work:** Ensures transactional consistency across operations
- **Repository Pattern:** Provides abstraction over data access complexity
- **Configuration Pattern:** Keeps entity mapping organized and maintainable
- **Clean Architecture:** Maintains testability and separation of concerns

**Trade-offs:**
- **Pros:** Centralized management, automatic cross-cutting concerns, transaction safety
- **Cons:** Entity Framework dependency, context lifecycle complexity
- **Decision:** Benefits of centralized management outweigh framework coupling

**Anti-patterns Avoided:**
- **God Object:** Context focused on data access, not business logic
- **Tight Coupling:** Uses interfaces and abstractions where possible
- **Repository Overkill:** Generic repository reduces boilerplate while maintaining abstraction

## Binding / Wiring / Configuration

**Dependency Injection:**
- Registered as scoped service in ServiceCollectionExtensions
- Scoped lifetime ensures one context per request/unit of work
- Constructor injection provides DbContextOptions configuration

**Data Binding:**
- DbSet properties automatically bound to database tables
- Entity configurations map domain entities to database schema
- Navigation properties configured for relationships and foreign keys

**Configuration Sources:**
- Fluent API configurations in separate configuration classes
- OnModelCreating method applies all configurations
- Connection string configured through DI options pattern

**Runtime Wiring:**
- Entity Framework Core change tracker automatically manages entity state
- Query filters applied automatically to all queries
- Transaction management coordinated through context methods

**Registration Points:**
- ServiceCollectionExtensions.AddRepositoryLayer() for production
- AddRepositoryLayerInMemory() for testing scenarios
- UseSqliteDatabase() for custom SQLite configuration

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Register in DI container
services.AddRepositoryLayer();

// Use in repository
public class ProjectRepository
{
    private readonly SnitcherDbContext _context;
    
    public ProjectRepository(SnitcherDbContext context)
    {
        _context = context;
    }
    
    public async Task<ProjectEntity> CreateAsync(ProjectEntity project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(); // Automatic timestamps
        return project;
    }
}
```

**Realistic Example:**
```csharp
public class WorkspaceService
{
    private readonly SnitcherDbContext _context;
    
    public async Task<Workspace> CreateWorkspaceWithProjectsAsync(
        CreateWorkspaceDto dto)
    {
        // Use Unit of Work pattern
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var workspace = new Workspace
            {
                Name = dto.Name,
                Path = dto.Path,
                IsDefault = dto.IsDefault
            };
            
            _context.Workspaces.Add(workspace);
            
            // Add projects
            foreach (var projectDto in dto.Projects)
            {
                var project = new ProjectEntity
                {
                    Name = projectDto.Name,
                    Path = projectDto.Path,
                    WorkspaceId = workspace.Id
                };
                _context.Projects.Add(project);
            }
            
            // Single SaveChanges with automatic timestamps
            await _context.SaveChangesAsync();
            
            await transaction.CommitAsync();
            return workspace;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<List<Workspace>> GetActiveWorkspacesAsync()
    {
        // Soft delete filter automatically applied
        return await _context.Workspaces
            .Include(w => w.Projects)
            .ToListAsync();
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Sharing context across threads
var context = serviceProvider.GetService<SnitcherDbContext>();
Task.Run(() => context.Workspaces.ToList()); // WRONG - Not thread-safe

// WRONG: Long-lived context
public class BadService
{
    private readonly SnitcherDbContext _context; // WRONG - Should be scoped
    
    public BadService(SnitcherDbContext context)
    {
        _context = context; // This context will live too long
    }
}

// WRONG: Forgetting to handle soft delete filter
public async Task<List<Workspace>> GetAllIncludingDeletedAsync()
{
    // This will exclude soft-deleted entities due to global filter
    return await _context.Workspaces.ToListAsync();
    
    // Correct way:
    // return await _context.Workspaces
    //     .IgnoreQueryFilters()
    //     .ToListAsync();
}
```

**How to Test in Isolation:**
```csharp
[Test]
public async Task Context_ShouldUpdateTimestampsAutomatically()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SnitcherDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;
    
    using var context = new SnitcherDbContext(options);
    var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    
    // Act
    context.Workspaces.Add(workspace);
    await context.SaveChangesAsync();
    
    var originalTime = workspace.UpdatedAt;
    workspace.Name = "Updated";
    await context.SaveChangesAsync();
    
    // Assert
    Assert.That(workspace.CreatedAt, Is.Not.EqualTo(default(DateTime)));
    Assert.That(workspace.UpdatedAt, Is.GreaterThan(originalTime));
}

[Test]
public async Task Context_ShouldFilterSoftDeletedEntities()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SnitcherDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;
    
    using var context = new SnitcherDbContext(options);
    var workspace1 = new Workspace { Name = "Active", Path = @"C:\Active" };
    var workspace2 = new Workspace { Name = "Deleted", Path = @"C:\Deleted" };
    
    context.Workspaces.Add(workspace1);
    context.Workspaces.Add(workspace2);
    await context.SaveChangesAsync();
    
    // Act
    workspace2.MarkAsDeleted();
    await context.SaveChangesAsync();
    
    var activeWorkspaces = await context.Workspaces.ToListAsync();
    var allWorkspaces = await context.Workspaces
        .IgnoreQueryFilters()
        .ToListAsync();
    
    // Assert
    Assert.That(activeWorkspaces.Count, Is.EqualTo(1));
    Assert.That(allWorkspaces.Count, Is.EqualTo(2));
}
```

**How to Mock or Replace:**
```csharp
// Mock DbContext for testing
public class MockSnitcherDbContext : SnitcherDbContext
{
    public List<Workspace> WorkspacesList { get; } = new();
    public List<ProjectEntity> ProjectsList { get; } = new();
    
    public MockSnitcherDbContext() 
        : base(new DbContextOptionsBuilder<SnitcherDbContext>()
            .UseInMemoryDatabase("MockDb").Options)
    {
    }
    
    public override DbSet<Workspace> Workspaces => 
        MockDbSet.Create(WorkspacesList);
    
    public override DbSet<ProjectEntity> Projects => 
        MockDbSet.Create(ProjectsList);
}

// Mock DbSet for in-memory testing
public static class MockDbSet
{
    public static DbSet<T> Create<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        return mockSet.Object;
    }
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **New Entities:** Add DbSet property and entity configuration
2. **Additional Query Filters:** Extend ConfigureSoftDeleteFilters method
3. **Custom Interceptors:** Add interceptors for additional cross-cutting concerns
4. **Performance Optimizations:** Add indexes and query hints in configurations

**Where NOT to Add Logic:**
- **Business Validation:** Belongs in service layer or domain entities
- **File System Operations:** Belongs in dedicated file services
- **User Interface Logic:** Belongs in presentation layer
- **Complex Domain Operations:** Belong in domain services

**Safe Extension Points:**
- OnModelCreating for additional entity configurations
- SaveChangesAsync override for additional interceptors
- New DbSet properties for additional entities
- Additional transaction management methods

**Common Mistakes:**
1. **Adding Business Logic:** Keep context focused on data access only
2. **Long-Lived Contexts:** Use scoped lifetime, not singleton
3. **Thread Safety Violations:** Never share context instances across threads
4. **Complex Queries:** Move complex queries to repository specifications

**Refactoring Warnings:**
- Changing connection string format affects all database operations
- Removing query filters may expose soft-deleted data unexpectedly
- Modifying SaveChangesAsync behavior affects all timestamp management
- Adding new entities requires database migrations

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Database Connection Failures:** SQLite file access or permission issues
- **Migration Conflicts:** Database schema out of sync with model
- **Transaction Deadlocks:** Concurrent transaction conflicts
- **Change Tracker Issues:** Entity state conflicts

**Null/Reference Risks:**
- **DbContext Options:** Null options cause configuration failures
- **Entity References:** Navigation properties may be null if not loaded
- **Transaction Objects:** Null transaction references in rollback scenarios

**Performance Risks:**
- **Large Result Sets:** Unbounded queries may cause memory issues
- **N+1 Query Problems:** Missing Include() calls cause excessive queries
- **Change Tracker Overhead:** Tracking many entities impacts performance
- **Connection Pool Exhaustion:** Improper context lifecycle management

**Logging Points:**
- Database connection and disconnection events
- Transaction begin/commit/rollback operations
- Entity change tracking and save operations
- Query execution and performance metrics

**How to Debug Step-by-Step:**
1. **Connection Issues:** Check database file path and permissions
2. **Query Problems:** Enable EF Core logging to see generated SQL
3. **Transaction Issues:** Verify transaction scope and rollback behavior
4. **Change Tracker:** Inspect entity states and tracked changes

**Common Debugging Scenarios:**
```csharp
// Debug generated SQL
services.AddDbContext<SnitcherDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.LogTo(Console.WriteLine, LogLevel.Information);
    options.EnableSensitiveDataLogging();
});

// Debug change tracking
var entries = context.ChangeTracker.Entries();
foreach (var entry in entries)
{
    Debug.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
}

// Debug query filters
var sql = context.Workspaces.ToQueryString();
Debug.WriteLine($"Generated SQL: {sql}");

// Debug transaction state
Debug.WriteLine($"Current transaction: {context.Database.CurrentTransaction?.TransactionId ?? "None"}");
```

## Cross-References

**Related Classes:**
- `WorkspaceConfiguration`: Fluent API configuration for Workspace entity
- `ProjectEntityConfiguration`: Fluent API configuration for ProjectEntity
- `EfRepository<TEntity>`: Generic repository using this context
- `UnitOfWork`: Unit of Work implementation wrapping this context

**Upstream Callers:**
- `Snitcher.Repository.Repositories.*`: All repository implementations
- `Snitcher.Service.Services.*`: Service layer through repository abstraction
- Application startup and configuration systems

**Downstream Dependencies:**
- `Workspace` and `ProjectEntity` domain entities
- Entity Framework Core infrastructure
- SQLite database provider and engine

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md`, `IEntity.md`, `ISoftDeletable.md`
- **After:** `EfRepository.md`, `UnitOfWork.md`, `WorkspaceConfiguration.md`
- **Related:** `ServiceCollectionExtensions.md` (for DI configuration)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Unit of Work Pattern:** Centralized transaction coordination
- **Automatic Audit Trails:** Timestamp management through interceptors
- **Global Query Filters:** Soft delete implementation pattern
- **DbContext Configuration:** Organized entity configuration approach
- **Connection Management:** Proper database connection lifecycle

**What Is Project-Specific:**
- **SQLite Desktop Configuration:** Specific to desktop application needs
- **AppData Directory Strategy:** Specific to Windows application storage
- **Workspace-Project Model:** Specific to this application's domain

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define DbContext Class:** Inherit from DbContext with proper options
2. **Configure Entities:** Use OnModelCreating with Fluent API configurations
3. **Implement Audit Intercepts:** Override SaveChangesAsync for timestamp management
4. **Add Query Filters:** Configure global filters for cross-cutting concerns
5. **Manage Transactions:** Implement Unit of Work pattern with proper rollback
6. **Configure Connection:** Set up appropriate database provider and connection string
7. **Register Dependencies:** Configure proper DI lifetime and registration

**Key Architectural Insights:**
- **Centralized Management:** DbContext provides single point of control
- **Automatic Cross-Cutting Concerns:** Interceptors reduce boilerplate code
- **Query Filter Power:** Global filters prevent accidental data exposure
- **Transaction Safety:** Proper rollback handling ensures data integrity
- **Configuration Organization:** Separate configuration classes maintain clarity

**Implementation Checklist for New Projects:**
- [ ] Define DbContext with appropriate constructor and options
- [ ] Configure entity mappings using Fluent API
- [ ] Implement automatic audit trail functionality
- [ ] Add global query filters for soft delete or other concerns
- [ ] Configure proper database connection and provider
- [ ] Implement transaction management with rollback
- [ ] Set up proper dependency injection registration
- [ ] Add comprehensive logging and debugging support
- [ ] Test with both real and in-memory databases
