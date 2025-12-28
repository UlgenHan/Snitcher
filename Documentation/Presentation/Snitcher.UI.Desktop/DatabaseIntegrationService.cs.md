# DatabaseIntegrationService.cs

## Overview

`DatabaseIntegrationService` is the critical bridge between the UI layer and the application's clean architecture database layer. This service maps UI models to database entities and provides a simplified, async-friendly API for workspace and project operations. It implements the Repository pattern at the UI level while delegating actual database operations to the underlying service layer.

**Why it exists**: To abstract the complexity of the clean architecture layers from the UI, provide type-safe mapping between UI models and database entities, and offer a single point of coordination for all database operations needed by the UI.

**What problem it solves**: Eliminates direct dependencies between UI and database layers, provides consistent error handling and logging for all data operations, and centralizes the mapping logic between different model representations.

**What would break if removed**: The UI would have no way to access database operations, requiring direct dependencies on all application layers and breaking the clean architecture separation.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0
- Microsoft.Extensions.DependencyInjection 8.0.1
- Microsoft.Extensions.Logging 8.0.1

**Libraries**:
- System.Collections.Generic (Collections)
- System.Linq (Query operations)
- System.Threading.Tasks (Async operations)

**Persistence**: Indirect via Snitcher.Service layer interfaces

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Async/await support, dependency injection container

## Architectural Role

**Layer**: Presentation/Infrastructure (Database Integration)

**Responsibility Boundaries**:
- MUST map between UI models and database entities
- MUST coordinate database operations for UI
- MUST provide consistent error handling
- MUST NOT implement business logic
- MUST NOT access database directly

**What it MUST do**:
- Convert UI models to database entities
- Call appropriate service layer methods
- Handle and log database errors
- Provide async methods for UI operations
- Initialize default data structures

**What it MUST NOT do**:
- Implement validation rules
- Perform business calculations
- Access EF Core directly
- Handle UI-specific logic

**Dependencies (Incoming)**: UI ViewModels (SnitcherMainViewModel)

**Dependencies (Outgoing)**: IWorkspaceService, IProjectService, ILogger

## Execution Flow

**Where execution starts**: Constructor called via DI container when first requested

**How control reaches this component**:
1. UI ViewModel calls method (e.g., GetWorkspacesAsync)
2. Service maps UI parameters to domain parameters
3. Service layer method called
4. Results mapped back to UI models
5. Results returned to UI

**Call sequence example (GetWorkspacesAsync)**:
1. UI calls `GetWorkspacesAsync()`
2. Service calls `_workspaceService.GetAllWorkspacesAsync()`
3. Results mapped via `MapToWorkspaceModel()`
4. UI models returned to caller

**Synchronous vs asynchronous behavior**: All public methods are async to prevent UI blocking

**Threading/Dispatcher notes**: No UI thread operations - pure data layer coordination

**Lifecycle**: Singleton service - lives for application duration

## Public API / Surface Area

**Interface**: `IDatabaseIntegrationService`

**Constructors**:
```csharp
public DatabaseIntegrationService(
    IWorkspaceService workspaceService,
    IProjectService projectService,
    ILogger<DatabaseIntegrationService> logger)
```

**Workspace Operations**:
- `Task<IEnumerable<Workspace>> GetWorkspacesAsync()` - Get all workspaces
- `Task<Workspace?> GetWorkspaceAsync(string id)` - Get specific workspace
- `Task<Workspace> CreateWorkspaceAsync(string name, string description = "")` - Create workspace
- `Task<Workspace> UpdateWorkspaceAsync(Workspace workspace)` - Update workspace
- `Task<bool> DeleteWorkspaceAsync(string id)` - Delete workspace

**Project Operations**:
- `Task<IEnumerable<Project>> GetProjectsAsync(string workspaceId)` - Get workspace projects
- `Task<Project> CreateProjectAsync(string workspaceId, string name, string description = "", string path = "")` - Create project
- `Task<bool> DeleteProjectAsync(string projectId)` - Delete project

**Namespace Operations** (Deprecated):
- `Task<IEnumerable<Namespace>> GetNamespacesAsync(string workspaceId)` - Returns empty
- `Task<Namespace> CreateNamespaceAsync(...)` - Throws NotSupportedException
- `Task<bool> DeleteNamespaceAsync(string namespaceId)` - Throws NotSupportedException

**Search Operations**:
- `Task<SearchResults> SearchAsync(string searchTerm)` - Search workspaces and projects

**Initialization**:
- `Task InitializeAsync()` - Initialize service and ensure default workspace

**Expected Input/Output**: Methods take UI model parameters and return UI model results; all operations are async.

**Side Effects**:
- Modifies database via service calls
- Creates default workspace if none exists
- Logs all operations and errors

**Error Behavior**: All methods wrap service calls in try-catch, log errors, and return appropriate default values (empty collections, null objects, or false booleans).

## Internal Logic Breakdown

**Initialization Pattern** (lines 41-57):
```csharp
public async Task InitializeAsync()
{
    try
    {
        _logger.LogInformation("Initializing database service...");
        await EnsureDefaultWorkspaceAsync();
        _logger.LogInformation("Database service initialized successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to initialize database service");
        throw;
    }
}
```

**Mapping Pattern** (lines 433-446):
```csharp
private static Workspace MapToWorkspaceModel(Core.Entities.Workspace entity)
{
    return new Workspace
    {
        Id = entity.Id.ToString(),
        Name = entity.Name,
        Description = entity.Description ?? "",
        IsDefault = entity.IsDefault,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Projects = new ObservableCollection<Project>(),
        Namespaces = new ObservableCollection<Namespace>()
    };
}
```

**Error Handling Pattern** (consistent across all methods):
```csharp
try
{
    // Database operation
    var result = await _service.SomeOperationAsync();
    return MapToUiModel(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to operation");
    return defaultValue; // empty collection, null, or false
}
```

**Search Implementation** (lines 325-377):
- Gets ALL workspaces first to enable project searching
- Filters workspaces by search term
- Iterates through all workspaces to get projects
- Filters projects by search term
- Returns combined results

**Default Workspace Creation** (lines 403-426):
- Checks for existing default workspace
- Creates "Default Workspace" if none exists
- Sets as default workspace
- Handles creation errors gracefully

## Patterns & Principles Used

**Adapter Pattern**: Adapts the clean architecture service layer to UI-specific needs

**Repository Pattern**: Provides repository-like interface for UI layer

**Mapper Pattern**: Converts between domain entities and UI models

**Async/Await Pattern**: All operations are non-blocking

**Error Handling Pattern**: Consistent try-catch with logging

**Why these patterns were chosen**:
- Adapter to isolate UI from architecture complexity
- Repository for familiar data access pattern
- Mapper to handle model representation differences
- Async for responsive UI
- Consistent error handling for reliability

**Trade-offs**:
- Additional mapping layer adds complexity
- Potential performance overhead from mapping
- Duplicate error handling code

**Anti-patterns avoided**:
- No direct database access
- No business logic in service
- No synchronous blocking operations
- No unhandled exceptions

## Binding / Wiring / Configuration

**Dependency Injection**:
- Registered as scoped service in App.axaml.cs
- Constructor injection of dependencies
- Service lifetime matches UI needs

**Data Binding**: None (pure service layer)

**Configuration Sources**:
- No external configuration needed
- Behavior driven by method parameters

**Runtime Wiring**:
- Resolved from DI container by ViewModels
- Service layer dependencies injected automatically
- Logger configured by application startup

**Registration Points**:
- Registered in App.axaml.cs line 91
- Dependencies resolved via constructor injection

## Example Usage

**Minimal Example**:
```csharp
// Get all workspaces
var workspaces = await databaseService.GetWorkspacesAsync();

// Create new workspace
var workspace = await databaseService.CreateWorkspaceAsync("My Workspace", "Description");
```

**Realistic Example**:
```csharp
// Create project in workspace
var project = await databaseService.CreateProjectAsync(
    workspaceId, 
    "New Project", 
    "Project description", 
    @"C:\Projects\NewProject"
);

// Search across all data
var results = await databaseService.SearchAsync("search term");
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't access database directly
var context = new SnitcherDbContext();
var workspaces = context.Workspaces.ToList();

// BAD - Don't use synchronous methods
var workspaces = databaseService.GetWorkspaces(); // Method doesn't exist
```

**How to test in isolation**:
```csharp
// Mock dependencies
var mockWorkspaceService = new Mock<IWorkspaceService>();
var mockProjectService = new Mock<IProjectService>();
var mockLogger = new Mock<ILogger<DatabaseIntegrationService>>();

var service = new DatabaseIntegrationService(
    mockWorkspaceService.Object,
    mockProjectService.Object,
    mockLogger.Object);

// Test methods
var workspaces = await service.GetWorkspacesAsync();
mockWorkspaceService.Verify(x => x.GetAllWorkspacesAsync(), Times.Once);
```

**How to mock or replace**:
- Mock IWorkspaceService and IProjectService for data operations
- Mock ILogger for testing logging behavior
- Create test doubles for specific scenarios

## Extension & Modification Guide

**How to add new entity support**:
1. Add new service interface dependency
2. Add new public methods for CRUD operations
3. Implement mapping methods for new entity
4. Add error handling following existing pattern
5. Update SearchAsync to include new entity

**Where NOT to add logic**:
- Don't add business validation rules
- Don't implement UI-specific logic
- Don't add direct database access

**Safe extension points**:
- New entity operations following existing patterns
- Enhanced search functionality
- Additional mapping methods
- Batch operations for performance

**Common mistakes**:
- Forgetting to handle async properly
- Not mapping entities correctly
- Missing error handling in new methods
- Not updating search functionality

**Refactoring warnings**:
- Mapping logic can become complex
- Search method may need optimization for large datasets
- Error handling is repetitive - consider base class
- Consider caching for frequently accessed data

## Failure Modes & Debugging

**Common runtime errors**:
- ArgumentException for invalid IDs
- NullReferenceException when service dependencies missing
- TaskCanceledException for timeout scenarios

**Null/reference risks**:
- Service dependencies may be null - constructor validation prevents this
- Search results may be null - returns empty SearchResults
- Entity mapping may fail if entities null - handled in mapping methods

**Performance risks**:
- SearchAsync loads all workspaces and projects - may be slow with large datasets
- Mapping overhead for large collections
- Memory usage with large object graphs

**Logging points**:
- All service method calls logged with parameters
- Success/failure of database operations
- Initialization progress and errors
- Search operation statistics

**How to debug step-by-step**:
1. Set breakpoint in method being debugged
2. Check incoming parameters
3. Step through service layer call
4. Verify mapping results
5. Check return value before returning to caller

## Cross-References

**Related classes**:
- `IWorkspaceService` - Workspace operations
- `IProjectService` - Project operations
- `Workspace` (UI model) - UI representation
- `Project` (UI model) - UI representation
- `Core.Entities.Workspace` - Database entity
- `Core.Entities.ProjectEntity` - Database entity

**Upstream callers**:
- `SnitcherMainViewModel` - Primary consumer
- Other ViewModels (potential future use)

**Downstream dependencies**:
- Service layer interfaces
- Entity Framework Core (via services)
- Logging infrastructure

**Documents to read before/after**:
- Before: `SnitcherMainViewModel.cs` (consumer)
- After: Service layer interfaces
- After: Entity model documentation

## Knowledge Transfer Notes

**Reusable concepts**:
- Adapter pattern for layer isolation
- Async repository pattern implementation
- Consistent error handling strategy
- Entity mapping between layers
- Service layer abstraction

**Project-specific elements**:
- Snitcher workspace/project domain
- Simplified architecture (namespaces deprecated)
- Search across multiple entity types
- Default workspace initialization

**How to recreate pattern elsewhere**:
1. Define interface for service operations
2. Implement constructor injection of dependencies
3. Create mapping methods between model types
4. Implement all methods as async with error handling
5. Add logging for all operations
6. Handle edge cases (null values, invalid IDs)

**Key insights**:
- Always map between layers to maintain separation
- Handle errors consistently and provide meaningful defaults
- Use async throughout to prevent blocking
- Log both success and failure cases for debugging
- Initialize required data structures in InitializeAsync
- Consider performance implications of search operations
