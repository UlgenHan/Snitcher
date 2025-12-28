# App.axaml.cs

## Overview

App.axaml.cs is the main application class for the Snitcher desktop application built with Avalonia UI. It serves as the central bootstrap point, configuring dependency injection, setting up the main window, and initializing all application services. This class orchestrates the entire application startup sequence and establishes the foundation for the MVVM architecture.

**Why it exists**: To provide a centralized location for application initialization, dependency injection setup, and main window configuration. It ensures all services are properly registered and the application starts with the correct infrastructure in place.

**Problem it solves**: Without this centralized bootstrap, service registration would be scattered, dependency injection would be incomplete, and the application would lack a unified startup sequence, leading to potential runtime failures and maintenance issues.

**What would break if removed**: The entire application would fail to start. No services would be registered, the main window wouldn't be created, dependency injection would not work, and the UI framework wouldn't be properly initialized.

## Tech Stack Identification

**Languages used**: C# 12.0, XAML

**Frameworks**: .NET 8.0, Avalonia UI 11.0, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting, Microsoft.Extensions.Logging

**Libraries**: Microsoft.EntityFrameworkCore, Snitcher.Service, Snitcher.Repository, Snitcher.Sniffer

**UI frameworks**: Avalonia UI (cross-platform desktop UI)

**Persistence / communication technologies**: Entity Framework Core, SQLite, HTTP proxy infrastructure

**Build tools**: MSBuild, Avalonia build tools

**Runtime assumptions**: .NET 8.0 runtime, Avalonia UI runtime, desktop environment

**Version hints**: Uses modern Avalonia 11.0 patterns, .NET 8.0 dependency injection, async initialization

## Architectural Role

**Layer**: Presentation Layer (Application Bootstrap)

**Responsibility boundaries**:
- MUST configure dependency injection container
- MUST set up main window and view model
- MUST initialize all application services
- MUST configure logging infrastructure
- MUST NOT contain business logic
- MUST NOT handle UI-specific interactions

**What it MUST do**:
- Register all services in DI container
- Configure database and persistence
- Set up main window with MVVM pattern
- Initialize logging infrastructure
- Handle application lifecycle events
- Configure UI-specific services

**What it MUST NOT do**:
- Implement business rules or validation
- Handle user interactions directly
- Perform data operations
- Contain presentation logic beyond setup

**Dependencies (incoming)**: Avalonia UI framework, operating system

**Dependencies (outgoing)**: All service layers, UI frameworks, infrastructure components

## Execution Flow

**Where execution starts**: App class is instantiated by Avalonia framework during application startup.

**How control reaches this component**:
1. Operating system launches application
2. Avalonia framework creates App instance
3. Initialize() method called for XAML loading
4. OnFrameworkInitializationCompleted() called for setup
5. ConfigureServices() called to set up DI
6. Main window created and shown

**Call sequence (step-by-step)**:
1. App constructor called by framework
2. Initialize() loads XAML resources
3. OnFrameworkInitializationCompleted() checks application lifetime
4. ConfigureServices() builds service container
5. Main window instantiated with DI
6. View model resolved from container
7. Main window shown to user
8. Database initialization started in background

**Synchronous vs asynchronous behavior**: Configuration is synchronous, database initialization is asynchronous

**Threading / dispatcher / event loop notes**: UI thread handles initialization, database initialization runs on background thread

**Lifecycle**: Created → Initialized → Configured → Running → Shutdown

## Public API / Surface Area

**Constructors**:
- `public App()`: Default constructor called by Avalonia framework

**Protected Methods**:
- `override void Initialize()`: Load XAML resources and themes
- `override void OnFrameworkInitializationCompleted()`: Set up main application structure

**Private Methods**:
- `IServiceProvider ConfigureServices()`: Configure dependency injection and services
- `void DisableAvaloniaDataAnnotationValidation()`: Placeholder for validation configuration

**Properties**:
- `static IServiceProvider? ServiceProvider`: Access to service container throughout application

**Expected input/output**:
- Input: Framework initialization events
- Output: Configured application with all services running

**Side effects**:
- Registers all services in DI container
- Creates and shows main window
- Initializes database in background
- Sets up logging infrastructure

**Error behavior**: Throws exceptions during configuration if services fail to register, handles database initialization errors gracefully

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Class definition and service provider (lines 30-33)**:
```csharp
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
```
- Partial class for XAML code-behind integration
- Private service provider for DI container access
- Enables service resolution throughout application

**Initialize method (lines 34-37)**:
```csharp
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
}
```
- Loads XAML resources and themes
- Called automatically by Avalonia framework
- Sets up UI styling and resources

**OnFrameworkInitializationCompleted method (lines 39-58)**:
```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        // DisableAvaloniaDataAnnotationValidation();
        
        // Setup dependency injection
        _serviceProvider = ConfigureServices();
        
        // Create and show main window with DI
        desktop.MainWindow = new SnitcherMainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<SnitcherMainViewModel>()
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```
- Checks for desktop application lifetime
- Sets up dependency injection container
- Creates main window with view model from DI
- Establishes MVVM pattern from startup
- Commented validation disabling for future use

**ConfigureServices method (lines 60-150)**:
```csharp
private IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Configure logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddDebug();
#if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
#else
        builder.SetMinimumLevel(LogLevel.Information);
#endif
    });

    // Configure Snitcher application stack with SQLite database
    services.ConfigureSnitcher(options =>
    {
        options.DatabaseProvider = "sqlite";
        options.DatabasePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Snitcher",
            "snitcher.db");
#if DEBUG
        options.EnableSensitiveDataLogging = true;
        options.EnableDetailedErrors = true;
#endif
    });
```
- Creates new ServiceCollection for DI
- Configures logging with console and debug output
- Sets different log levels for debug vs release
- Configures Snitcher stack with SQLite database
- Sets database path to user's AppData directory
- Enables sensitive data logging in debug mode

**UI services registration (lines 90-94)**:
```csharp
// Register UI services
services.AddScoped<IDatabaseIntegrationService, DatabaseIntegrationService>();
services.AddTransient<SnitcherMainViewModel>();
services.AddTransient<MainApplicationWindowViewModel>();
```
- Registers UI-specific services
- Database integration service bridges UI and data layers
- Main view models registered for DI resolution

**Core ViewModels registration (lines 96-98)**:
```csharp
// Register Core ViewModels
services.AddTransient<WelcomeViewModel>();
services.AddTransient<ExtensionsViewModel>();
```
- Registers core application view models
- Transient lifetime for view models (new instance per resolution)

**Feature-specific ViewModels (lines 100-120)**:
```csharp
// Register Domain-specific ViewModels and Services
if (UIConfiguration.Features.EnableRequestBuilder)
{
    services.AddTransient<RequestBuilderViewModel>();
    services.AddTransient<IRequestSender, RequestSender>();
}

if (UIConfiguration.Features.EnableCollections)
{
    services.AddTransient<CollectionsExplorerViewModel>();
}

if (UIConfiguration.Features.EnableAutomation)
{
    services.AddTransient<AutomationWorkflowViewModel>();
}

if (UIConfiguration.Features.EnableWorkspaceManagement)
{
    services.AddTransient<WorkspaceManagerViewModel>();
}
```
- Conditionally registers features based on configuration
- Enables modular feature activation
- Registers both view models and related services
- Follows feature toggle pattern

**Proxy services registration (lines 122-131)**:
```csharp
// Register Proxy Inspector services
if (UIConfiguration.Features.EnableHttpsInterception)
{
    services.AddSingleton<ICertificateManager, CertificateManager>();
    services.AddSingleton<Snitcher.Sniffer.Core.Interfaces.ILogger>(provider => 
        new SnitcherLoggerAdapter(provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SnitcherLoggerAdapter>>()));
    services.AddSingleton<IProxyService, ProxyService>();
    services.AddTransient<ProxyInspectorViewModel>();
    services.AddSingleton<IFlowMapper, FlowMapperService>();
}
```
- Registers proxy interception services
- Sets up certificate management for HTTPS
- Adapts logging between different logging systems
- Registers proxy service and view model
- Singleton lifetime for shared proxy resources

**Service provider creation and database initialization (lines 133-149)**:
```csharp
// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Initialize database
_ = Task.Run(async () =>
{
    try
    {
        await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogError(ex, "Failed to initialize database");
    }
});

return serviceProvider;
```
- Builds service provider from registration
- Starts database initialization on background thread
- Handles database initialization errors gracefully
- Logs errors but doesn't fail application startup

**Static service provider access (lines 160-162)**:
```csharp
/// <summary>
/// Gets the service provider for accessing services throughout the application.
/// </summary>
public static IServiceProvider? ServiceProvider => (Current as App)?._serviceProvider;
```
- Provides static access to service provider
- Enables service resolution from anywhere in application
- Used for manual service resolution when needed

**Algorithms used**:
- Dependency injection container building
- Service lifetime management
- Conditional feature registration
- Background task initialization

**Conditional logic explanation**:
- Debug vs release configuration affects logging levels
- Feature toggles control service registration
- Desktop lifetime check ensures proper UI setup
- Background initialization prevents UI blocking

**State transitions**:
- App Created → XAML Loaded → Services Configured → Main Window Shown → Database Initialized

**Important invariants**:
- All services registered before main window creation
- Database initialization doesn't block UI startup
- Feature configuration controls available functionality
- Service provider available throughout application lifetime

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Dependency Injection Pattern**: Service registration and resolution
- **Service Locator Pattern**: Static access to service provider
- **Factory Pattern**: Service provider creates view models
- **Observer Pattern**: Application lifetime events

**Architectural patterns**:
- **MVVM Pattern**: View models bound to views
- **Clean Architecture**: Dependency direction from UI to domain
- **Configuration as Code**: Programmatic service registration
- **Feature Toggle Pattern**: Conditional feature activation

**Why these patterns were chosen (inferred)**:
- Dependency injection enables testability and loose coupling
- MVVM separates UI from business logic
- Feature toggles enable modular development
- Service locator provides convenient access to DI

**Trade-offs**:
- Service locator vs constructor injection: More convenient but less explicit
- Conditional registration vs always registered: More flexible but complex
- Background database init vs synchronous: Better UX but more complex

**Anti-patterns avoided or possibly introduced**:
- Avoided: Hard-coded service dependencies
- Avoided: Scattered configuration code
- Possible risk: Service locator overuse

## Binding / Wiring / Configuration

**Dependency injection**: Configures entire DI container for application

**Data binding**: Main window data-bound to SnitcherMainViewModel

**Configuration sources**: Code-based configuration, UIConfiguration feature flags

**Runtime wiring**: Microsoft.Extensions.DependencyInjection, Avalonia DI integration

**Registration points**: Application startup, feature configuration

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Accessing services from anywhere in the application
var logger = App.ServiceProvider?.GetService<ILogger<SomeClass>>();
var workspaceService = App.ServiceProvider?.GetService<IWorkspaceService>();
```

**Realistic example**:
```csharp
public class SomeViewModel
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<SomeViewModel> _logger;
    
    // Constructor injection (preferred)
    public SomeViewModel(IWorkspaceService workspaceService, ILogger<SomeViewModel> logger)
    {
        _workspaceService = workspaceService;
        _logger = logger;
    }
    
    // Service locator access (when injection not possible)
    public void SomeMethod()
    {
        var proxyService = App.ServiceProvider?.GetService<IProxyService>();
        if (proxyService != null)
        {
            // Use proxy service
        }
    }
}
```

**Feature configuration example**:
```csharp
public static class UIConfiguration
{
    public static class Features
    {
        public const bool EnableRequestBuilder = true;
        public const bool EnableCollections = true;
        public const bool EnableAutomation = false;
        public const bool EnableWorkspaceManagement = true;
        public const bool EnableHttpsInterception = true;
    }
}
```

**Database initialization handling**:
```csharp
// In App.axaml.cs - database initialization is already handled
// But you can access the database status:
public async Task CheckDatabaseStatusAsync()
{
    var dbService = App.ServiceProvider?.GetService<IDatabaseIntegrationService>();
    if (dbService != null)
    {
        await dbService.InitializeAsync();
        // Database is ready
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Accessing services before App is fully initialized
public class EarlyInitializer
{
    public void Initialize()
    {
        var service = App.ServiceProvider?.GetService<ISomeService>(); // Returns null
    }
}

// WRONG: Modifying service provider after creation
var services = new ServiceCollection();
// ... register services
var provider = services.BuildServiceProvider();
// Can't add more services after BuildServiceProvider() called

// WRONG: Assuming all features are enabled
var automationVm = App.ServiceProvider?.GetService<AutomationWorkflowViewModel>();
// Returns null if EnableAutomation is false

// WRONG: Blocking on database initialization
// Don't do this - it's already handled asynchronously
await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);
```

**How to test this in isolation**:
```csharp
[Test]
public void App_ShouldConfigureAllServices()
{
    // Arrange
    var app = new App();
    
    // Act
    var serviceProvider = app.ConfigureServices();
    
    // Assert
    Assert.That(serviceProvider, Is.Not.Null);
    Assert.That(serviceProvider.GetService<IWorkspaceService>(), Is.Not.Null);
    Assert.That(serviceProvider.GetService<IProjectService>(), Is.Not.Null);
    Assert.That(serviceProvider.GetService<SnitcherMainViewModel>(), Is.Not.Null);
}

[Test]
public void App_ShouldRespectFeatureConfiguration()
{
    // Arrange
    var originalAutomationEnabled = UIConfiguration.Features.EnableAutomation;
    UIConfiguration.Features.EnableAutomation = false;
    
    var app = new App();
    
    try
    {
        // Act
        var serviceProvider = app.ConfigureServices();
        
        // Assert
        Assert.That(serviceProvider.GetService<AutomationWorkflowViewModel>(), Is.Null);
    }
    finally
    {
        // Cleanup
        UIConfiguration.Features.EnableAutomation = originalAutomationEnabled;
    }
}
```

**How to mock or replace it**:
```csharp
// For testing UI components, create test service provider
public static class TestApp
{
    public static IServiceProvider CreateTestProvider()
    {
        var services = new ServiceCollection();
        
        // Register test services
        services.AddTransient<IWorkspaceService, MockWorkspaceService>();
        services.AddTransient<SnitcherMainViewModel, TestMainViewModel>();
        
        // Configure in-memory database
        services.ConfigureSnitcher(options => 
            options.DatabaseProvider = "inmemory");
        
        return services.BuildServiceProvider();
    }
}

// Use in UI tests
[Test]
public void MainWindow_ShouldLoadCorrectly()
{
    var serviceProvider = TestApp.CreateTestProvider();
    var viewModel = serviceProvider.GetRequiredService<SnitcherMainViewModel>();
    
    var window = new SnitcherMainWindow
    {
        DataContext = viewModel
    };
    
    Assert.That(window.DataContext, Is.Not.Null);
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add feature flag to UIConfiguration.Features
2. Register new view models and services in ConfigureServices
3. Add conditional registration based on feature flag
4. Update main window or navigation to include new feature
5. Add any required infrastructure services

**Where NOT to add logic**:
- Don't add business logic or validation
- Don't add UI interaction handling
- Don't add data processing logic
- Don't add user-specific configuration

**Safe extension points**:
- New service registrations
- Additional logging configuration
- Enhanced database setup
- New feature flags and conditional registration
- Additional infrastructure services

**Common mistakes**:
- Adding too many services to App class
- Registering services with wrong lifetimes
- Forgetting to handle database initialization errors
- Making feature configuration too complex
- Blocking UI thread on startup

**Refactoring warnings**:
- Changing service lifetimes affects application behavior
- Removing service registrations breaks dependent code
- Modifying feature flags affects available functionality
- Changing database configuration affects persistence

## Failure Modes & Debugging

**Common runtime errors**:
- **InvalidOperationException**: Service registration conflicts or missing services
- **ArgumentNullException**: Missing required services in constructors
- **DbUpdateException**: Database initialization failures
- **TypeLoadException**: Missing dependencies or version conflicts

**Null/reference risks**:
- ServiceProvider is null until configuration complete
- Services may not be registered if features disabled
- Database initialization failures handled gracefully

**Performance risks**:
- Too many services registered increases startup time
- Database initialization blocking UI thread (avoid this)
- Large service graphs increase memory usage
- Incorrect service lifetimes causing memory leaks

**Logging points**:
- Application startup and service registration
- Database initialization success/failure
- Service resolution failures
- Application shutdown events

**How to debug step-by-step**:
1. Set breakpoint in ConfigureServices to trace service registration
2. Check service provider after configuration
3. Monitor database initialization in background task
4. Verify main window creation and view model resolution
5. Test feature toggle behavior

## Cross-References

**Related classes**:
- SnitcherMainViewModel (main view model)
- SnitcherMainWindow (main window)
- SnitcherConfiguration (service configuration)
- UIConfiguration (feature configuration)

**Upstream callers**:
- Avalonia UI framework
- Operating system launcher
- Application host

**Downstream dependencies**:
- All registered services
- UI components and views
- Database infrastructure

**Documents that should be read before/after**:
- Read: SnitcherMainViewModel documentation
- Read: SnitcherConfiguration documentation
- Read: UIConfiguration documentation
- Read: Service layer documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Avalonia application bootstrap pattern
- Dependency injection configuration
- Feature toggle implementation
- MVVM application setup
- Background initialization patterns

**What is project-specific**:
- Specific service registrations
- Snitcher feature set
- Database configuration details
- Particular UI framework integration

**How to recreate this pattern from scratch elsewhere**:
1. Create Application class inheriting from framework base
2. Override initialization methods for setup
3. Configure dependency injection container
4. Register all required services with appropriate lifetimes
5. Set up main window with MVVM pattern
6. Initialize infrastructure on background threads
7. Provide static access to service container
8. Handle initialization errors gracefully

**Key insights for implementation**:
- Always use dependency injection for testability
- Initialize heavy operations on background threads
- Use feature toggles for modular development
- Provide graceful error handling for infrastructure
- Keep App class focused on configuration only
- Use appropriate service lifetimes (Scoped, Transient, Singleton)
