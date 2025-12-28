# Workspace.cs (Domain Entity)

## Overview

`Workspace.cs` is a concrete domain entity representing a workspace in the Snitcher application. As the top-level organizational unit, workspaces contain projects and provide the primary structure for organizing development work. This entity extends BaseEntity to inherit identity, audit, and soft delete capabilities while adding workspace-specific business logic and validation.

**Why it exists**: To model the core business concept of workspaces, provide a container for project organization, enforce workspace-specific business rules, and maintain the relationship structure within the domain model.

**What problem it solves**: Defines the workspace concept with proper business validation, establishes the containment relationship with projects, enforces uniqueness constraints, and provides the foundation for workspace management operations.

**What would break if removed**: The domain model would lose its primary organizational concept, project containment would be undefined, workspace business rules would be lost, and the entire workspace management feature would cease to function.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure domain logic)

**Persistence**: Framework-agnostic (mapped by EF Core)

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Standard .NET runtime with collection support

## Architectural Role

**Layer**: Domain Layer (Entity)

**Responsibility Boundaries**:
- MUST model workspace business concepts
- MUST enforce workspace validation rules
- MUST maintain project relationships
- MUST NOT contain persistence logic
- MUST NOT handle UI-specific concerns

**What it MUST do**:
- Store workspace properties (name, description, path)
- Maintain collection of child projects
- Provide validation for business rules
- Support default workspace semantics
- Inherit base entity behaviors

**What it MUST NOT do**:
- Access database directly
- Implement file system operations
- Handle user interface logic
- Perform complex business calculations

**Dependencies (Incoming)**: Repository layer, service layer

**Dependencies (Outgoing**: BaseEntity (inheritance), ProjectEntity (navigation)

## Execution Flow

**Where execution starts**: Created by domain services or repository layer when new workspaces are instantiated

**How control reaches this component**:
1. Service layer creates workspace instance
2. Properties populated from user input or external data
3. Validation performed via IsValid() method
4. Entity passed to repository for persistence
5. Navigation properties loaded by EF Core

**Validation Flow** (lines 49-53):
1. Check name is not null or whitespace
2. Check path is not null or whitespace
3. Return combined validation result

**Relationship Flow**:
1. Projects collection initialized as empty List
2. EF Core populates collection on entity load
3. Business logic can manipulate collection
4. Changes tracked by Unit of Work pattern

**Synchronous vs asynchronous behavior**: All operations are synchronous - pure domain logic

**Threading/Dispatcher notes**: No threading concerns - in-memory entity operations

**Lifecycle**: Created by services → Managed by EF Core → Garbage collected when no longer referenced

## Public API / Surface Area

**Inheritance**: `class Workspace : BaseEntity`

**Properties**:
- `string Name` - Workspace display name (must be unique)
- `string? Description` - Optional workspace description
- `string Path` - File system path to workspace root
- `string? Version` - Optional workspace version
- `bool IsDefault` - Default workspace flag
- `ICollection<ProjectEntity> Projects` - Child projects navigation

**Methods**:
- `bool IsValid()` - Validates workspace business rules

**Inherited from BaseEntity**:
- `Guid Id` - Unique identifier
- `DateTime CreatedAt` - Creation timestamp
- `DateTime UpdatedAt` - Modification timestamp
- `bool IsDeleted` - Soft delete flag
- `MarkAsDeleted()`, `Restore()`, `UpdateTimestamp()`

**Expected Input/Output**: Properties store workspace data, IsValid() returns validation state, Projects collection provides navigation to child entities.

**Side Effects**:
- Property changes tracked by EF Core change tracker
- Projects collection modifications affect relationships
- IsValid() performs validation without side effects

**Error Behavior**: No explicit error handling - relies on .NET framework exceptions for invalid operations.

## Internal Logic Breakdown

**Property Definitions** (lines 13-43):
```csharp
public string Name { get; set; } = string.Empty;
public string? Description { get; set; }
public string Path { get; set; } = string.Empty;
public string? Version { get; set; }
public bool IsDefault { get; set; } = false;
public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
```
- Name and Path required for business validity
- Description and Version optional for additional context
- IsDefault enforces single default workspace constraint
- Projects navigation enables one-to-many relationship
- Virtual collection enables EF Core lazy loading

**Validation Logic** (lines 49-53):
```csharp
public bool IsValid()
{
    return !string.IsNullOrWhiteSpace(Name) && 
           !string.IsNullOrWhiteSpace(Path);
}
```
- Simple null/whitespace validation
- Enforces business rule that workspaces must have name and path
- Could be extended with more complex validation rules
- Used by service layer for input validation

**Navigation Property** (lines 43):
```csharp
public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
```
- Virtual enables EF Core proxy creation and lazy loading
- Initialized as List to prevent null reference issues
- Collection type enables EF Core change tracking
- One-to-many relationship with ProjectEntity

**Default Workspace Pattern**:
- IsDefault property enables marking special workspace
- Business logic must enforce only one default exists
- Used by application for initial workspace selection

## Patterns & Principles Used

**Entity Pattern**: Rich domain model with behavior and validation

**Aggregate Root Pattern**: Workspace is root of workspace-project aggregate

**Validation Pattern**: IsValid() method encapsulates business rules

**Navigation Property Pattern**: Virtual collections for relationship mapping

**Inheritance Pattern**: Extends BaseEntity for common behaviors

**Why these patterns were chosen**:
- Entity pattern for rich domain modeling
- Aggregate root for transactional consistency
- Validation for business rule enforcement
- Navigation for EF Core relationship mapping
- Inheritance to eliminate code duplication

**Trade-offs**:
- Simple validation may be insufficient for complex rules
- Navigation property couples to EF Core
- Default workspace logic scattered across application
- Validation method returns boolean instead of detailed results

**Anti-patterns avoided**:
- No anemic domain model (has validation behavior)
- No violation of encapsulation (properties properly scoped)
- No direct database access
- No business logic in service layer only

## Binding / Wiring / Configuration

**Data Binding**: Not applicable (domain entity)

**Configuration Sources**: No external configuration needed

**Runtime Wiring**:
- Mapped by EF Core Fluent API in Repository layer
- Used by domain services for business operations
- Recognized by application services for DTO mapping

**Framework Integration**:
- Entity Framework Core entity mapping
- Repository pattern implementation
- Domain service layer operations

## Example Usage

**Minimal Example**:
```csharp
// Create workspace
var workspace = new Workspace
{
    Name = "My Workspace",
    Path = @"C:\Workspaces\MyWorkspace"
};

// Validate
if (workspace.IsValid())
{
    // Save workspace
}
```

**Realistic Example**:
```csharp
// Create default workspace
var defaultWorkspace = new Workspace
{
    Name = "Default Workspace",
    Description = "Default workspace for new projects",
    Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Snitcher", "Default"),
    IsDefault = true
};

// Add project
var project = new ProjectEntity
{
    Name = "My Project",
    Workspace = defaultWorkspace
};
defaultWorkspace.Projects.Add(project);
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't create invalid workspace
var workspace = new Workspace(); // Name and Path are empty
workspace.IsValid(); // Returns false

// BAD - Don't manipulate navigation collection directly in some contexts
workspace.Projects = null; // Breaks EF Core tracking
```

**How to test in isolation**:
```csharp
[Test]
public void Workspace_ShouldValidateRequiredFields()
{
    var workspace = new Workspace();
    Assert.IsFalse(workspace.IsValid());
    
    workspace.Name = "Test";
    Assert.IsFalse(workspace.IsValid());
    
    workspace.Path = @"C:\Test";
    Assert.IsTrue(workspace.IsValid());
}

[Test]
public void Workspace_ShouldInheritBaseEntityBehavior()
{
    var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    
    Assert.AreNotEqual(Guid.Empty, workspace.Id);
    Assert.IsFalse(workspace.IsDeleted);
    
    workspace.MarkAsDeleted();
    Assert.IsTrue(workspace.IsDeleted);
}
```

**How to mock or replace**:
- Create test instances directly
- Use factory methods for test data
- Mock only when testing interfaces that depend on Workspace

## Extension & Modification Guide

**How to add new workspace behavior**:
1. Add new properties for workspace-specific data
2. Extend IsValid() method with additional validation
3. Add domain methods for workspace operations
4. Consider impact on EF Core mapping

**Where NOT to add logic**:
- Don't add file system operations
- Don't add UI-specific properties
- Don't add persistence logic

**Safe extension points**:
- Additional validation rules in IsValid()
- New domain-specific methods
- Computed properties for derived data
- Domain events for workspace operations

**Common mistakes**:
- Adding complex validation that should be in service layer
- Making navigation properties non-virtual
- Forgetting to initialize collection properties
- Adding business logic that depends on external services

**Refactoring warnings**:
- Changing property types affects database schema
- Modifying validation affects service layer
- Adding new navigation properties affects EF Core mapping
- Consider impact on existing workspace instances

## Failure Modes & Debugging

**Common runtime errors**:
- NullReferenceException if Projects collection set to null
- InvalidOperationException if EF Core tracking broken
- ValidationException if business rules violated

**Null/reference risks**:
- Description and Version can be null - handled by nullable types
- Projects collection initialized to prevent null references
- Name and Path validated to prevent empty values

**Performance risks**:
- Large Projects collection can impact memory usage
- Lazy loading of Projects can cause N+1 query problems
- Validation called frequently may impact performance

**Logging points**: None in entity - logging handled by calling code

**How to debug step-by-step**:
1. Set breakpoints in property setters to monitor changes
2. Debug IsValid() method for validation issues
3. Monitor Projects collection for relationship problems
4. Verify inheritance from BaseEntity works correctly
5. Test entity lifecycle with EF Core operations

## Cross-References

**Related classes**:
- `BaseEntity` (base class)
- `ProjectEntity` (child entities)
- `IWorkspaceRepository` (data access)
- `WorkspaceService` (business logic)

**Upstream callers**:
- Repository layer (persistence)
- Service layer (business operations)
- Application layer (DTO mapping)

**Downstream dependencies**:
- BaseEntity (inheritance)
- ProjectEntity (navigation)

**Documents to read before/after**:
- Before: `BaseEntity.cs` (base functionality)
- After: `ProjectEntity.cs` (child entities)
- After: Repository pattern documentation

## Knowledge Transfer Notes

**Reusable concepts**:
- Domain entity pattern with rich behavior
- Validation encapsulation in entities
- Aggregate root pattern for transaction boundaries
- Navigation property pattern for relationships
- Inheritance for shared entity behavior

**Project-specific elements**:
- Snitcher workspace domain concept
- File system path integration
- Default workspace business rule
- Project containment relationship

**How to recreate pattern elsewhere**:
1. Create entity class inheriting from base entity
2. Add business-specific properties
3. Implement validation method for business rules
4. Add navigation properties for relationships
5. Use virtual collections for ORM compatibility
6. Initialize collections to prevent null references

**Key insights**:
- Keep validation simple and rule-based
- Use navigation properties for relationship modeling
- Initialize collections to prevent null reference exceptions
- Consider aggregate boundaries when defining relationships
- Make navigation properties virtual for ORM compatibility
- Use nullable types for optional properties
- Provide clear validation feedback through IsValid() method
