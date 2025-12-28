# Workspace

## Overview

The Workspace class represents the highest-level organizational container in the Snitcher application, serving as a logical grouping mechanism for related projects. It provides a way to organize multiple projects under a single umbrella, enabling users to manage large-scale code analysis operations across multiple codebases. Workspaces act as the root of the organizational hierarchy, containing projects and their respective namespaces.

This entity exists to provide a structured approach to managing multiple related projects within the application. It enables users to group projects by client, team, technology stack, or any other organizational criteria. The Workspace entity maintains the relationship between the application's logical organization and the physical grouping of related codebases, facilitating bulk operations, reporting, and management across project collections.

If the Workspace class were removed, the system would lose:
- The ability to organize related projects into logical groups
- Hierarchical organization structure (Workspace → Projects → Namespaces)
- Bulk operations across multiple projects
- Workspace-level reporting and analytics
- Organizational context for project management
- Scalable project organization for large codebases

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- .NET 8.0 Base Class Library
- Entity Framework Core (implicit through navigation properties)

**Libraries:**
- System namespace for core functionality
- Snitcher.Core.Interfaces for domain contracts
- System.Collections.Generic for navigation properties

**UI Frameworks:**
- N/A (Domain layer component)

**Persistence/Communication Technologies:**
- Entity Framework Core for relational mapping
- SQLite provider for local storage
- Navigation properties for relationship mapping

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- NuGet package management

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires file system access for path validation
- UTC DateTime handling for timestamp consistency

**Version Hints:**
- Uses modern C# features (nullable reference types, expression-bodied members)
- Compatible with .NET 8.0 and later versions
- Follows Entity Framework Core conventions

## Architectural Role

**Layer:** Domain Layer (Clean Architecture - Core)

**Responsibility Boundaries:**
- MUST provide workspace identity and basic information
- MUST maintain file system path references for workspace root
- MUST organize projects in hierarchical structure
- MUST support default workspace identification
- MUST NOT contain file system operations (delegated to services)
- MUST NOT perform project-level operations (delegated to project services)

**What it MUST do:**
- Store workspace metadata (name, description, version)
- Maintain file system path references
- Track project collections through navigation properties
- Identify default workspace for application
- Provide basic workspace integrity validation

**What it MUST NOT do:**
- Access the file system directly
- Perform project analysis or management operations
- Validate path existence or permissions
- Handle external service communications
- Implement business logic beyond basic validation

**Dependencies (Incoming):**
- ProjectEntity entities (child relationship)
- WorkspaceService for business operations
- WorkspaceRepository for persistence operations
- Application layer for workspace management

**Dependencies (Outgoing):**
- BaseEntity for infrastructure functionality
- IAuditableEntity for audit trail support
- ISoftDeletable for soft delete functionality
- ProjectEntity for navigation properties

## Execution Flow

**Where execution starts:**
- Workspace creation through service layer
- Workspace retrieval from repository layer
- Direct property access for validation and updates

**How control reaches this component:**
1. User creates new workspace through UI → Service layer → Workspace constructor
2. Application initialization creates default workspace → Service layer calls repository
3. Workspace validation → Service layer calls IsValid()
4. Project organization operations → Navigation property access

**Call Sequence (Workspace Creation):**
1. UI layer initiates workspace creation
2. WorkspaceService validates input and calls repository
3. Repository creates new Workspace instance
4. BaseEntity constructor initializes audit fields
5. Workspace-specific properties set from input data
6. Entity saved to database through repository

**Call Sequence (Default Workspace Operations):**
1. Application startup checks for default workspace
2. WorkspaceService retrieves or creates default workspace
3. IsDefault property used to identify primary workspace
4. Projects automatically associated with default workspace

**Synchronous vs Asynchronous Behavior:**
- Entity operations are synchronous
- Persistence operations through repository are asynchronous
- No I/O operations within entity itself

**Threading/Dispatcher Notes:**
- Entity instances are not thread-safe for concurrent modifications
- Read operations can be performed concurrently
- Default workspace operations should be synchronized

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Instantiated through service layer with initial properties
2. **Usage:** Modified through business operations, projects added/removed
3. **Default Management:** May serve as application default workspace
4. **Soft Delete:** Optionally marked as deleted but retained for history
5. **Physical Deletion:** Eventually removed by repository layer if needed

## Public API / Surface Area

**Constructors:**
- Implicit parameterless constructor (inherited from BaseEntity)

**Public Methods:**
- `bool IsValid()`: Validates that required fields (Name, Path) are not empty
- `void MarkAsDeleted()`: Inherited - marks workspace as soft-deleted
- `void Restore()`: Inherited - restores a soft-deleted workspace
- `void UpdateTimestamp()`: Inherited - updates audit timestamp
- `bool Equals(object? obj)`: Inherited - identity-based equality
- `int GetHashCode()`: Inherited - hash code based on Id
- `string ToString()`: Inherited - string representation

**Public Properties:**
- `string Name`: Workspace name (must be unique within application scope)
- `string? Description`: Optional workspace description
- `string Path`: File system path to workspace root directory
- `string? Version`: Optional workspace version string
- `bool IsDefault`: Indicates if this is the application's default workspace
- `ICollection<ProjectEntity> Projects`: Navigation property for child projects
- `Guid Id`: Inherited - unique identifier
- `DateTime CreatedAt`: Inherited - creation timestamp
- `DateTime UpdatedAt`: Inherited - last modification timestamp
- `bool IsDeleted`: Inherited - soft delete flag

**Expected Input/Output:**
- **Input:** Workspace metadata through service layer
- **Output:** Validated workspace entity with proper relationships

**Side Effects:**
- Changing properties affects entity state for persistence
- Navigation properties enable cascade operations
- IsDefault flag affects application behavior

**Error Behavior:**
- IsValid() returns false for invalid state but doesn't throw exceptions
- No validation exceptions thrown at entity level (handled by service layer)
- Navigation properties may throw null reference exceptions if not properly loaded

## Internal Logic Breakdown

**Validation Logic (Lines 49-53):**
```csharp
public bool IsValid()
{
    return !string.IsNullOrWhiteSpace(Name) && 
           !string.IsNullOrWhiteSpace(Path);
}
```
- Ensures required fields are present and not just whitespace
- Does not validate path existence (delegated to service layer)
- Provides basic integrity checking before persistence

**Default Workspace Logic:**
```csharp
public bool IsDefault { get; set; } = false;
```
- Defaults to false for most workspaces
- Application ensures exactly one workspace has IsDefault = true
- Used by service layer for workspace resolution

**Property Initialization:**
```csharp
public string Name { get; set; } = string.Empty;
public string Path { get; set; } = string.Empty;
```
- Properties initialized to safe default values
- Prevents null reference exceptions in validation logic
- Follows C# best practices for string properties

**Navigation Property Setup:**
```csharp
public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
```
- Virtual property enables Entity Framework Core lazy loading
- Initialized to empty collection to prevent null references
- Supports one-to-many relationship with projects

**Important Invariants:**
- Name and Path must not be null or empty for valid workspaces
- Exactly one workspace should have IsDefault = true
- Navigation property collection is never null (initialized to empty list)
- All timestamp updates use UTC for consistency

## Patterns & Principles Used

**Design Patterns:**
- **Active Record Pattern:** Entity contains both data and behavior
- **Aggregate Root Pattern:** Workspace serves as root for project hierarchy
- **Factory Pattern (Implicit):** Created through service layer factories

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** Workspace as a domain aggregate root
- **Clean Architecture:** Infrastructure-free domain entity
- **Repository Pattern Support:** Designed to work with repository abstraction

**Why These Patterns Were Chosen:**
- **Active Record:** Reduces anemic domain model, keeps related behavior close
- **Aggregate Root:** Ensures project consistency through workspace boundary
- **Clean Architecture:** Maintains testability and separation of concerns

**Trade-offs:**
- **Pros:** Clear ownership hierarchy, contained business logic, testable
- **Cons:** Potential for large aggregate over time, coupling to BaseEntity
- **Decision:** Benefits of clear organization outweigh complexity costs

**Anti-patterns Avoided:**
- **Anemic Domain Model:** Entity contains validation and update behavior
- **God Object:** Focused responsibility on workspace management only
- **Primitive Obsession:** Uses proper types and relationships

## Binding / Wiring / Configuration

**Dependency Injection:**
- Not directly registered in DI container
- Created and managed through repository pattern
- No constructor injection (infrastructure-free design)

**Data Binding:**
- Properties configured for Entity Framework Core mapping
- Navigation properties support lazy and eager loading
- Foreign key relationships automatically mapped

**Configuration Sources:**
- EF Core Fluent API in SnitcherDbContext
- Convention-based configuration for common properties
- Custom configurations for business-specific constraints

**Runtime Wiring:**
- Discovered through reflection by EF Core
- Automatically mapped to database schema
- Cascade behaviors configured for project relationships

**Registration Points:**
- Indirect registration through repository pattern
- Model builder configuration in SnitcherDbContext
- Service layer registration for business operations

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Create workspace through service layer
var workspace = new Workspace 
{ 
    Name = "MyWorkspace", 
    Path = @"C:\Workspaces\MyWorkspace",
    IsDefault = true
};

// Validate workspace
if (workspace.IsValid())
{
    // Workspace is ready for persistence
    Console.WriteLine($"Workspace {workspace.Name} is valid");
}
```

**Realistic Example:**
```csharp
public class WorkspaceManagementService
{
    public async Task<Workspace> CreateWorkspaceAsync(CreateWorkspaceDto dto)
    {
        var workspace = new Workspace
        {
            Name = dto.Name,
            Description = dto.Description,
            Path = dto.Path,
            Version = dto.Version,
            IsDefault = dto.IsDefault
        };
        
        // Additional validation in service layer
        if (!await ValidateWorkspacePathAsync(workspace.Path))
        {
            throw new ArgumentException("Invalid workspace path");
        }
        
        // Ensure only one default workspace
        if (workspace.IsDefault)
        {
            await ClearDefaultWorkspaceAsync();
        }
        
        return await _workspaceRepository.CreateAsync(workspace);
    }
    
    public async Task<IEnumerable<ProjectEntity>> GetWorkspaceProjectsAsync(Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        return workspace.Projects;
    }
    
    public async Task<Workspace> GetDefaultWorkspaceAsync()
    {
        return await _workspaceRepository.GetDefaultAsync();
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Assuming entity validates path existence
var workspace = new Workspace { Name = "Test", Path = "C:\\NonExistent" };
if (workspace.IsValid()) // Returns true - doesn't check file system!
{
    // Path might not exist - validation is only basic
}

// WRONG: Multiple default workspaces
var workspace1 = new Workspace { Name = "WS1", IsDefault = true };
var workspace2 = new Workspace { Name = "WS2", IsDefault = true };
// Application should prevent this state

// WRONG: Direct file system access in entity
public class BadWorkspace : BaseEntity
{
    public string Path { get; set; }
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Path) && 
               Directory.Exists(Path); // WRONG - Entity shouldn't access file system!
    }
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void Workspace_ShouldValidateRequiredFields()
{
    // Arrange
    var workspace = new Workspace();
    
    // Act & Assert
    Assert.That(workspace.IsValid(), Is.False);
    
    workspace.Name = "Test";
    Assert.That(workspace.IsValid(), Is.False);
    
    workspace.Path = @"C:\Test";
    Assert.That(workspace.IsValid(), Is.True);
}

[Test]
public void Workspace_ShouldManageProjects()
{
    // Arrange
    var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    var project1 = new ProjectEntity { Name = "Project1", WorkspaceId = workspace.Id };
    var project2 = new ProjectEntity { Name = "Project2", WorkspaceId = workspace.Id };
    
    // Act
    workspace.Projects.Add(project1);
    workspace.Projects.Add(project2);
    
    // Assert
    Assert.That(workspace.Projects.Count, Is.EqualTo(2));
}

[Test]
public void DefaultWorkspace_ShouldBeIdentifiable()
{
    // Arrange
    var defaultWorkspace = new Workspace { Name = "Default", IsDefault = true };
    var regularWorkspace = new Workspace { Name = "Regular", IsDefault = false };
    
    // Act & Assert
    Assert.That(defaultWorkspace.IsDefault, Is.True);
    Assert.That(regularWorkspace.IsDefault, Is.False);
}
```

**How to Mock or Replace:**
```csharp
// For testing services, create test doubles
public class MockWorkspace : Workspace
{
    public MockWorkspace()
    {
        Id = Guid.NewGuid(); // Predictable ID for testing
    }
    
    public override void UpdateTimestamp()
    {
        // Custom timestamp logic for testing
        base.UpdateTimestamp();
    }
}

// Mock repository for testing
public class MockWorkspaceRepository : IWorkspaceRepository
{
    private readonly List<Workspace> _workspaces = new();
    
    public async Task<Workspace> CreateAsync(Workspace workspace)
    {
        workspace.Id = Guid.NewGuid();
        _workspaces.Add(workspace);
        return workspace;
    }
    
    public async Task<Workspace> GetDefaultAsync()
    {
        return _workspaces.FirstOrDefault(w => w.IsDefault);
    }
    
    // ... other methods
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **New Properties:** Add properties with appropriate validation in IsValid()
2. **Workspace Metadata:** Add additional workspace-level information
3. **Enhanced Validation:** Extend IsValid() method with business rules
4. **Workspace Configuration:** Add settings and preferences for workspace

**Where NOT to Add Logic:**
- **File System Operations:** Belong in service layer (path validation, file access)
- **Project Management:** Belong in dedicated project services
- **External Service Calls:** Violates clean architecture principles
- **Complex Business Rules:** Belong in service layer or domain services

**Safe Extension Points:**
- IsValid() method for additional validation
- New properties for workspace metadata
- Navigation properties for additional relationships
- Workspace configuration properties

**Common Mistakes:**
1. **Adding File System Access:** Entities should remain infrastructure-independent
2. **Complex Validation Logic:** Keep validation simple, move complex rules to services
3. **Direct Database Operations:** Use repository pattern instead
4. **Business Logic in Setters:** Use explicit methods for business operations

**Refactoring Warnings:**
- Changing property types requires database migration
- Removing navigation properties breaks existing queries
- Modifying validation logic affects service layer behavior
- Adding new relationships may require cascade configuration

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Invalid Workspace Paths:** Path exists but is inaccessible (permissions issues)
- **Multiple Default Workspaces:** Application logic violation causing conflicts
- **Circular Project References:** Invalid project relationships within workspace

**Null/Reference Risks:**
- **Description Property:** Nullable by design, handle null values
- **Version Property:** Nullable by design, handle null values
- **Navigation Properties:** May be null if not loaded from repository

**Performance Risks:**
- **Large Project Collections:** Navigation property may load many child entities
- **Deep Project Hierarchies:** Recursive operations may be expensive
- **Frequent Default Workspace Queries:** May cause database performance issues

**Logging Points:**
- Workspace creation and modification
- Default workspace changes
- Validation failures
- Project collection operations

**How to Debug Step-by-Step:**
1. **Workspace Creation:** Set breakpoint in service layer workspace creation
2. **Validation Issues:** Step through IsValid() method with test data
3. **Default Workspace Logic:** Monitor default workspace assignment and conflicts
4. **Navigation Problems:** Check entity state and loading strategies

**Common Debugging Scenarios:**
```csharp
// Debug workspace validation
var workspace = new Workspace { Name = "", Path = "" };
Debug.WriteLine($"Workspace valid: {workspace.IsValid()}"); // Should be false

// Debug default workspace logic
var workspace = new Workspace { Name = "Test", IsDefault = true };
Debug.WriteLine($"Is default workspace: {workspace.IsDefault}");

// Debug project relationships
Debug.WriteLine($"Project count: {workspace.Projects.Count}");
foreach (var project in workspace.Projects)
{
    Debug.WriteLine($"Project: {project.Name}, Workspace: {project.WorkspaceId}");
}
```

## Cross-References

**Related Classes:**
- `BaseEntity`: Base class providing infrastructure functionality
- `ProjectEntity`: Child entities organized within workspaces
- `Project`: Alternative project entity used in different contexts
- `ProjectNamespace`: Grandchild entities through project relationship

**Upstream Callers:**
- `Snitcher.Service.Services.WorkspaceService`: Primary business logic consumer
- `Snitcher.Repository.Repositories.WorkspaceRepository`: Persistence operations
- Application initialization for default workspace management
- UI layer for workspace management operations

**Downstream Dependencies:**
- `ProjectEntity` entities through navigation properties
- Validation logic in service layer
- Repository layer for persistence operations

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md`, `ProjectEntity.md`
- **After:** `WorkspaceService.md`, `WorkspaceRepository.md`
- **Related:** `SnitcherDbContext.md` (for persistence configuration)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Aggregate Root Pattern:** Managing hierarchical entity relationships
- **Workspace Organization Model:** Structuring project grouping mechanisms
- **Default Entity Pattern:** Identifying primary entities in collections
- **Hierarchical Data Management:** Multi-level organization structures
- **Navigation Property Management:** Entity relationship patterns

**What Is Project-Specific:**
- **Workspace-Project Hierarchy:** Specific to this application's organization
- **Default Workspace Concept:** Specific to application's workspace management

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Root Entity:** Inherit from BaseEntity for infrastructure
2. **Add Business Properties:** Name, description, path, version fields
3. **Implement Default Logic:** Add IsDefault property and validation
4. **Configure Child Relationships:** Navigation properties for contained entities
5. **Maintain Separation:** Keep file system operations in service layer
6. **Ensure Testability:** Design for easy unit testing and mocking
7. **Document Organization Rules:** Clearly define workspace purpose and usage

**Key Architectural Insights:**
- **Hierarchical Organization:** Workspaces provide natural grouping mechanism
- **Default Entity Management:** Single default entity simplifies many operations
- **Entity Boundaries:** Keep entities focused on data and basic validation
- **Service Layer Delegation:** External operations belong in services
- **Relationship Management:** Navigation properties simplify hierarchical data

**Implementation Checklist for New Projects:**
- [ ] Define clear entity boundaries and responsibilities
- [ ] Implement basic validation in IsValid() method
- [ ] Add default entity identification logic
- [ ] Configure navigation properties for child relationships
- [ ] Ensure infrastructure independence
- [ ] Add comprehensive unit tests
- [ ] Document validation rules and invariants
- [ ] Plan for database schema evolution
