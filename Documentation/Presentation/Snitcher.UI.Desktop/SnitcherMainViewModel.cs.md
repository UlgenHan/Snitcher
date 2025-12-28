# SnitcherMainViewModel.cs

## Overview

`SnitcherMainViewModel` is the central ViewModel for the Snitcher desktop application's main window. It implements the MVVM pattern using CommunityToolkit.Mvvm and serves as the primary orchestrator for workspace management, project operations, and user interactions. This ViewModel bridges the UI with the database integration service, handling all CRUD operations, search functionality, and UI state management.

**Why it exists**: To provide a clean separation between UI logic and business operations while managing the complex state of workspaces, projects, and user interactions in a responsive, testable manner.

**What problem it solves**: Eliminates code-behind logic, centralizes data operations, provides reactive UI updates through observable properties, and implements proper async/await patterns for database operations.

**What would break if removed**: The main window would have no functionality - no data loading, no workspace/project management, no search capability, and no user interaction handling.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0
- Avalonia UI 11.3.10 (via CommunityToolkit.Mvvm integration)
- Microsoft.Extensions.Logging 8.0.1

**Libraries**:
- CommunityToolkit.Mvvm 8.2.1 (ObservableObject, RelayCommand, ObservableProperty)
- System.Collections.ObjectModel (ObservableCollection)
- Avalonia.Controls (Window management)

**UI Framework**: Avalonia UI with MVVM pattern

**Persistence**: Indirect via IDatabaseIntegrationService

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Desktop environment with async/await support

## Architectural Role

**Layer**: Presentation Layer (ViewModel)

**Responsibility Boundaries**:
- MUST manage UI state and user interactions
- MUST coordinate with database service for data operations
- MUST NOT implement business logic (delegates to services)
- MUST NOT access database directly
- MUST provide reactive property updates

**What it MUST do**:
- Manage observable collections for UI binding
- Handle user commands and interactions
- Coordinate async database operations
- Provide loading states and status messages
- Implement search functionality
- Manage workspace/project selection state

**What it MUST NOT do**:
- Implement validation rules (delegates to services)
- Access database directly
- Implement business algorithms
- Handle file system operations directly

**Dependencies (Incoming)**: UI (View binds to properties and commands)

**Dependencies (Outgoing)**: IDatabaseIntegrationService, ILogger

## Execution Flow

**Where execution starts**: Constructor called via DI container when main window is created

**How control reaches this component**:
1. App.axaml.cs creates main window
2. DI container resolves SnitcherMainViewModel
3. Constructor injected with dependencies
4. InitializeAsync() called for data loading

**Call sequence**:
1. **Constructor** (lines 61-67):
   - Dependency injection setup
   - InitializeAsync() called
2. **InitializeAsync** (lines 69-90):
   - Set loading state
   - Initialize database service
   - Load initial data
   - Handle errors and final state
3. **LoadDataAsync** (lines 92-142):
   - Clear existing collections
   - Load workspaces from database
   - Load projects for each workspace
   - Load namespaces for each workspace
   - Build recent projects list
   - Select first workspace
4. **Command handlers**: Execute based on user interactions

**Synchronous vs asynchronous behavior**: Constructor is synchronous, but immediately calls async InitializeAsync(). All data operations and command handlers are async.

**Threading/Dispatcher notes**: All database operations run on background threads via async/await. UI property updates automatically marshal to UI thread by ObservableProperty.

**Lifecycle**: Created via DI → Lives for main window duration → Disposed with window

## Public API / Surface Area

**Constructors**:
```csharp
public SnitcherMainViewModel(IDatabaseIntegrationService databaseService, ILogger<SnitcherMainViewModel> logger)
```

**Observable Properties**:
- `Workspaces` - Collection of workspace objects
- `RecentProjects` - Collection of recently used projects
- `Namespaces` - Collection of namespaces (deprecated)
- `SelectedWorkspace` - Currently selected workspace
- `SelectedProject` - Currently selected project
- `SelectedNamespace` - Currently selected namespace
- `SearchTerm` - Current search text
- `IsLoading` - Loading state indicator
- `StatusMessage` - User status feedback
- `ShowSearchResults` - Search results visibility
- `SearchResults` - Search results container
- `IsWorkspaceOpened` - Workspace detail view state

**Computed Properties**:
- `IsNotWorkspaceOpenedAndNotSearching` - UI state helper

**Commands**:
- `CreateWorkspaceCommand` - Create new workspace
- `CreateProjectCommand` - Create new project
- `CreateNamespaceCommand` - Create new namespace (deprecated)
- `OpenWorkspaceCommand` - Open workspace detail view
- `CloseWorkspaceCommand` - Close workspace detail view
- `OpenProjectCommand` - Open project
- `DeleteWorkspaceCommand` - Delete workspace
- `DeleteProjectCommand` - Delete project
- `DeleteNamespaceCommand` - Delete namespace (deprecated)
- `SearchCommand` - Execute search
- `ClearSearchCommand` - Clear search
- `RefreshDataCommand` - Refresh all data

**Expected Input/Output**: Commands take optional parameters (workspace/project objects), properties expose collections and state for UI binding.

**Side Effects**:
- Modifies database via service calls
- Updates observable collections
- Changes UI state properties
- Triggers property change notifications

**Error Behavior**: All commands wrap database calls in try-catch, log errors, and update StatusMessage with user-friendly error messages.

## Internal Logic Breakdown

**Data Loading Pattern** (lines 92-142):
```csharp
private async Task LoadDataAsync()
{
    // Clear collections
    Workspaces.Clear();
    RecentProjects.Clear();
    
    // Load workspaces with nested data
    var workspaces = await _databaseService.GetWorkspacesAsync();
    foreach (var workspace in workspaces.OrderBy(w => w.IsDefault ? 0 : 1).ThenBy(w => w.Name))
    {
        // Load related data for each workspace
        var projects = await _databaseService.GetProjectsAsync(workspace.Id);
        var namespaces = await _databaseService.GetNamespacesAsync(workspace.Id);
        
        // Populate collections
        Workspaces.Add(workspace);
    }
    
    // Build recent projects list
    var recent = Workspaces.SelectMany(w => w.Projects)
        .OrderByDescending(p => p.UpdatedAt)
        .Take(6);
}
```

**Command Pattern Implementation**:
All commands follow consistent pattern:
1. Validate input parameters
2. Show loading status
3. Execute database operation via service
4. Update UI collections
5. Handle errors with logging and user feedback

**Search Implementation** (lines 422-452):
```csharp
[RelayCommand]
public async Task Search()
{
    if (string.IsNullOrWhiteSpace(SearchTerm))
    {
        ShowSearchResults = false;
        return;
    }
    
    IsLoading = true;
    var results = await _databaseService.SearchAsync(SearchTerm);
    SearchResults = results;
    ShowSearchResults = true;
    StatusMessage = $"Found {results.Workspaces.Count} workspace(s) and {results.Projects.Count} project(s)";
}
```

**State Management**:
- Loading states set before async operations
- Status messages provide user feedback
- Collections updated atomically
- Selection state maintained during refreshes

**Error Handling Strategy**:
- All async operations wrapped in try-catch
- Errors logged with full exception details
- User-friendly messages shown in StatusMessage
- Loading state always cleared in finally blocks

## Patterns & Principles Used

**MVVM Pattern**: Clear separation of View and ViewModel, data binding through observable properties

**Command Pattern**: RelayCommand encapsulates user actions, enables binding from UI

**Async/Await Pattern**: Non-blocking database operations, responsive UI

**Observer Pattern**: ObservableProperty automatically notifies UI of changes

**Repository Pattern**: Access to data through IDatabaseIntegrationService abstraction

**Why these patterns were chosen**:
- MVVM for testability and separation of concerns
- Commands for clean UI interaction handling
- Async for responsive user experience
- Observer for reactive UI updates
- Repository for data access abstraction

**Trade-offs**:
- Large ViewModel class with many responsibilities
- Complex state management in single class
- Memory usage due to observable collections

**Anti-patterns avoided**:
- No direct database access
- No UI-specific code in ViewModel
- No synchronous blocking operations
- No hardcoded business logic

## Binding / Wiring / Configuration

**Dependency Injection**:
- Constructor injection of services
- Registered as transient in App.axaml.cs
- Dependencies: IDatabaseIntegrationService, ILogger

**Data Binding**:
- Properties bound to UI elements via ObservableProperty
- Commands bound to buttons and menu items
- Collections bound to lists and grids

**Configuration Sources**:
- No external configuration needed
- Behavior driven by user interactions and database state

**Runtime Wiring**:
- View binds to ViewModel properties in XAML
- Commands triggered by UI interactions
- Service calls made through injected dependencies

**Registration Points**:
- Registered in App.axaml.cs line 92
- Dependencies resolved by DI container

## Example Usage

**Minimal Example**:
```csharp
// Create ViewModel (normally done by DI)
var viewModel = new SnitcherMainViewModel(databaseService, logger);

// Bind to UI in XAML
<TextBox Text="{Binding SearchTerm}"/>
<Button Command="{Binding SearchCommand}"/>
```

**Realistic Example**:
```csharp
// Programmatic interaction
await viewModel.OpenWorkspaceCommand.ExecuteAsync(selectedWorkspace);
await viewModel.CreateProjectCommand.ExecuteAsync(workspace);
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't manipulate collections directly
viewModel.Workspaces.Add(newWorkspace); // Use service instead

// BAD - Don't call database directly
var projects = await databaseService.GetProjectsAsync(); // Use ViewModel
```

**How to test in isolation**:
```csharp
// Mock dependencies
var mockDbService = new Mock<IDatabaseIntegrationService>();
var mockLogger = new Mock<ILogger<SnitcherMainViewModel>>();
var viewModel = new SnitcherMainViewModel(mockDbService.Object, mockLogger.Object);

// Test commands
await viewModel.CreateWorkspaceCommand.ExecuteAsync();
mockDbService.Verify(x => x.CreateWorkspaceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
```

**How to mock or replace**:
- Mock IDatabaseIntegrationService for data operations
- Mock ILogger for testing logging behavior
- Create test ViewModels with mock services

## Extension & Modification Guide

**How to add new feature**:
1. Add new observable property if needed
2. Create new RelayCommand method
3. Implement logic using database service
4. Update UI to bind new property/command
5. Add error handling and status messages

**Where NOT to add logic**:
- Don't add business validation rules
- Don't implement data access logic
- Don't add UI-specific code

**Safe extension points**:
- New command methods following existing pattern
- New computed properties for UI state
- Additional status message handling
- Enhanced error reporting

**Common mistakes**:
- Forgetting to make command methods async
- Not updating loading states properly
- Direct collection manipulation instead of using service
- Missing error handling in new commands

**Refactoring warnings**:
- Large method complexity in command handlers
- Repeated error handling patterns
- Complex state management logic
- Potential memory leaks with event subscriptions

## Failure Modes & Debugging

**Common runtime errors**:
- NullReferenceException when database service unavailable
- InvalidOperationException when commands executed with invalid state
- TaskCanceledException when async operations timeout

**Null/reference risks**:
- SelectedWorkspace can be null - always check before use
- Database service may throw exceptions - wrap in try-catch
- Dialog windows may return null - validate results

**Performance risks**:
- Large workspace collections can slow UI loading
- Frequent refresh operations can cause excessive database calls
- Memory leaks if event handlers not properly removed

**Logging points**:
- All database operations logged with success/failure
- Command execution errors logged with full details
- Initialization progress logged for debugging

**How to debug step-by-step**:
1. Set breakpoint in constructor for initialization issues
2. Debug command methods for interaction problems
3. Check database service calls for data issues
4. Monitor property changes for UI binding problems
5. Use StatusMessage to track operation flow

## Cross-References

**Related classes**:
- `DatabaseIntegrationService` - Data operations
- `Workspace` - Workspace model
- `Project` - Project model
- `CreateWorkspaceDialog` - Workspace creation UI
- `SnitcherMainWindow` - Main window view

**Upstream callers**:
- `App.axaml.cs` (creates ViewModel)
- UI View (binds to properties/commands)

**Downstream dependencies**:
- `IDatabaseIntegrationService` (data operations)
- Dialog classes (user interactions)
- Model classes (data structures)

**Documents to read before/after**:
- Before: `App.axaml.cs` (DI setup)
- After: `DatabaseIntegrationService.cs` (data layer)
- After: `Workspace.cs` (data model)

## Knowledge Transfer Notes

**Reusable concepts**:
- MVVM pattern with CommunityToolkit.Mvvm
- Async command handling pattern
- Observable collection management
- Status message and loading state pattern
- Error handling in ViewModels

**Project-specific elements**:
- Snitcher workspace/project domain model
- Database integration service interface
- Dialog-based create/edit operations
- Search functionality across multiple entity types

**How to recreate pattern elsewhere**:
1. Create ViewModel inheriting from ObservableObject
2. Use [ObservableProperty] for data binding
3. Use [RelayCommand] for user interactions
4. Inject dependencies via constructor
5. Implement async operations with proper error handling
6. Provide loading states and user feedback

**Key insights**:
- Keep ViewModels focused on UI coordination, not business logic
- Always handle async operations with try-catch-finally
- Use loading states to provide user feedback during operations
- Implement proper null checking for optional selections
- Log both success and failure cases for debugging
