# SnitcherDbContext

## Overview

SnitcherDbContext is the primary Entity Framework Core database context for the Snitcher application. It manages the connection to SQLite database, configures entity mappings, handles database operations, and implements transaction management through the IUnitOfWork interface. This context serves as the central hub for all data persistence operations.

**Why it exists**: To provide a centralized, type-safe, and efficient way to interact with the SQLite database, abstracting away raw SQL operations and providing Entity Framework's ORM capabilities including change tracking, lazy loading, and query optimization.

**Problem it solves**: Without this context, developers would need to write raw SQL queries, manage database connections manually, handle schema mappings, and implement transaction management from scratch - significantly increasing complexity and error potential.

**What would break if removed**: All data persistence would fail. The application couldn't save or retrieve workspaces, projects, or any other entities. Database migrations would fail, and the entire data access layer would cease to function.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0, Entity Framework Core 8.0

**Libraries**: Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite

**UI frameworks**: N/A (infrastructure layer)

**Persistence / communication technologies**: SQLite database, Entity Framework Core ORM

**Build tools**: MSBuild, EF Core tools for migrations

**Runtime assumptions**: .NET 8.0 runtime, SQLite provider available, file system access for database file

**Version hints**: Uses EF Core 8.0 features, modern async patterns, SQLite-specific configurations

## Architectural Role

**Layer**: Infrastructure Layer (Repository)

**Responsibility boundaries**:
- MUST manage database connections and operations
- MUST configure entity mappings and relationships
- MUST handle database migrations and schema management
- MUST implement transaction management via IUnitOfWork
- MUST NOT contain business logic
- MUST NOT be accessed directly by UI layer

**What it MUST do**:
- Provide DbSets for entity access
- Configure entity relationships and constraints
- Handle automatic timestamp management
- Implement soft delete query filters
- Manage database transactions
- Apply database migrations

**What it MUST NOT do**:
- Implement business rules or validation
- Handle UI-specific concerns
- Perform complex business workflows
- Access external services or APIs

**Dependencies (incoming)**: Repository layer, Service layer, Application layer

**Dependencies (outgoing)**: Entity Framework Core, SQLite provider, Entity configurations

## Execution Flow

**Where execution starts**: SnitcherDbContext is instantiated by dependency injection when services request database operations.

**How control reaches this component**:
1. Application startup configures DbContext in DI container
2. Services request repositories or DbContext directly
3. DI container provides configured DbContext instance
4. Database operations are performed through the context

**Call sequence (step-by-step)**:
1. Service layer calls repository method
2. Repository uses DbContext to perform operation
3. DbContext tracks changes and generates SQL
4. SQL executed against SQLite database
5. Results materialized as entity objects
6. Changes tracked until SaveChangesAsync called

**Synchronous vs asynchronous behavior**: Supports both, but async operations preferred for performance

**Threading / dispatcher / event loop notes**: DbContext instances are not thread-safe - should not be shared across threads

**Lifecycle**: Scoped service lifetime - new instance per request/scope

## Public API / Surface Area

**DbSets**:
- `DbSet<Workspace> Workspaces`: Workspace entity collection
- `DbSet<ProjectEntity> Projects`: Project entity collection

**IUnitOfWork Implementation**:
- `Task BeginTransactionAsync(CancellationToken cancellationToken = default)`: Begin database transaction
- `Task CommitAsync(CancellationToken cancellationToken = default)`: Commit transaction
- `Task RollbackAsync(CancellationToken cancellationToken = default)`: Rollback transaction

**DbContext Overrides**:
- `Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)`: Save changes with timestamp updates

**Protected Methods**:
- `OnModelCreating(ModelBuilder modelBuilder)`: Configure entity mappings
- `OnConfiguring(DbContextOptionsBuilder optionsBuilder)`: Configure database connection

**Expected input/output**:
- Input: Entity objects, query expressions, transaction commands
- Output: Query results, entity collections, operation success/failure

**Side effects**:
- Modifies database state
- Updates entity timestamps automatically
- Manages transaction state

**Error behavior**: Throws Entity Framework exceptions for database errors, wraps transaction errors appropriately

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Class declaration and interfaces (line 13)**:
```csharp
public class SnitcherDbContext : DbContext, IUnitOfWork
```
- Inherits from EF Core DbContext
- Implements IUnitOfWork for transaction management
- Enables dependency injection and unit testing

**DbSet properties (lines 18-23)**:
```csharp
public DbSet<Workspace> Workspaces { get; set; }
public DbSet<ProjectEntity> Projects { get; set; }
```
- Provides typed access to entity collections
- Enables LINQ queries and entity tracking
- Maps to database tables by convention

**Constructor (lines 28-31)**:
```csharp
public SnitcherDbContext(DbContextOptions<SnitcherDbContext> options) : base(options)
{
}
```
- Accepts options from dependency injection
- Passes options to base DbContext constructor
- Enables configuration flexibility

**OnModelCreating method (lines 37-50)**:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply entity configurations
    modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
    modelBuilder.ApplyConfiguration(new ProjectEntityConfiguration());
    
    // Configure query filters for soft delete
    ConfigureSoftDeleteFilters(modelBuilder);
    
    // Configure indexes for performance
    ConfigureIndexes(modelBuilder);
}
```
- Applies fluent API configurations
- Sets up entity relationships and constraints
- Configures global query filters
- Sets up performance indexes

**OnConfiguring method (lines 56-73)**:
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        // Default configuration for development
        var databasePath = GetDefaultDatabasePath();
        optionsBuilder.UseSqlite($"Data Source={databasePath}");
        
        // Enable sensitive data logging in development
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }
    
    base.OnConfiguring(optionsBuilder);
}
```
- Provides fallback configuration
- Sets up SQLite connection string
- Enables development-time debugging features
- Uses conditional compilation for debug features

**SaveChangesAsync override (lines 80-84)**:
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    UpdateTimestamps();
    return await base.SaveChangesAsync(cancellationToken);
}
```
- Automatically updates timestamps before saving
- Delegates to base implementation
- Ensures audit trail consistency

**Transaction methods (lines 90-126)**:
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

public async Task RollbackAsync(CancellationToken cancellationToken = default)
{
    if (Database.CurrentTransaction != null)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }
}
```
- Implements transaction management pattern
- Handles nested transaction scenarios
- Provides automatic rollback on commit failure
- Ensures proper cleanup

**Soft delete filter configuration (lines 132-136)**:
```csharp
private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<ProjectEntity>().HasQueryFilter(e => !e.IsDeleted);
}
```
- Automatically filters out soft-deleted entities
- Applies to all queries unless explicitly bypassed
- Enables data recovery capabilities

**Timestamp update logic (lines 152-171)**:
```csharp
private void UpdateTimestamps()
{
    var entries = ChangeTracker.Entries<BaseEntity>();
    
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Property(e => e.CreatedAt).CurrentValue = DateTime.UtcNow;
            entry.Property(e => eUpdatedAt).CurrentValue = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
            
            // Don't update CreatedAt on modification
            entry.Property(e => e.CreatedAt).IsModified = false;
        }
    }
}
```
- Automatically manages audit timestamps
- Handles both insert and update scenarios
- Preserves original creation timestamps

**Database path resolution (lines 178-188)**:
```csharp
private static string GetDefaultDatabasePath()
{
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var appFolder = Path.Combine(appDataPath, "Snitcher");
    var databasePath = Path.Combine(appFolder, "snitcher.db");
    
    // Ensure the directory exists
    Directory.CreateDirectory(appFolder);
    
    return databasePath;
}
```
- Resolves to user's AppData directory
- Creates directory if needed
- Provides cross-platform path handling

**Algorithms used**:
- Entity Framework change tracking algorithm
- Query filter composition
- Transaction management pattern
- Timestamp update algorithm

**Conditional logic explanation**:
- Debug configuration affects logging behavior
- Transaction state checking prevents nested transactions
- Entity state enumeration determines timestamp updates
- Directory creation only if needed

**State transitions**:
- Context created → Ready for operations
- Transaction begun → Operations within transaction
- SaveChanges called → Changes persisted to database
- Transaction committed/rolled back → Transaction completed

**Important invariants**:
- Always updates timestamps on save
- Never returns soft-deleted entities in queries
- Database directory always exists before connection
- Transaction state is properly managed

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Unit of Work Pattern**: Implemented via IUnitOfWork interface
- **Repository Pattern**: DbContext acts as repository repository
- **Active Record Pattern**: Entities contain behavior
- **Configuration Pattern**: Separate configuration classes

**Architectural patterns**:
- **Clean Architecture**: Infrastructure dependency direction
- **Domain-Driven Design**: Ubiquitous language in mappings
- **CQRS**: Separate read/write concerns through EF

**Why these patterns were chosen (inferred)**:
- Unit of Work ensures transactional consistency
- Repository pattern abstracts data access
- Configuration pattern keeps mappings organized
- Clean architecture maintains dependency rules

**Trade-offs**:
- DbContext as Unit of Work: Convenient but couples concerns
- SQLite vs other databases: Simple but less scalable
- Automatic timestamps: Convenient but less explicit
- Soft delete filters: Useful but add query complexity

**Anti-patterns avoided or possibly introduced**:
- Avoided: Direct SQL injection vulnerabilities
- Avoided: Hardcoded connection strings
- Possible risk: DbContext doing too much

## Binding / Wiring / Configuration

**Dependency injection**: Registered as scoped service in ServiceCollectionExtensions

**Data binding (if UI)**: N/A - infrastructure layer

**Configuration sources**: AppSettings, environment variables, default fallback

**Runtime wiring**: Entity Framework dependency injection system

**Registration points**: ServiceCollectionExtensions.ConfigureSnitcher() method

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Using DbContext directly
using var context = new SnitcherDbContext(options);
var workspaces = await context.Workspaces.ToListAsync();
```

**Realistic example**:
```csharp
public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly SnitcherDbContext _context;
    
    public WorkspaceRepository(SnitcherDbContext context)
    {
        _context = context;
    }
    
    public async Task<Workspace> AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        await _context.Workspaces.AddAsync(workspace, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return workspace;
    }
    
    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Include(w => w.Projects)
            .ToListAsync(cancellationToken);
    }
}
```

**Transaction example**:
```csharp
public class ProjectService
{
    private readonly SnitcherDbContext _context;
    
    public async Task CreateProjectWithWorkspaceAsync(Workspace workspace, Project project)
    {
        await _context.BeginTransactionAsync();
        try
        {
            await _context.Workspaces.AddAsync(workspace);
            await _context.Projects.AddAsync(project);
            await _context.CommitAsync();
        }
        catch
        {
            await _context.RollbackAsync();
            throw;
        }
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Sharing DbContext across threads
var context = serviceProvider.GetRequiredService<SnitcherDbContext>();
Task.Run(() => context.Workspaces.ToList()); // Thread safety violation

// WRONG: Not disposing DbContext
var context = new SnitcherDbContext(options);
// Use context but don't dispose - resource leak

// WRONG: Long-lived DbContext
public class BadService 
{
    private readonly SnitcherDbContext _context; // Should be scoped
    public BadService(SnitcherDbContext context) => _context = context;
}

// WRONG: Bypassing soft delete filters
var allWorkspaces = await context.Workspaces
    .IgnoreQueryFilters() // Bypasses soft delete - dangerous
    .ToListAsync();
```

**How to test this in isolation**:
```csharp
// Using in-memory database for testing
var options = new DbContextOptionsBuilder<SnitcherDbContext>()
    .UseInMemoryDatabase("TestDb")
    .Options;

using var context = new SnitcherDbContext(options);
await context.Database.EnsureCreatedAsync();

// Test operations
var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
await context.Workspaces.AddAsync(workspace);
await context.SaveChangesAsync();

var result = await context.Workspaces.FindAsync(workspace.Id);
Assert.That(result, Is.Not.Null);
```

**How to mock or replace it**:
```csharp
// Using repository pattern to abstract DbContext
public interface IWorkspaceRepository
{
    Task<Workspace> AddAsync(Workspace workspace);
    Task<IReadOnlyList<Workspace>> GetAllAsync();
}

// Mock repository for testing
public class MockWorkspaceRepository : IWorkspaceRepository
{
    private readonly List<Workspace> _workspaces = new();
    
    public async Task<Workspace> AddAsync(Workspace workspace)
    {
        _workspaces.Add(workspace);
        return workspace;
    }
    
    public async Task<IReadOnlyList<Workspace>> GetAllAsync()
    {
        return _workspaces.AsReadOnly();
    }
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new DbSet property for new entity
2. Add entity configuration in OnModelCreating
3. Add soft delete filter if needed
4. Update transaction logic if needed
5. Add migration for schema changes

**Where NOT to add logic**:
- Don't add business validation logic
- Don't add UI-specific concerns
- Don't add complex workflow orchestration
- Don't add external service calls

**Safe extension points**:
- New DbSets for additional entities
- Additional entity configurations
- Custom query filters
- Additional transaction methods
- Database-specific optimizations

**Common mistakes**:
- Adding business logic to DbContext
- Making DbContext long-lived or singleton
- Forgetting to update migrations
- Ignoring thread safety concerns
- Hardcoding connection strings

**Refactoring warnings**:
- Changing DbSet names breaks queries
- Removing entities breaks existing data
- Changing connection strings affects all environments
- Modifying transaction logic affects data consistency

## Failure Modes & Debugging

**Common runtime errors**:
- **SqlException**: Database connection or query failures
- **InvalidOperationException**: Invalid entity state or configuration
- **TimeoutException**: Long-running queries or transactions
- **DbUpdateConcurrencyException**: Concurrent modification conflicts

**Null/reference risks**:
- DbSet properties are never null after construction
- Options parameter required in constructor
- Database connection can fail but is handled gracefully

**Performance risks**:
- Large result sets impact memory usage
- N+1 query problems with lazy loading
- Missing indexes cause slow queries
- Long-running transactions block other operations

**Logging points**:
- Sensitive data logging in debug mode
- Entity Framework logging for query analysis
- Transaction logging for debugging
- Error logging for database failures

**How to debug step-by-step**:
1. Enable sensitive data logging in development
2. Use EF Core logging to see generated SQL
3. Set breakpoints in OnModelCreating for configuration issues
4. Monitor ChangeTracker for unexpected entity states
5. Use database inspection tools to verify schema

## Cross-References

**Related classes**:
- BaseEntity (timestamp management)
- Workspace, ProjectEntity (mapped entities)
- IUnitOfWork (transaction interface)
- Entity configuration classes

**Upstream callers**:
- Repository implementations
- Service layer (indirectly through repositories)
- Application startup (configuration)

**Downstream dependencies**:
- SQLite database engine
- Entity Framework Core runtime
- Database migration system

**Documents that should be read before/after**:
- Read: BaseEntity documentation (timestamp management)
- Read: Workspace/ProjectEntity documentation (mapped entities)
- Read: Entity configuration documentation
- Read: Repository pattern documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Entity Framework Core DbContext pattern
- Unit of Work implementation
- Automatic timestamp management
- Soft delete query filters
- Transaction management patterns

**What is project-specific**:
- Specific entity types and relationships
- SQLite database configuration
- Particular timestamp update logic
- Specific database path resolution

**How to recreate this pattern from scratch elsewhere**:
1. Create DbContext class inheriting from EF Core DbContext
2. Add DbSet properties for entities
3. Implement IUnitOfWork for transaction management
4. Override OnModelCreating for entity configuration
5. Override SaveChanges for automatic updates
6. Configure database provider and connection
7. Add query filters for cross-cutting concerns

**Key insights for implementation**:
- Always use scoped lifetime for DbContext
- Implement automatic audit trail in SaveChanges override
- Use query filters for cross-cutting concerns
- Handle transaction errors with automatic rollback
- Separate entity configurations into dedicated classes
- Use conditional compilation for development features
