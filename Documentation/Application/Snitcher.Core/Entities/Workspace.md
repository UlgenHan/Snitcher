# Workspace

## Overview

Workspace is a concrete domain entity representing a top-level organizational container within the Snitcher application. It serves as the highest level of organization, containing multiple projects and providing a logical grouping mechanism for related software projects. Workspaces enable users to organize their development work into separate contexts, such as personal projects, work projects, or client-specific groupings.

**Why it exists**: To provide a hierarchical organization structure for projects, allowing users to manage multiple unrelated or related projects within separate contexts. It enables better organization, access control, and project management at a macro level.

**Problem it solves**: Without workspaces, all projects would exist in a flat structure, making it difficult to organize large numbers of projects or separate projects by context, client, or purpose. Workspaces provide the necessary organizational hierarchy.

**What would break if removed**: The entire organizational hierarchy would collapse. Users could not group projects logically, making the application difficult to use with multiple projects. Default workspace management and project categorization would fail.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0

**Libraries**: None (pure domain model)

**UI frameworks**: N/A (domain layer)

**Persistence / communication technologies**: Entity Framework Core (implicit through attributes)

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, Entity Framework Core for persistence

**Version hints**: Uses modern C# features and nullable reference types

## Architectural Role

**Layer**: Domain Layer (Core)

**Responsibility boundaries**:
- MUST encapsulate workspace data and behavior
- MUST provide validation for workspace state
- MUST manage project relationships
- MUST handle default workspace designation
- MUST NOT contain persistence logic
- MUST NOT depend on external services

**What it MUST do**:
- Store essential workspace information (name, path, description, version)
- Manage collection of contained projects
- Track default workspace status
- Validate workspace state and business rules
- Provide organizational structure for projects

**What it MUST NOT do**:
- Access databases or external APIs
- Handle file system operations directly
- Implement UI-specific logic
- Contain complex business workflows beyond basic validation

**Dependencies (incoming)**: Service layer, Repository layer, Application layer

**Dependencies (outgoing)**: BaseEntity, ProjectEntity collection

## Execution Flow

**Where execution starts**: Workspace entities are created when users initiate new workspaces or when Entity Framework materializes existing workspaces from the database.

**How control reaches this component**:
1. User creates new workspace through UI
2. Service layer instantiates Workspace entity
3. Repository layer persists Workspace to database
4. Entity Framework loads Workspace for queries
5. Default workspace is created automatically on first run

**Call sequence (step-by-step)**:
1. Service layer receives create workspace request
2. Workspace entity is instantiated with provided data
3. Validation occurs via IsValid() method
4. Entity is passed to repository for persistence
5. Database stores workspace with generated ID and timestamps
6. Default workspace flag is set if applicable

**Synchronous vs asynchronous behavior**: Synchronous - all operations are in-memory

**Threading / dispatcher / event loop notes**: Thread-safe for read operations, but instances should not be shared across threads without synchronization

**Lifecycle (creation → usage → disposal)**:
1. Creation: Constructor initializes with default values
2. Population: Properties set with workspace data
3. Validation: IsValid() called to verify state
4. Default designation: IsDefault set if this is the default workspace
5. Persistence: Entity saved to database
6. Project management: Projects collection managed throughout lifecycle
7. Disposal: Garbage collected when no longer referenced

## Public API / Surface Area

**Constructors**:
- `public Workspace()`: Default constructor for Entity Framework

**Public methods**:
- `bool IsValid()`: Validates that the workspace has required data (name and path)

**Properties**:
- `string Name`: Workspace name (required, must be unique)
- `string? Description`: Optional workspace description
- `string Path`: File system path to workspace root (required)
- `string? Version`: Optional workspace version identifier
- `bool IsDefault`: Indicates if this is the default workspace (default: false)
- `ICollection<ProjectEntity> Projects`: Navigation property to contained projects

**Events**: None

**Expected input/output**:
- Input: Workspace data through property setters
- Output: Validation results through IsValid() method

**Side effects**:
- No direct side effects beyond state management
- Changes to IsDefault affect application-level default workspace logic

**Error behavior**:
- No exceptions thrown for validation failures (returns false)
- Properties can be set to invalid values but IsValid() will detect them
- Null reference exceptions possible if Projects accessed before initialization

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Properties (lines 13-43)**:
```csharp
public string Name { get; set; } = string.Empty;
public string? Description { get; set; }
public string Path { get; set; } = string.Empty;
public string? Version { get; set; }
public bool IsDefault { get; set; } = false;
public virtual ICollection<ProjectEntity> Projects { get; set; } = new List<ProjectEntity>();
```
- Name and Path are required fields initialized to empty strings
- Description and Version are optional nullable fields
- IsDefault tracks if this is the application's default workspace
- Projects provides navigation to child project entities
- Virtual allows Entity Framework proxy creation

**IsValid method (lines 49-53)**:
```csharp
public bool IsValid()
{
    return !string.IsNullOrWhiteSpace(Name) && 
           !string.IsNullOrWhiteSpace(Path);
}
```
- Validates that required fields are present and not just whitespace
- Used by service layer for business rule validation
- Returns false for workspaces missing name or path
- Does not validate IsDefault flag (business rule concern)

**Algorithms used**:
- Simple string validation for required fields
- Collection initialization for navigation properties
- Boolean flag management for default workspace tracking

**Conditional logic explanation**:
- String.IsNullOrWhiteSpace checks for null, empty, or whitespace-only strings
- Logical AND ensures both required fields are valid
- No complex branching - straightforward validation logic

**State transitions**:
- Created → Valid (when name and path are set)
- Valid → Default (when IsDefault set to true)
- Any state → Modified (via base UpdateTimestamp)

**Important invariants**:
- Name and Path must not be null or empty for valid workspaces
- Only one workspace should have IsDefault = true (enforced at service layer)
- Projects collection is never null (initialized as empty list)
- Default workspace cannot be deleted (enforced at service layer)

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Aggregate Root Pattern**: Workspace serves as root for project entities
- **Active Record Pattern**: Entity contains behavior for state management
- **Validation Pattern**: IsValid() method provides validation logic

**Architectural patterns**:
- **Domain-Driven Design (DDD)**: Rich domain model with behavior
- **Clean Architecture**: Pure domain model with no infrastructure dependencies

**Why these patterns were chosen (inferred)**:
- Aggregate Root establishes clear ownership hierarchy
- Active Record keeps related behavior with data
- Validation in entity ensures data integrity

**Trade-offs**:
- Rich domain model vs anemic entities: More behavior but better encapsulation
- Validation in entity vs separate validator: Simpler but less flexible
- Default workspace flag in entity vs service: Simpler but mixes concerns

**Anti-patterns avoided or possibly introduced**:
- Avoided: Anemic domain model (entity contains behavior)
- Avoided: God object (focused on workspace concerns only)
- Possible risk: Default workspace logic may become complex

## Binding / Wiring / Configuration

**Dependency injection**: None - Workspace entities are not registered in DI container

**Data binding (if UI)**: N/A - domain layer

**Configuration sources**: None - behavior is hardcoded

**Runtime wiring**: Entity Framework automatically maps properties to database columns

**Registration points**: None - entities are discovered by convention

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Create new workspace
var workspace = new Workspace 
{ 
    Name = "MyProjects", 
    Path = @"C:\Projects\MyProjects" 
};

// Validate workspace
if (workspace.IsValid())
{
    // Save to database
    await repository.AddAsync(workspace);
}
```

**Realistic example**:
```csharp
public class WorkspaceService
{
    public async Task<Workspace> CreateWorkspaceAsync(string name, string path, string? description = null)
    {
        var workspace = new Workspace
        {
            Name = name,
            Path = path,
            Description = description,
            Version = "1.0.0",
            IsDefault = false // Don't make new workspaces default by default
        };

        if (!workspace.IsValid())
            throw new ArgumentException("Workspace name and path are required");

        return await repository.AddAsync(workspace);
    }

    public async Task<Workspace> CreateDefaultWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Default Workspace",
            Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Snitcher", "Default"),
            Description = "Default workspace for Snitcher projects",
            IsDefault = true
        };

        return await repository.AddAsync(workspace);
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Not validating before saving
var workspace = new Workspace { Name = "", Path = "" };
await repository.AddAsync(workspace); // Will save invalid data

// WRONG: Assuming default workspace enforcement
var workspace1 = new Workspace { Name = "WS1", Path = @"C:\WS1", IsDefault = true };
var workspace2 = new Workspace { Name = "WS2", Path = @"C:\WS2", IsDefault = true };
await repository.AddAsync(workspace1);
await repository.AddAsync(workspace2);
// Entity allows multiple defaults - business rule enforcement needed

// WRONG: Modifying collection directly
workspace.Projects = null; // Breaks Entity Framework expectations
// Should use workspace.Projects.Clear() or modify existing collection
```

**How to test this in isolation**:
```csharp
[Test]
public void Workspace_ShouldValidateCorrectly()
{
    // Arrange
    var validWorkspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    var invalidWorkspace = new Workspace { Name = "", Path = "" };

    // Act & Assert
    Assert.That(validWorkspace.IsValid(), Is.True);
    Assert.That(invalidWorkspace.IsValid(), Is.False);
}

[Test]
public void Workspace_ShouldHandleDefaultFlag()
{
    // Arrange & Act
    var workspace = new Workspace { Name = "Test", Path = @"C:\Test" };
    
    // Assert
    Assert.That(workspace.IsDefault, Is.False);
    
    // Act
    workspace.IsDefault = true;
    
    // Assert
    Assert.That(workspace.IsDefault, Is.True);
}
```

**How to mock or replace it**:
```csharp
// For testing services that depend on Workspace entities
public class MockWorkspace : Workspace
{
    public MockWorkspace(Guid id, string name, string path)
    {
        // Use reflection to set private properties for testing
        typeof(BaseEntity).GetProperty(nameof(Id))?.SetValue(this, id);
        Name = name;
        Path = path;
    }
}

// Or create test data builders
public class WorkspaceBuilder
{
    private Workspace _workspace = new();
    
    public WorkspaceBuilder WithName(string name)
    {
        _workspace.Name = name;
        return this;
    }
    
    public WorkspaceBuilder WithPath(string path)
    {
        _workspace.Path = path;
        return this;
    }
    
    public WorkspaceBuilder AsDefault()
    {
        _workspace.IsDefault = true;
        return this;
    }
    
    public Workspace Build() => _workspace;
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new properties for additional workspace metadata
2. Add validation methods for new business rules
3. Add behavior methods for workspace operations
4. Update IsValid() to include new validation

**Where NOT to add logic**:
- Don't add database access or persistence logic
- Don't add file system operations or I/O
- Don't add UI-specific properties or methods
- Don't add complex workflow orchestration

**Safe extension points**:
- New properties can be added with appropriate validation
- New methods can be added for workspace-specific behavior
- Existing properties can be enhanced with validation logic

**Common mistakes**:
- Adding too many properties (violates Single Responsibility)
- Making validation too complex in the entity
- Adding dependencies on external services
- Forgetting to initialize collection properties
- Setting multiple workspaces as default without validation

**Refactoring warnings**:
- Changing property types will break database mappings
- Removing properties will break existing data
- Changing validation logic may affect business rules
- Modifying IsDefault behavior affects application startup

## Failure Modes & Debugging

**Common runtime errors**:
- **NullReferenceException**: If Projects collection is accessed after being set to null
- **ArgumentException**: From calling code when validation fails (entity itself doesn't throw)
- **InvalidOperationException**: From Entity Framework if navigation properties are misconfigured

**Null/reference risks**:
- Name and Path can be empty strings but not null (due to initialization)
- Description and Version can be null (intended)
- Projects collection should never be null
- IsDefault is boolean, never null

**Performance risks**:
- Large Projects collections can impact serialization
- String operations in IsValid are minimal impact
- Collection initialization overhead is minimal

**Logging points**:
- No built-in logging (domain layer separation)
- ToString() from BaseEntity useful for debug output
- IsValid() results should be logged at service layer
- IsDefault changes should be logged for audit purposes

**How to debug step-by-step**:
1. Set breakpoint in IsValid() to check validation logic
2. Watch Name and Path properties during validation
3. Monitor IsDefault flag changes during operations
4. Check Projects collection state during operations
5. Use BaseEntity.ToString() for entity identification

## Cross-References

**Related classes**:
- BaseEntity (inherits from)
- ProjectEntity (contained in Projects collection)
- IWorkspaceRepository (persists Workspace entities)
- WorkspaceService (contains business logic for workspaces)

**Upstream callers**:
- WorkspaceService creates and manages workspaces
- Application startup creates default workspace
- UI layer manages workspace selection and operations

**Downstream dependencies**:
- ProjectEntity entities depend on Workspace
- Database schema includes Workspace table
- Entity Framework mappings rely on Workspace structure

**Documents that should be read before/after**:
- Read: BaseEntity documentation (inheritance)
- Read: ProjectEntity documentation (child entities)
- Read: IWorkspaceRepository documentation (persistence)
- Read: WorkspaceService documentation (business logic)

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Aggregate root pattern for entity hierarchies
- Domain entity pattern with validation
- Default entity management patterns
- Collection initialization best practices
- Organizational hierarchy modeling

**What is project-specific**:
- Specific property names (Name, Path, Version, etc.)
- Default workspace concept
- Project relationship structure
- Validation rules for workspace data

**How to recreate this pattern from scratch elsewhere**:
1. Define aggregate root entity class inheriting from base entity
2. Add properties for organizational data
3. Initialize collection properties to avoid nulls
4. Add validation methods for business rules
5. Add default entity management logic
6. Ensure proper Entity Framework mapping
7. Follow domain-driven design principles

**Key insights for implementation**:
- Always initialize collection properties to avoid null reference exceptions
- Use nullable reference types for optional fields
- Include validation logic in the entity for data integrity
- Keep entities focused on domain concerns only
- Consider default entity management at application level
- Use aggregate root pattern to establish clear ownership
