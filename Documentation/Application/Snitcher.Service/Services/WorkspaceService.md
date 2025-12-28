# WorkspaceService

## Overview

WorkspaceService is the primary business logic component for managing workspace entities in the Snitcher application. It encapsulates all workspace-related operations including creation, retrieval, updating, deletion, search, and default workspace management. This service acts as the orchestrator between the UI layer and the repository layer, enforcing business rules and ensuring data integrity.

**Why it exists**: To provide a centralized, testable, and maintainable location for all workspace business logic, separating concerns between data access and business operations while enforcing validation and business rules.

**Problem it solves**: Without this service, business logic would be scattered across UI components or directly in repositories, making the code difficult to test, maintain, and extend. It provides a clear contract for workspace operations.

**What would break if removed**: All workspace management functionality would fail. UI components couldn't create, update, or delete workspaces. Business rules would be bypassed, and the application would lose its workspace organization capabilities.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0, Microsoft.Extensions.Logging

**Libraries**: None (pure business logic)

**UI frameworks**: N/A (service layer)

**Persistence / communication technologies**: Repository pattern abstraction, Entity Framework Core (implicit)

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, dependency injection container, logging infrastructure

**Version hints**: Uses modern C# features, async/await patterns, dependency injection

## Architectural Role

**Layer**: Application Layer (Service)

**Responsibility boundaries**:
- MUST implement workspace business logic and rules
- MUST validate inputs and enforce business constraints
- MUST coordinate repository operations
- MUST handle error scenarios appropriately
- MUST NOT access the database directly
- MUST NOT contain UI-specific logic

**What it MUST do**:
- Create, read, update, delete workspaces
- Enforce business rules (unique names, default workspace protection)
- Search and filter workspaces
- Manage default workspace designation
- Handle transaction coordination
- Provide comprehensive error handling and logging

**What it MUST NOT do**:
- Access Entity Framework DbContext directly
- Implement UI-specific validation or formatting
- Handle file system operations beyond validation
- Contain presentation logic

**Dependencies (incoming)**: UI layer, Application layer, other services

**Dependencies (outgoing)**: IWorkspaceRepository, IUnitOfWork, ILogger

## Execution Flow

**Where execution starts**: WorkspaceService methods are called by UI ViewModels or other service components when workspace operations are needed.

**How control reaches this component**:
1. User performs workspace action in UI
2. ViewModel calls WorkspaceService method
3. Service validates input and enforces business rules
4. Service calls repository methods
5. Repository performs data access operations
6. Results flow back through service to UI

**Call sequence (step-by-step)**:
1. UI layer calls service method (e.g., CreateWorkspaceAsync)
2. Service validates input parameters
3. Service checks business rules (e.g., name uniqueness)
4. Service creates workspace entity
5. Service calls repository to persist entity
6. Service coordinates unit of work if needed
7. Result returned to caller with appropriate error handling

**Synchronous vs asynchronous behavior**: All operations are asynchronous by design

**Threading / dispatcher / event loop notes**: Thread-safe through dependency injection and async operations

**Lifecycle**: Scoped service lifetime - new instance per request/scope

## Public API / Surface Area

**Constructors**:
- `WorkspaceService(IWorkspaceRepository workspaceRepository, IUnitOfWork unitOfWork, ILogger<WorkspaceService> logger)`: Dependency injection constructor

**Workspace Management**:
- `Task<Workspace> CreateWorkspaceAsync(string name, string description = "", string path = "", CancellationToken cancellationToken = default)`: Create new workspace
- `Task<IEnumerable<Workspace>> GetAllWorkspacesAsync(CancellationToken cancellationToken = default)`: Retrieve all workspaces
- `Task<Workspace?> GetWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default)`: Get workspace by ID
- `Task<Workspace?> GetWorkspaceByNameAsync(string name, CancellationToken cancellationToken = default)`: Get workspace by name
- `Task<Workspace> UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)`: Update existing workspace
- `Task<bool> DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken = default)`: Delete workspace

**Default Workspace Management**:
- `Task<Workspace?> GetDefaultWorkspaceAsync(CancellationToken cancellationToken = default)`: Get default workspace
- `Task<bool> SetDefaultWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)`: Set workspace as default

**Search Operations**:
- `Task<IEnumerable<Workspace>> SearchWorkspacesAsync(string searchTerm, CancellationToken cancellationToken = default)`: Search workspaces

**Expected input/output**:
- Input: Workspace data, search terms, IDs
- Output: Workspace entities, success indicators, error exceptions

**Side effects**:
- Modifies database state through repository
- Updates default workspace designation
- Logs operations and errors

**Error behavior**: Throws specific exceptions for business rule violations, logs errors, provides meaningful error messages

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Constructor and dependencies (lines 23-31)**:
```csharp
public WorkspaceService(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ILogger<WorkspaceService> logger)
{
    _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```
- Dependency injection with null validation
- Ensures all required dependencies are available
- Throws ArgumentNullException for missing dependencies

**CreateWorkspaceAsync method (lines 41-79)**:
```csharp
public async Task<Workspace> CreateWorkspaceAsync(string name, string description = "", string path = "", CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Creating workspace {Name}", name);

    try
    {
        // Check if workspace with same name already exists
        var existingWorkspace = await _workspaceRepository.GetByNameAsync(name, cancellationToken);
        if (existingWorkspace != null)
        {
            throw new InvalidOperationException($"A workspace with name '{name}' already exists.");
        }

        // Use default path if not provided
        if (string.IsNullOrWhiteSpace(path))
        {
            path = GetDefaultWorkspacePath(name);
        }

        var workspace = new Workspace
        {
            Name = name,
            Description = description,
            Path = path,
            IsDefault = false // Don't make new workspaces default by default
        };

        var createdWorkspace = await _workspaceRepository.AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created workspace {Name} with ID {Id}", createdWorkspace.Name, createdWorkspace.Id);
        return createdWorkspace;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create workspace {Name}", name);
        throw;
    }
}
```
- Validates workspace name uniqueness
- Generates default path if not provided
- Creates workspace entity with proper defaults
- Persists through repository with transaction coordination
- Comprehensive logging and error handling

**DeleteWorkspaceAsync method (lines 175-205)**:
```csharp
public async Task<bool> DeleteWorkspaceAsync(Guid id, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Deleting workspace with ID {Id}", id);

    try
    {
        var workspace = await _workspaceRepository.GetByIdAsync(id, cancellationToken);
        if (workspace == null)
        {
            _logger.LogWarning("Workspace with ID {Id} not found", id);
            return false;
        }

        // Don't allow deletion of default workspace
        if (workspace.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete the default workspace.");
        }

        await _workspaceRepository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted workspace {Name}", workspace.Name);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete workspace with ID {Id}", id);
        throw;
    }
}
```
- Validates workspace existence before deletion
- Enforces business rule protecting default workspace
- Performs soft delete through repository
- Proper transaction coordination

**SearchWorkspacesAsync method (lines 266-285)**:
```csharp
public async Task<IEnumerable<Workspace>> SearchWorkspacesAsync(string searchTerm, CancellationToken cancellationToken = default)
{
    _logger.LogDebug("Searching workspaces with term {SearchTerm}", searchTerm);

    try
    {
        var workspaces = await _workspaceRepository.GetAllAsync(cancellationToken);
        var filteredWorkspaces = workspaces
            .Where(w => w.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       (w.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        return filteredWorkspaces;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to search workspaces with term {SearchTerm}", searchTerm);
        throw;
    }
}
```
- Retrieves all workspaces from repository
- Performs client-side filtering (could be optimized with database search)
- Case-insensitive search across name and description
- Handles null description safely

**GetDefaultWorkspacePath method (lines 292-297)**:
```csharp
private static string GetDefaultWorkspacePath(string name)
{
    var sanitized = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Snitcher", "Workspaces", sanitized);
}
```
- Sanitizes workspace name for valid file system path
- Constructs path in user profile directory
- Handles invalid file name characters

**Algorithms used**:
- String sanitization for file paths
- Case-insensitive string comparison
- Null-safe string operations
- Exception handling and logging patterns

**Conditional logic explanation**:
- Name uniqueness validation prevents duplicates
- Default workspace protection prevents accidental deletion
- Path generation with fallback ensures valid paths
- Null coalescing for safe description handling

**State transitions**:
- Created → Validated → Persisted → Logged
- Existing → Validated → Updated → Persisted
- Existing → Validated → Deleted → Logged
- Any state → Searched → Filtered → Returned

**Important invariants**:
- Workspace names must be unique
- Default workspace cannot be deleted
- All operations are logged for audit trail
- Transactions ensure data consistency

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Service Layer Pattern**: Encapsulates business logic
- **Repository Pattern**: Accesses data through repository abstraction
- **Unit of Work Pattern**: Coordinates transactions
- **Dependency Injection Pattern**: Manages dependencies

**Architectural patterns**:
- **Clean Architecture**: Service layer depends on abstractions
- **Domain-Driven Design**: Encapsulates domain knowledge
- **SOLID Principles**: Single responsibility, dependency inversion

**Why these patterns were chosen (inferred)**:
- Service layer separates business logic from infrastructure
- Repository pattern enables testability and flexibility
- Unit of Work ensures transactional consistency
- Dependency injection enables loose coupling

**Trade-offs**:
- Service layer overhead vs direct repository access: More layers but better separation
- Client-side search vs database search: Simpler but less efficient for large datasets
- Exception handling vs result objects: Clearer error flow but more try/catch blocks

**Anti-patterns avoided or possibly introduced**:
- Avoided: Anemic service (no business logic)
- Avoided: God service (focused on workspace concerns)
- Possible risk: Service doing too much validation

## Binding / Wiring / Configuration

**Dependency injection**: Registered as scoped service in ServiceCollectionExtensions

**Data binding (if UI)**: N/A - service layer

**Configuration sources**: Dependency injection container

**Runtime wiring**: Microsoft.Extensions.DependencyInjection

**Registration points**: SnitcherConfiguration.ConfigureSnitcher() method

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Create workspace
var workspace = await workspaceService.CreateWorkspaceAsync("MyProject", "My project workspace");
```

**Realistic example**:
```csharp
public class WorkspaceViewModel
{
    private readonly WorkspaceService _workspaceService;
    
    public async Task CreateWorkspaceCommand()
    {
        try
        {
            var workspace = await _workspaceService.CreateWorkspaceAsync(
                Name, 
                Description, 
                Path);
            
            // Update UI with new workspace
            Workspaces.Add(workspace);
            StatusMessage = "Workspace created successfully";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message; // "A workspace with name 'X' already exists"
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating workspace: {ex.Message}";
        }
    }
    
    public async Task DeleteWorkspaceCommand(Workspace workspace)
    {
        try
        {
            var success = await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
            if (success)
            {
                Workspaces.Remove(workspace);
                StatusMessage = "Workspace deleted successfully";
            }
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message; // "Cannot delete the default workspace"
        }
    }
}
```

**Transaction example**:
```csharp
public async Task CreateWorkspaceWithProjectAsync()
{
    // Service handles transactions internally
    var workspace = await workspaceService.CreateWorkspaceAsync("New Workspace");
    
    // Project service would handle its own transactions
    await projectService.CreateProjectAsync(workspace.Id, "New Project");
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Bypassing business rules
var workspace = new Workspace { Name = "Duplicate" };
await repository.AddAsync(workspace); // Doesn't check for duplicates

// WRONG: Not handling business exceptions
try 
{
    await workspaceService.CreateWorkspaceAsync("Test");
}
catch // Catches all exceptions including business rule violations
{
    // Should handle specific exceptions differently
}

// WRONG: Assuming synchronous operations
var workspace = workspaceService.CreateWorkspaceAsync("Test").Result; // Deadlock risk

// WRONG: Modifying entity directly
var workspace = await workspaceService.GetWorkspaceByIdAsync(id);
workspace.IsDefault = true; // Should use SetDefaultWorkspaceAsync
```

**How to test this in isolation**:
```csharp
[Test]
public async Task CreateWorkspace_ShouldEnforceUniqueNames()
{
    // Arrange
    var mockRepo = new Mock<IWorkspaceRepository>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var mockLogger = new Mock<ILogger<WorkspaceService>>();
    
    var existingWorkspace = new Workspace { Name = "Test" };
    mockRepo.Setup(r => r.GetByNameAsync("Test", It.IsAny<CancellationToken>()))
        .ReturnsAsync(existingWorkspace);
    
    var service = new WorkspaceService(mockRepo.Object, mockUnitOfWork.Object, mockLogger.Object);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.CreateWorkspaceAsync("Test"));
    
    Assert.That(ex.Message, Does.Contain("already exists"));
}

[Test]
public async Task DeleteWorkspace_ShouldProtectDefault()
{
    // Arrange
    var mockRepo = new Mock<IWorkspaceRepository>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var mockLogger = new Mock<ILogger<WorkspaceService>>();
    
    var defaultWorkspace = new Workspace { Id = Guid.NewGuid(), IsDefault = true };
    mockRepo.Setup(r => r.GetByIdAsync(defaultWorkspace.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(defaultWorkspace);
    
    var service = new WorkspaceService(mockRepo.Object, mockUnitOfWork.Object, mockLogger.Object);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.DeleteWorkspaceAsync(defaultWorkspace.Id));
    
    Assert.That(ex.Message, Does.Contain("Cannot delete the default workspace"));
}
```

**How to mock or replace it**:
```csharp
// Using mock framework
var mockWorkspaceService = new Mock<IWorkspaceService>();
mockWorkspaceService.Setup(s => s.CreateWorkspaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(new Workspace { Name = "Mocked" });

// Manual mock for testing
public class TestWorkspaceService : IWorkspaceService
{
    private readonly List<Workspace> _workspaces = new();
    
    public async Task<Workspace> CreateWorkspaceAsync(string name, string description = "", string path = "")
    {
        if (_workspaces.Any(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Workspace already exists");
            
        var workspace = new Workspace { Name = name, Description = description, Path = path };
        _workspaces.Add(workspace);
        return workspace;
    }
    
    // ... implement other methods for testing
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new method to IWorkspaceService interface
2. Implement method in WorkspaceService with business logic
3. Add appropriate validation and error handling
4. Include logging for the new operation
5. Write unit tests for the new functionality

**Where NOT to add logic**:
- Don't add database access code (use repository)
- Don't add UI-specific validation or formatting
- Don't add file system operations beyond path validation
- Don't add presentation layer concerns

**Safe extension points**:
- New business methods for workspace operations
- Enhanced validation logic
- Additional search capabilities
- Workspace export/import functionality
- Workspace analytics and reporting

**Common mistakes**:
- Adding too many responsibilities (violates SRP)
- Bypassing repository pattern
- Not handling business rule violations properly
- Forgetting to log operations
- Making synchronous calls to async methods

**Refactoring warnings**:
- Changing method signatures breaks calling code
- Modifying business rules affects data validation
- Adding new dependencies affects DI configuration
- Changing exception types affects error handling

## Failure Modes & Debugging

**Common runtime errors**:
- **InvalidOperationException**: Business rule violations (duplicate names, deleting default)
- **ArgumentNullException**: Missing required parameters
- **RepositoryException**: Data access failures from repository layer
- **OperationCanceledException**: Operation cancelled via cancellation token

**Null/reference risks**:
- Repository dependencies validated in constructor
- Workspace entities validated before operations
- Logger dependency validated in constructor
- Search terms handled safely for null/empty

**Performance risks**:
- SearchWorkspacesAsync loads all workspaces (inefficient for large datasets)
- String operations in search could be optimized
- Logging overhead in high-frequency operations

**Logging points**:
- All major operations logged at Information level
- Errors logged at Error level with full exception details
- Debug operations logged at Debug level
- Performance metrics could be added

**How to debug step-by-step**:
1. Enable debug logging to see all operations
2. Set breakpoints in service methods to trace execution
3. Monitor repository calls and responses
4. Check business rule validation logic
5. Verify transaction coordination

## Cross-References

**Related classes**:
- IWorkspaceService (interface contract)
- IWorkspaceRepository (data access)
- IUnitOfWork (transaction management)
- Workspace (domain entity)

**Upstream callers**:
- UI ViewModels (presentation layer)
- Application services (orchestration)
- Other services (cross-service operations)

**Downstream dependencies**:
- Repository implementations (data persistence)
- Database through Entity Framework
- Logging infrastructure

**Documents that should be read before/after**:
- Read: IWorkspaceService documentation (contract)
- Read: IWorkspaceRepository documentation (data access)
- Read: Workspace documentation (domain entity)
- Read: UnitOfWork documentation (transactions)

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Service layer pattern for business logic
- Repository pattern for data access abstraction
- Unit of Work pattern for transaction management
- Comprehensive logging and error handling
- Business rule enforcement patterns

**What is project-specific**:
- Specific workspace business rules
- Default workspace management logic
- Path generation and sanitization
- Particular validation rules

**How to recreate this pattern from scratch elsewhere**:
1. Define service interface with business methods
2. Implement service class with dependency injection
3. Add comprehensive validation and business rules
4. Coordinate repository operations through Unit of Work
5. Include logging and error handling
6. Follow async/await patterns throughout
7. Write unit tests for all business logic

**Key insights for implementation**:
- Always validate inputs and enforce business rules
- Use dependency injection for testability
- Log all significant operations for debugging
- Handle business rule violations with specific exceptions
- Keep services focused on specific business domains
- Use async operations for I/O bound work
