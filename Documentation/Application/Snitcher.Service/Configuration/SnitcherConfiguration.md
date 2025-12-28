# SnitcherConfiguration

## Overview

SnitcherConfiguration is the central configuration and bootstrapping component for the entire Snitcher application stack. It provides a fluent API for setting up dependency injection, configuring database providers, registering services and repositories, and initializing the database. This class serves as the main entry point for configuring the application's infrastructure.

**Why it exists**: To provide a centralized, type-safe, and extensible way to configure all application services, ensuring consistent setup across different environments and enabling easy testing with different configurations.

**Problem it solves**: Without this configuration class, each component would need to be manually registered with specific dependencies, leading to scattered configuration code, potential registration errors, and difficulty in swapping implementations (e.g., SQLite vs in-memory database for testing).

**What would break if removed**: The entire application would fail to start. No services would be registered, the database wouldn't be configured, and dependency injection would not work. The application would have no runtime infrastructure.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Entity Framework Core

**Libraries**: Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.Extensions.Logging

**UI frameworks**: N/A (infrastructure configuration)

**Persistence / communication technologies**: SQLite, in-memory database, custom database providers

**Build tools**: MSBuild, Entity Framework tools

**Runtime assumptions**: .NET 8.0 runtime, dependency injection container, database providers available

**Version hints**: Uses modern .NET configuration patterns, EF Core 8.0 features, extension method patterns

## Architectural Role

**Layer**: Application Layer (Configuration)

**Responsibility boundaries**:
- MUST configure all application services
- MUST set up database providers and connections
- MUST register repositories and services
- MUST initialize database schema
- MUST NOT contain business logic
- MUST NOT depend on UI layer

**What it MUST do**:
- Configure dependency injection container
- Set up database context with appropriate provider
- Register all repository and service implementations
- Provide database initialization
- Support multiple database providers
- Enable configuration customization

**What it MUST NOT do**:
- Implement business rules or validation
- Handle UI-specific concerns
- Access application-specific data directly
- Make runtime decisions based on business logic

**Dependencies (incoming)**: Application startup, hosting environment, test frameworks

**Dependencies (outgoing)**: All service and repository types, Entity Framework, logging infrastructure

## Execution Flow

**Where execution starts**: SnitcherConfiguration is called during application startup to configure the service container.

**How control reaches this component**:
1. Application startup code calls ConfigureSnitcher()
2. Extension method registers all services
3. Database provider is configured based on options
4. Service container is built and used by host

**Call sequence (step-by-step)**:
1. Startup code creates ServiceCollection
2. ConfigureSnitcher() extension method called
3. SnitcherOptions created and configured
4. Database provider configured based on options
5. Repositories registered with DI container
6. Services registered with DI container
7. Service provider built and returned
8. InitializeDatabaseAsync() called to set up database

**Synchronous vs asynchronous behavior**: Configuration is synchronous, database initialization is asynchronous

**Threading / dispatcher / event loop notes**: Thread-safe during configuration, database initialization should be called once during startup

**Lifecycle**: Configuration occurs once during application startup, services have configured lifetimes

## Public API / Surface Area

**Extension Methods**:
- `IServiceCollection ConfigureSnitcher(this IServiceCollection services)`: Configure with default options
- `IServiceCollection ConfigureSnitcher(this IServiceCollection services, Action<SnitcherOptions> configureOptions)`: Configure with custom options

**Database Initialization**:
- `Task InitializeDatabaseAsync(IServiceProvider serviceProvider, bool ensureCreated = true, bool applyMigrations = true)`: Initialize database schema

**Configuration Class**:
- `SnitcherOptions`: Configuration options for database provider and settings

**Expected input/output**:
- Input: Service collection, configuration options
- Output: Configured service collection, initialized database

**Side effects**:
- Registers services in DI container
- Configures database providers
- Creates/updates database schema
- Enables logging infrastructure

**Error behavior**: Throws configuration exceptions for invalid setups, database exceptions for initialization failures

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Main configuration method (lines 22-73)**:
```csharp
public static IServiceCollection ConfigureSnitcher(
    this IServiceCollection services,
    Action<SnitcherOptions> configureOptions)
{
    var options = new SnitcherOptions();
    configureOptions?.Invoke(options);

    // Configure database based on options
    switch (options.DatabaseProvider.ToLowerInvariant())
    {
        case "sqlite":
            services.ConfigureSqliteDatabase(options.DatabasePath);
            break;
        case "inmemory":
            services.ConfigureInMemoryDatabase(options.DatabaseName);
            break;
        case "custom":
            if (options.ConfigureDbContext == null)
                throw new ArgumentException("ConfigureDbContext must be provided when using custom database provider.");
            services.AddDbContext<SnitcherDbContext>(options.ConfigureDbContext);
            break;
        default:
            throw new ArgumentException($"Unsupported database provider: {options.DatabaseProvider}");
    }

    // Register repositories
    services.AddScoped<IWorkspaceRepository, Repository.Repositories.WorkspaceRepository>();
    services.AddScoped<IProjectRepository, Repository.Repositories.ProjectRepository>();
    services.AddScoped(typeof(IRepository<>), typeof(Repository.Repositories.EfRepository<>));
    services.AddScoped(typeof(IRepository<,>), typeof(Repository.Repositories.EfRepository<,>));
    services.AddScoped<IUnitOfWork, Repository.Repositories.UnitOfWork>();

    // Register services
    services.AddScoped<Service.Interfaces.IWorkspaceService, Service.Services.WorkspaceService>();
    services.AddScoped<Service.Interfaces.IProjectService, Service.Services.ProjectService>();

    // Register options for dependency injection
    services.AddSingleton(options);

    return services;
}
```
- Creates and configures SnitcherOptions
- Switches on database provider type
- Configures appropriate database context
- Registers all repository implementations
- Registers all service implementations
- Registers options as singleton for DI
- Returns configured service collection

**SQLite configuration (lines 81-89)**:
```csharp
private static IServiceCollection ConfigureSqliteDatabase(
    this IServiceCollection services,
    string? databasePath)
{
    var connectionString = GetSqliteConnectionString(databasePath);
    services.AddDbContext<SnitcherDbContext>(options =>
        options.UseSqlite(connectionString));
    return services;
}
```
- Generates SQLite connection string
- Configures DbContext with SQLite provider
- Handles path resolution and directory creation

**In-memory configuration (lines 97-104)**:
```csharp
private static IServiceCollection ConfigureInMemoryDatabase(
    this IServiceCollection services,
    string databaseName)
{
    services.AddDbContext<SnitcherDbContext>(options =>
        options.UseInMemoryDatabase(databaseName));
    return services;
}
```
- Configures DbContext for in-memory database
- Used primarily for testing scenarios
- Provides isolated database per test run

**Connection string generation (lines 111-138)**:
```csharp
private static string GetSqliteConnectionString(string? databasePath)
{
    if (string.IsNullOrWhiteSpace(databasePath))
    {
        // Use default path in user's AppData
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "Snitcher");
        var dbPath = Path.Combine(appFolder, "snitcher.db");
        
        // Ensure directory exists
        Directory.CreateDirectory(appFolder);
        
        return $"Data Source={dbPath}";
    }
    else
    {
        // Use custom path
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        return $"Data Source={fullPath}";
    }
}
```
- Resolves default database path to AppData
- Creates directory if it doesn't exist
- Handles custom path resolution
- Ensures full path resolution

**Database initialization (lines 146-190)**:
```csharp
public static async Task InitializeDatabaseAsync(
    IServiceProvider serviceProvider,
    bool ensureCreated = true,
    bool applyMigrations = true)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SnitcherDbContext>();

    try
    {
        // Check if database exists and has tables
        var canConnect = await context.Database.CanConnectAsync();
        
        if (!canConnect)
        {
            // Database doesn't exist, create it
            await context.Database.EnsureCreatedAsync();
        }
        else if (applyMigrations)
        {
            // Database exists, apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync();
            }
        }
    }
    catch (Exception ex)
    {
        // If migration fails, try to ensure created as fallback
        System.Diagnostics.Debug.WriteLine($"Migration failed: {ex.Message}. Attempting to ensure database is created.");
        
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception fallbackEx)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize database: {fallbackEx.Message}");
            throw;
        }
    }
}
```
- Creates service scope for database operations
- Checks database connectivity
- Applies migrations if needed
- Provides fallback to EnsureCreated
- Comprehensive error handling and logging

**SnitcherOptions class (lines 195-234)**:
```csharp
public class SnitcherOptions
{
    public string DatabaseProvider { get; set; } = "sqlite";
    public string? DatabasePath { get; set; }
    public string DatabaseName { get; set; } = "SnitcherDb";
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int? CommandTimeout { get; set; }
}
```
- Configuration properties for database setup
- Supports multiple database providers
- Includes debugging and performance options
- Extensible for future configuration needs

**Algorithms used**:
- Provider selection algorithm (switch statement)
- Path resolution and directory creation
- Migration vs EnsureCreated decision logic
- Service registration patterns

**Conditional logic explanation**:
- Database provider selection determines configuration path
- Path resolution handles both default and custom paths
- Migration logic checks for pending migrations before applying
- Error handling provides graceful fallbacks

**State transitions**:
- Service collection → Configured service collection
- No database → Database created
- Existing database → Migrations applied
- Migration failure → Fallback to EnsureCreated

**Important invariants**:
- Always registers required services
- Database directory always exists before connection
- At least one database provider must be configured
- Custom provider requires configuration action

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Builder Pattern**: Fluent configuration API
- **Strategy Pattern**: Database provider selection
- **Factory Pattern**: Service provider creation
- **Options Pattern**: Configuration object pattern

**Architectural patterns**:
- **Dependency Injection**: Service registration and resolution
- **Configuration as Code**: Programmatic configuration
- **Startup Configuration**: Centralized bootstrapping

**Why these patterns were chosen (inferred)**:
- Builder pattern provides fluent, readable configuration
- Strategy pattern enables database provider flexibility
- Dependency injection enables testability and loose coupling
- Options pattern provides type-safe configuration

**Trade-offs**:
- Centralized configuration vs distributed: Simpler but less flexible
- Multiple database providers vs single: More flexible but more complex
- Automatic database initialization vs manual: Convenient but less control

**Anti-patterns avoided or possibly introduced**:
- Avoided: Hardcoded dependencies
- Avoided: Configuration scattered across files
- Possible risk: Configuration class becoming too large

## Binding / Wiring / Configuration

**Dependency injection**: Configures the entire DI container

**Data binding (if UI)**: N/A - infrastructure configuration

**Configuration sources**: Code-based configuration, could be extended to use appsettings.json

**Runtime wiring**: Microsoft.Extensions.DependencyInjection

**Registration points**: Application startup, test setup

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// In application startup
var services = new ServiceCollection();
services.ConfigureSnitcher();
var serviceProvider = services.BuildServiceProvider();
```

**Realistic example**:
```csharp
// In Program.cs or application startup
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Configure logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    // Configure Snitcher with custom options
    services.ConfigureSnitcher(options =>
    {
        options.DatabaseProvider = "sqlite";
        options.DatabasePath = @"C:\Data\MySnitcher.db";
        options.EnableSensitiveDataLogging = true;
        options.EnableDetailedErrors = true;
    });

    // Build service provider
    var serviceProvider = services.BuildServiceProvider();

    // Initialize database
    await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);

    return serviceProvider;
}
```

**Testing configuration example**:
```csharp
// In test setup
public async Task<TestContext> CreateTestContext()
{
    var services = new ServiceCollection();
    
    services.ConfigureSnitcher(options =>
    {
        options.DatabaseProvider = "inmemory";
        options.DatabaseName = Guid.NewGuid().ToString(); // Unique per test
    });

    var serviceProvider = services.BuildServiceProvider();
    await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);

    return new TestContext(serviceProvider);
}
```

**Custom database provider example**:
```csharp
// Using SQL Server instead of SQLite
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "custom";
    options.ConfigureDbContext = builder => 
        builder.UseSqlServer(connectionString);
});
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Not initializing database
var services = new ServiceCollection();
services.ConfigureSnitcher();
var serviceProvider = services.BuildServiceProvider();
// Database doesn't exist - operations will fail

// WRONG: Using unsupported provider
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "mysql"; // Not supported
});

// WRONG: Custom provider without configuration
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "custom";
    // Missing ConfigureDbContext - will throw exception
});

// WRONG: Multiple configurations
services.ConfigureSnitcher(); // Configures SQLite
services.ConfigureSnitcher(options => 
    options.DatabaseProvider = "inmemory"); // Overwrites previous config
```

**How to test this in isolation**:
```csharp
[Test]
public async Task ConfigureSnitcher_ShouldRegisterAllServices()
{
    // Arrange
    var services = new ServiceCollection();
    
    // Act
    services.ConfigureSnitcher();
    var serviceProvider = services.BuildServiceProvider();
    
    // Assert
    Assert.That(serviceProvider.GetRequiredService<IWorkspaceService>(), Is.Not.Null);
    Assert.That(serviceProvider.GetRequiredService<IProjectService>(), Is.Not.Null);
    Assert.That(serviceProvider.GetRequiredService<IWorkspaceRepository>(), Is.Not.Null);
    Assert.That(serviceProvider.GetRequiredService<IProjectRepository>(), Is.Not.Null);
    Assert.That(serviceProvider.GetRequiredService<SnitcherDbContext>(), Is.Not.Null);
}

[Test]
public async Task InitializeDatabase_ShouldCreateDatabase()
{
    // Arrange
    var services = new ServiceCollection();
    services.ConfigureSnitcher(options => 
        options.DatabaseProvider = "inmemory");
    var serviceProvider = services.BuildServiceProvider();
    
    // Act
    await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);
    
    // Assert
    var context = serviceProvider.GetRequiredService<SnitcherDbContext>();
    var canConnect = await context.Database.CanConnectAsync();
    Assert.That(canConnect, Is.True);
}
```

**How to mock or replace it**:
```csharp
// For testing, you can bypass configuration and register services manually
public static IServiceCollection ConfigureTestServices(this IServiceCollection services)
{
    // Register in-memory database
    services.AddDbContext<SnitcherDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
    
    // Register mock services
    services.AddScoped<IWorkspaceService, MockWorkspaceService>();
    services.AddScoped<IProjectService, MockProjectService>();
    
    return services;
}

// Or use test-specific configuration
public class TestConfiguration
{
    public static IServiceProvider CreateTestProvider()
    {
        var services = new ServiceCollection();
        services.ConfigureSnitcher(options =>
        {
            options.DatabaseProvider = "inmemory";
            options.DatabaseName = $"Test_{Guid.NewGuid()}";
        });
        return services.BuildServiceProvider();
    }
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new properties to SnitcherOptions for configuration
2. Add new service registrations in ConfigureSnitcher method
3. Add new database provider support if needed
4. Update InitializeDatabaseAsync for new requirements
5. Add validation for new configuration options

**Where NOT to add logic**:
- Don't add business logic or validation
- Don't add UI-specific configuration
- Don't add runtime decision making based on business rules
- Don't add application-specific service logic

**Safe extension points**:
- New SnitcherOptions properties
- Additional database providers
- Extra service registrations
- Enhanced database initialization
- Configuration validation

**Common mistakes**:
- Adding too many unrelated services
- Making configuration too complex
- Not handling configuration errors gracefully
- Forgetting to register new services
- Hardcoding configuration values

**Refactoring warnings**:
- Changing method signatures breaks calling code
- Removing service registrations breaks application
- Modifying options properties affects configuration
- Changing database initialization affects startup

## Failure Modes & Debugging

**Common runtime errors**:
- **ArgumentException**: Invalid database provider or missing configuration
- **InvalidOperationException**: Service registration conflicts
- **DbUpdateException**: Database initialization failures
- **FileNotFoundException**: Database path issues

**Null/reference risks**:
- ConfigureDbContext action can be null (validated)
- DatabasePath can be null (handled with default)
- ServiceCollection validated by extension method

**Performance risks**:
- Database initialization can be slow with large migrations
- Service registration overhead is minimal
- Connection string resolution is lightweight

**Logging points**:
- Database initialization errors logged to debug output
- Service registration failures throw immediately
- Database connection issues logged by EF Core

**How to debug step-by-step**:
1. Set breakpoint in ConfigureSnitcher to trace registration
2. Check SnitcherOptions values after configuration
3. Monitor database initialization in InitializeDatabaseAsync
4. Verify service resolution after configuration
5. Test database connectivity after initialization

## Cross-References

**Related classes**:
- SnitcherOptions (configuration object)
- SnitcherDbContext (database context)
- All service and repository interfaces
- Entity Framework configuration

**Upstream callers**:
- Application startup code
- Test setup code
- Hosting configuration

**Downstream dependencies**:
- All registered services
- Database infrastructure
- Logging framework

**Documents that should be read before/after**:
- Read: SnitcherOptions documentation
- Read: SnitcherDbContext documentation
- Read: Service layer documentation
- Read: Repository layer documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Centralized configuration pattern
- Extension method based service registration
- Database provider abstraction
- Options pattern for configuration
- Startup initialization patterns

**What is project-specific**:
- Specific service types registered
- Particular database providers supported
- Snitcher-specific configuration options
- Database initialization strategy

**How to recreate this pattern from scratch elsewhere**:
1. Create static configuration class with extension methods
2. Define options class for configuration parameters
3. Implement provider selection logic
4. Register all required services
5. Configure database context based on options
6. Add database initialization method
7. Include comprehensive error handling

**Key insights for implementation**:
- Use extension methods for fluent configuration
- Provide sensible defaults for all options
- Support multiple environments through configuration
- Include database initialization in configuration
- Make configuration testable with in-memory options
- Handle errors gracefully with fallbacks
