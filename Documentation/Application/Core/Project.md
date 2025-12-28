# Project

## Overview

The Project class represents a top-level organizational unit within the Snitcher application for code analysis and project management. It serves as a central container for organizing codebases, tracking analysis history, and maintaining project metadata. Each Project corresponds to a physical directory on the file system and acts as the primary entry point for code analysis operations.

This entity exists to provide a structured way to manage multiple codebases within the application. It bridges the gap between the physical file system and the application's analytical capabilities, enabling users to organize, track, and analyze different projects independently. The Project entity maintains the relationship between the application's logical organization and the physical location of source code.

If the Project class were removed, the system would lose:
- The ability to organize and manage multiple codebases
- File system path tracking for analysis operations
- Project-level analysis history and timestamps
- Hierarchical namespace organization through Projects
- The primary organizational unit for the entire application
- Integration points with workspace management

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
- MUST provide project identity and basic information
- MUST maintain file system path references
- MUST track analysis history timestamps
- MUST organize namespace hierarchies
- MUST NOT contain file system operations (delegated to services)
- MUST NOT perform analysis operations (delegated to analyzers)

**What it MUST do:**
- Store project metadata (name, description, version)
- Maintain file system path references
- Track analysis timestamps
- Provide namespace organization
- Validate basic project integrity

**What it MUST NOT do:**
- Access the file system directly
- Perform code analysis operations
- Validate path existence or permissions
- Handle external service communications
- Implement business logic beyond basic validation

**Dependencies (Incoming):**
- ProjectNamespace entities (child relationship)
- ProjectService for business operations
- ProjectRepository for persistence operations
- Workspace management (in some contexts)

**Dependencies (Outgoing):**
- BaseEntity for infrastructure functionality
- IAuditableEntity for audit trail support
- ISoftDeletable for soft delete functionality
- ProjectNamespace for navigation properties

## Execution Flow

**Where execution starts:**
- Project creation through service layer
- Project retrieval from repository layer
- Direct property access for validation and updates

**How control reaches this component:**
1. User creates new project through UI → Service layer → Project constructor
2. Analysis operations update timestamps → Service layer calls UpdateLastAnalyzed()
3. Project validation → Service layer calls IsValid()
4. Namespace hierarchy operations → Navigation property access

**Call Sequence (Project Creation):**
1. UI layer initiates project creation
2. ProjectService validates input and calls repository
3. Repository creates new Project instance
4. BaseEntity constructor initializes audit fields
5. Project-specific properties set from input data
6. Entity saved to database through repository

**Call Sequence (Analysis Update):**
1. Analysis service completes project analysis
2. Service layer retrieves project entity
3. UpdateLastAnalyzed() method called
4. LastAnalyzedAt timestamp updated to current UTC
5. BaseEntity.UpdateTimestamp() called for audit trail
6. Changes persisted through repository

**Synchronous vs Asynchronous Behavior:**
- Entity operations are synchronous
- Persistence operations through repository are asynchronous
- No I/O operations within entity itself

**Threading/Dispatcher Notes:**
- Entity instances are not thread-safe for concurrent modifications
- Read operations can be performed concurrently
- Timestamp updates should be synchronized in multi-threaded scenarios

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Instantiated through service layer with initial properties
2. **Usage:** Modified through business operations, timestamps updated
3. **Analysis:** Analysis timestamps updated through UpdateLastAnalyzed()
4. **Soft Delete:** Optionally marked as deleted but retained for history
5. **Physical Deletion:** Eventually removed by repository layer if needed

## Public API / Surface Area

**Constructors:**
- Implicit parameterless constructor (inherited from BaseEntity)

**Public Methods:**
- `void UpdateLastAnalyzed()`: Updates the LastAnalyzedAt timestamp to current UTC time
- `bool IsValid()`: Validates that required fields (Name, Path) are not empty
- `void MarkAsDeleted()`: Inherited - marks project as soft-deleted
- `void Restore()`: Inherited - restores a soft-deleted project
- `void UpdateTimestamp()`: Inherited - updates audit timestamp
- `bool Equals(object? obj)`: Inherited - identity-based equality
- `int GetHashCode()`: Inherited - hash code based on Id
- `string ToString()`: Inherited - string representation

**Public Properties:**
- `string Name`: Project name (must be unique within application scope)
- `string? Description`: Optional project description
- `string Path`: File system path to project root directory
- `string? Version`: Optional project version string
- `DateTime? LastAnalyzedAt`: Timestamp of last analysis (null if never analyzed)
- `ICollection<ProjectNamespace> Namespaces`: Navigation property for child namespaces
- `Guid Id`: Inherited - unique identifier
- `DateTime CreatedAt`: Inherited - creation timestamp
- `DateTime UpdatedAt`: Inherited - last modification timestamp
- `bool IsDeleted`: Inherited - soft delete flag

**Expected Input/Output:**
- **Input:** Project metadata through service layer
- **Output:** Validated project entity with proper relationships

**Side Effects:**
- UpdateLastAnalyzed() modifies audit timestamps
- Changing properties affects entity state for persistence
- Navigation properties enable cascade operations

**Error Behavior:**
- IsValid() returns false for invalid state but doesn't throw exceptions
- No validation exceptions thrown at entity level (handled by service layer)
- Navigation properties may throw null reference exceptions if not properly loaded

## Internal Logic Breakdown

**Validation Logic (Lines 58-62):**
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

**Analysis Update Logic (Lines 48-52):**
```csharp
public void UpdateLastAnalyzed()
{
    LastAnalyzedAt = DateTime.UtcNow;
    UpdateTimestamp();
}
```
- Updates analysis timestamp to current UTC time
- Calls base class method to maintain audit trail consistency
- Provides centralized timestamp management for analysis operations

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
public virtual ICollection<ProjectNamespace> Namespaces { get; set; } = new List<ProjectNamespace>();
```
- Virtual property enables Entity Framework Core lazy loading
- Initialized to empty collection to prevent null references
- Supports one-to-many relationship with namespaces

**Important Invariants:**
- Name and Path must not be null or empty for valid projects
- LastAnalyzedAt is null until first analysis is performed
- Navigation property collection is never null (initialized to empty list)
- All timestamp updates use UTC for consistency

## Patterns & Principles Used

**Design Patterns:**
- **Active Record Pattern:** Entity contains both data and behavior
- **Aggregate Root Pattern:** Project serves as root for namespace hierarchy
- **Factory Pattern (Implicit):** Created through service layer factories

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** Project as a domain aggregate root
- **Clean Architecture:** Infrastructure-free domain entity
- **Repository Pattern Support:** Designed to work with repository abstraction

**Why These Patterns Were Chosen:**
- **Active Record:** Reduces anemic domain model, keeps related behavior close
- **Aggregate Root:** Ensures namespace consistency through project boundary
- **Clean Architecture:** Maintains testability and separation of concerns

**Trade-offs:**
- **Pros:** Clear ownership hierarchy, contained business logic, testable
- **Cons:** Potential for large aggregate over time, coupling to BaseEntity
- **Decision:** Benefits of clear organization outweigh complexity costs

**Anti-patterns Avoided:**
- **Anemic Domain Model:** Entity contains validation and update behavior
- **God Object:** Focused responsibility on project management only
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
- Cascade behaviors configured for namespace relationships

**Registration Points:**
- Indirect registration through repository pattern
- Model builder configuration in SnitcherDbContext
- Service layer registration for business operations

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Create project through service layer
var project = new Project 
{ 
    Name = "MyProject", 
    Path = @"C:\Projects\MyProject" 
};

// Validate project
if (project.IsValid())
{
    // Project is ready for persistence
    Console.WriteLine($"Project {project.Name} is valid");
}
```

**Realistic Example:**
```csharp
public class ProjectAnalysisService
{
    public async Task AnalyzeProjectAsync(Guid projectId)
    {
        // Retrieve project
        var project = await _projectRepository.GetByIdAsync(projectId);
        
        // Perform analysis (simplified)
        await PerformCodeAnalysisAsync(project.Path);
        
        // Update analysis timestamp
        project.UpdateLastAnalyzed();
        
        // Persist changes
        await _projectRepository.UpdateAsync(project);
    }
    
    public async Task<Project> CreateProjectAsync(CreateProjectDto dto)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            Path = dto.Path,
            Version = dto.Version
        };
        
        // Additional validation in service layer
        if (!await ValidateProjectPathAsync(project.Path))
        {
            throw new ArgumentException("Invalid project path");
        }
        
        return await _projectRepository.CreateAsync(project);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Assuming entity validates path existence
var project = new Project { Name = "Test", Path = "C:\\NonExistent" };
if (project.IsValid()) // Returns true - doesn't check file system!
{
    // Path might not exist - validation is only basic
}

// WRONG: Direct file system access in entity
public class BadProject : BaseEntity
{
    public string Path { get; set; }
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Path) && 
               Directory.Exists(Path); // WRONG - Entity shouldn't access file system!
    }
}

// WRONG: Forgetting to update timestamps
public void UpdateProjectName(Project project, string newName)
{
    project.Name = newName;
    // Missing project.UpdateTimestamp() call!
    // This should be handled by repository or service layer
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void Project_ShouldValidateRequiredFields()
{
    // Arrange
    var project = new Project();
    
    // Act & Assert
    Assert.That(project.IsValid(), Is.False);
    
    project.Name = "Test";
    Assert.That(project.IsValid(), Is.False);
    
    project.Path = @"C:\Test";
    Assert.That(project.IsValid(), Is.True);
}

[Test]
public void Project_ShouldUpdateAnalysisTimestamp()
{
    // Arrange
    var project = new Project { Name = "Test", Path = @"C:\Test" };
    var originalTime = project.UpdatedAt;
    Thread.Sleep(10); // Ensure time difference
    
    // Act
    project.UpdateLastAnalyzed();
    
    // Assert
    Assert.That(project.LastAnalyzedAt, Is.Not.Null);
    Assert.That(project.UpdatedAt, Is.GreaterThan(originalTime));
}

[Test]
public void Project_ShouldManageNamespaces()
{
    // Arrange
    var project = new Project { Name = "Test", Path = @"C:\Test" };
    var namespace1 = new ProjectNamespace { Name = "Core", FullName = "Test.Core", ProjectId = project.Id };
    var namespace2 = new ProjectNamespace { Name = "UI", FullName = "Test.UI", ProjectId = project.Id };
    
    // Act
    project.Namespaces.Add(namespace1);
    project.Namespaces.Add(namespace2);
    
    // Assert
    Assert.That(project.Namespaces.Count, Is.EqualTo(2));
}
```

**How to Mock or Replace:**
```csharp
// For testing services, create test doubles
public class MockProject : Project
{
    public MockProject()
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
public class MockProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = new();
    
    public async Task<Project> CreateAsync(Project project)
    {
        project.Id = Guid.NewGuid();
        _projects.Add(project);
        return project;
    }
    
    // ... other methods
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **New Properties:** Add properties with appropriate validation in IsValid()
2. **Custom Validation:** Extend IsValid() method with business rules
3. **Additional Timestamps:** Add new timestamp fields with update methods
4. **Enhanced Relationships:** Add new navigation properties for related entities

**Where NOT to Add Logic:**
- **File System Operations:** Belong in service layer (path validation, file access)
- **Analysis Logic:** Belong in dedicated analysis services
- **External Service Calls:** Violates clean architecture principles
- **Complex Business Rules:** Belong in service layer or domain services

**Safe Extension Points:**
- IsValid() method for additional validation
- UpdateLastAnalyzed() for custom analysis tracking
- Property setters for validation logic
- New navigation properties for relationships

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
- **Invalid Project Paths:** Path exists but is inaccessible (permissions issues)
- **Circular Namespace References:** Self-referencing namespaces creating infinite loops
- **Navigation Property Null References:** Accessing unloaded navigation properties

**Null/Reference Risks:**
- **Description Property:** Nullable by design, handle null values
- **Version Property:** Nullable by design, handle null values
- **LastAnalyzedAt Property:** Null until first analysis, handle appropriately
- **Navigation Properties:** May be null if not loaded from repository

**Performance Risks:**
- **Large Namespace Collections:** Navigation property may load many child entities
- **Deep Namespace Hierarchies:** Recursive operations may be expensive
- **Frequent Timestamp Updates:** May cause excessive database writes

**Logging Points:**
- Project creation and modification
- Analysis timestamp updates
- Validation failures
- Namespace hierarchy operations

**How to Debug Step-by-Step:**
1. **Project Creation:** Set breakpoint in service layer project creation
2. **Validation Issues:** Step through IsValid() method with test data
3. **Analysis Updates:** Monitor UpdateLastAnalyzed() calls and timestamps
4. **Navigation Problems:** Check entity state and loading strategies

**Common Debugging Scenarios:**
```csharp
// Debug project validation
var project = new Project { Name = "", Path = "" };
Debug.WriteLine($"Project valid: {project.IsValid()}"); // Should be false

// Debug analysis updates
var project = new Project { Name = "Test", Path = @"C:\Test" };
Debug.WriteLine($"Before analysis: {project.LastAnalyzedAt}");
project.UpdateLastAnalyzed();
Debug.WriteLine($"After analysis: {project.LastAnalyzedAt}");

// Debug namespace relationships
Debug.WriteLine($"Namespace count: {project.Namespaces.Count}");
foreach (var ns in project.Namespaces)
{
    Debug.WriteLine($"Namespace: {ns.FullName}, Depth: {ns.Depth}");
}
```

## Cross-References

**Related Classes:**
- `BaseEntity`: Base class providing infrastructure functionality
- `ProjectNamespace`: Child entities organized within projects
- `ProjectEntity`: Alternative project entity used in workspace context
- `Workspace`: Potential parent container (in some organizational models)

**Upstream Callers:**
- `Snitcher.Service.Services.ProjectService`: Primary business logic consumer
- `Snitcher.Repository.Repositories.ProjectRepository`: Persistence operations
- Analysis services for timestamp updates
- UI layer for project management operations

**Downstream Dependencies:**
- `ProjectNamespace` entities through navigation properties
- Validation logic in service layer
- Repository layer for persistence operations

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md`, `ProjectNamespace.md`
- **After:** `ProjectService.md`, `ProjectRepository.md`
- **Related:** `SnitcherDbContext.md` (for persistence configuration)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Aggregate Root Pattern:** Managing hierarchical entity relationships
- **Project Organization Model:** Structuring codebase management entities
- **Timestamp Tracking:** Analysis history and audit trail implementation
- **Validation Separation:** Basic entity validation vs. complex business rules
- **Navigation Property Management:** Entity relationship patterns

**What Is Project-Specific:**
- **Analysis Tracking:** Specific to code analysis application requirements
- **File System Path Storage:** Tailored to this application's needs
- **Namespace Organization:** Specific to code structure analysis

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Core Identity:** Inherit from BaseEntity for infrastructure
2. **Add Business Properties:** Name, description, path, version fields
3. **Implement Validation:** Basic IsValid() method for required fields
4. **Add Timestamp Tracking:** Analysis history with UpdateLastAnalyzed()
5. **Configure Relationships:** Navigation properties for child entities
6. **Maintain Separation:** Keep file system operations in service layer
7. **Ensure Testability:** Design for easy unit testing and mocking

**Key Architectural Insights:**
- **Entity Boundaries:** Keep entities focused on data and basic validation
- **Service Layer Delegation:** External operations belong in services
- **Audit Trail Consistency:** Timestamp updates should be centralized
- **Relationship Management:** Navigation properties simplify hierarchical data
- **Validation Strategy:** Basic validation in entities, complex in services

**Implementation Checklist for New Projects:**
- [ ] Define clear entity boundaries and responsibilities
- [ ] Implement basic validation in IsValid() method
- [ ] Add timestamp tracking for business operations
- [ ] Configure navigation properties for relationships
- [ ] Ensure infrastructure independence
- [ ] Add comprehensive unit tests
- [ ] Document validation rules and invariants
- [ ] Plan for database schema evolution
