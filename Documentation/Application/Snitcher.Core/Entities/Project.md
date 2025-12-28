# Project

## Overview

Project is a concrete domain entity representing a software project within the Snitcher application. It serves as the primary organizational unit for code analysis, tracking project metadata, analysis history, and containing related namespaces. Projects are the central concept that users interact with when managing code analysis workflows.

**Why it exists**: To provide a structured representation of software projects that can be analyzed, tracked, and managed within the Snitcher ecosystem. It encapsulates all essential project information and provides business logic for project operations.

**Problem it solves**: Without a dedicated Project entity, the application would lack a coherent way to organize and track software projects, making it impossible to associate analysis results, manage project metadata, or maintain project hierarchies.

**What would break if removed**: The entire project management functionality would cease to exist. Users could not create, manage, or analyze software projects. Database tables would lose their primary organizational structure, and all project-related features would fail.

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
- MUST encapsulate project data and behavior
- MUST provide validation for project state
- MUST maintain analysis tracking information
- MUST NOT contain persistence logic
- MUST NOT depend on external services

**What it MUST do**:
- Store essential project information (name, path, description, version)
- Track analysis history and timestamps
- Validate project state and business rules
- Manage namespace relationships

**What it MUST NOT do**:
- Access databases or external APIs
- Handle file system operations directly
- Implement UI-specific logic
- Contain complex business workflows

**Dependencies (incoming)**: Service layer, Repository layer, Application layer

**Dependencies (outgoing)**: BaseEntity, ProjectNamespace collection

## Execution Flow

**Where execution starts**: Project entities are created when users initiate new projects or when Entity Framework materializes existing projects from the database.

**How control reaches this component**:
1. User creates new project through UI
2. Service layer instantiates Project entity
3. Repository layer persists Project to database
4. Entity Framework loads Project for queries

**Call sequence (step-by-step)**:
1. Service layer receives create project request
2. Project entity is instantiated with provided data
3. Validation occurs via IsValid() method
4. Entity is passed to repository for persistence
5. Database stores project with generated ID and timestamps

**Synchronous vs asynchronous behavior**: Synchronous - all operations are in-memory

**Threading / dispatcher / event loop notes**: Thread-safe for read operations, but instances should not be shared across threads without synchronization

**Lifecycle (creation → usage → disposal)**:
1. Creation: Constructor initializes with default values
2. Population: Properties set with project data
3. Validation: IsValid() called to verify state
4. Analysis: UpdateLastAnalyzed() called after analysis
5. Persistence: Entity saved to database
6. Disposal: Garbage collected when no longer referenced

## Public API / Surface Area

**Constructors**:
- `public Project()`: Default constructor for Entity Framework

**Public methods**:
- `void UpdateLastAnalyzed()`: Updates the LastAnalyzedAt timestamp to current time
- `bool IsValid()`: Validates that the project has required data (name and path)

**Properties**:
- `string Name`: Project name (required, must be unique)
- `string? Description`: Optional project description
- `string Path`: File system path to project root (required)
- `string? Version`: Optional project version identifier
- `DateTime? LastAnalyzedAt`: Timestamp of last analysis (nullable)
- `ICollection<ProjectNamespace> Namespaces`: Navigation property to related namespaces

**Events**: None

**Expected input/output**:
- Input: Project data through property setters
- Output: Validation results and analysis timestamp updates

**Side effects**:
- Updates LastAnalyzedAt when analysis occurs
- No external side effects

**Error behavior**:
- No exceptions thrown for validation failures (returns false)
- Properties can be set to invalid values but IsValid() will detect them
- Null reference exceptions possible if Namespaces accessed before initialization

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Properties (lines 13-43)**:
```csharp
public string Name { get; set; } = string.Empty;
public string? Description { get; set; }
public string Path { get; set; } = string.Empty;
public string? Version { get; set; }
public DateTime? LastAnalyzedAt { get; set; }
public virtual ICollection<ProjectNamespace> Namespaces { get; set; } = new List<ProjectNamespace>();
```
- Name and Path are required fields initialized to empty strings
- Description and Version are optional nullable fields
- LastAnalyzedAt tracks when project was last analyzed
- Namespaces provides navigation to child namespace entities
- Virtual allows Entity Framework proxy creation

**UpdateLastAnalyzed method (lines 48-52)**:
```csharp
public void UpdateLastAnalyzed()
{
    LastAnalyzedAt = DateTime.UtcNow;
    UpdateTimestamp();
}
```
- Sets LastAnalyzedAt to current UTC time
- Calls base class UpdateTimestamp to maintain audit trail
- Used by analysis services to track when analysis occurred

**IsValid method (lines 58-62)**:
```csharp
public bool IsValid()
{
    return !string.IsNullOrWhiteSpace(Name) && 
           !string.IsNullOrWhiteSpace(Path);
}
```
- Validates that required fields are present and not just whitespace
- Used by service layer for business rule validation
- Returns false for projects missing name or path

**Algorithms used**:
- Simple string validation for required fields
- UTC timestamp tracking for audit purposes
- Collection initialization for navigation properties

**Conditional logic explanation**:
- String.IsNullOrWhiteSpace checks for null, empty, or whitespace-only strings
- Logical AND ensures both required fields are valid
- No complex branching - straightforward validation logic

**State transitions**:
- Created → Valid (when name and path are set)
- Valid → Analyzed (when UpdateLastAnalyzed called)
- Any state → Modified (via base UpdateTimestamp)

**Important invariants**:
- Name and Path must not be null or empty for valid projects
- LastAnalyzedAt is null until first analysis
- Namespaces collection is never null (initialized as empty list)

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Active Record Pattern**: Entity contains behavior for state management
- **Validation Pattern**: IsValid() method provides validation logic
- **Aggregate Root Pattern**: Project serves as root for namespace entities

**Architectural patterns**:
- **Domain-Driven Design (DDD)**: Rich domain model with behavior
- **Clean Architecture**: Pure domain model with no infrastructure dependencies

**Why these patterns were chosen (inferred)**:
- Active Record keeps related behavior with data
- Validation in entity ensures data integrity
- Aggregate Root establishes clear ownership hierarchy

**Trade-offs**:
- Rich domain model vs anemic entities: More behavior but better encapsulation
- Validation in entity vs separate validator: Simpler but less flexible
- Required fields vs optional: Enforces business rules but reduces flexibility

**Anti-patterns avoided or possibly introduced**:
- Avoided: Anemic domain model (entity contains behavior)
- Avoided: God object (focused on project concerns only)
- Possible risk: Validation logic may become complex over time

## Binding / Wiring / Configuration

**Dependency injection**: None - Project entities are not registered in DI container

**Data binding (if UI)**: N/A - domain layer

**Configuration sources**: None - behavior is hardcoded

**Runtime wiring**: Entity Framework automatically maps properties to database columns

**Registration points**: None - entities are discovered by convention

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
// Create new project
var project = new Project 
{ 
    Name = "MyWebApp", 
    Path = @"C:\Projects\MyWebApp" 
};

// Validate project
if (project.IsValid())
{
    // Save to database
    await repository.AddAsync(project);
}
```

**Realistic example**:
```csharp
public class ProjectService
{
    public async Task<Project> CreateProjectAsync(string name, string path, string? description = null)
    {
        var project = new Project
        {
            Name = name,
            Path = path,
            Description = description,
            Version = "1.0.0"
        };

        if (!project.IsValid())
            throw new ArgumentException("Project name and path are required");

        return await repository.AddAsync(project);
    }

    public async Task MarkProjectAnalyzedAsync(Guid projectId)
    {
        var project = await repository.GetByIdAsync(projectId);
        if (project != null)
        {
            project.UpdateLastAnalyzed();
            await repository.UpdateAsync(project);
        }
    }
}
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Not validating before saving
var project = new Project { Name = "", Path = "" };
await repository.AddAsync(project); // Will save invalid data

// WRONG: Assuming analysis tracking is automatic
var project = new Project { Name = "Test", Path = @"C:\Test" };
await repository.AddAsync(project);
// project.LastAnalyzedAt is still null - need to call UpdateLastAnalyzed()

// WRONG: Modifying collection directly
project.Namespaces = null; // Breaks Entity Framework expectations
// Should use project.Namespaces.Clear() or modify existing collection
```

**How to test this in isolation**:
```csharp
[Test]
public void Project_ShouldValidateCorrectly()
{
    // Arrange
    var validProject = new Project { Name = "Test", Path = @"C:\Test" };
    var invalidProject = new Project { Name = "", Path = "" };

    // Act & Assert
    Assert.That(validProject.IsValid(), Is.True);
    Assert.That(invalidProject.IsValid(), Is.False);
}

[Test]
public void Project_ShouldTrackAnalysisTimestamp()
{
    // Arrange
    var project = new Project { Name = "Test", Path = @"C:\Test" };
    var before = DateTime.UtcNow;

    // Act
    project.UpdateLastAnalyzed();

    // Assert
    Assert.That(project.LastAnalyzedAt, Is.GreaterThanOrEqualTo(before));
    Assert.That(project.LastAnalyzedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
}
```

**How to mock or replace it**:
```csharp
// For testing services that depend on Project entities
public class MockProject : Project
{
    public MockProject(Guid id) 
    {
        // Use reflection to set private Id for testing
        typeof(BaseEntity).GetProperty(nameof(Id))?.SetValue(this, id);
    }
}

// Or create test data builders
public class ProjectBuilder
{
    private Project _project = new();
    
    public ProjectBuilder WithName(string name)
    {
        _project.Name = name;
        return this;
    }
    
    public ProjectBuilder WithPath(string path)
    {
        _project.Path = path;
        return this;
    }
    
    public Project Build() => _project;
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new properties for additional project metadata
2. Add validation methods for new business rules
3. Add behavior methods for project operations
4. Update IsValid() to include new validation

**Where NOT to add logic**:
- Don't add database access or persistence logic
- Don't add file system operations or I/O
- Don't add UI-specific properties or methods
- Don't add complex workflow orchestration

**Safe extension points**:
- New properties can be added with appropriate validation
- New methods can be added for project-specific behavior
- Existing methods can be overridden if virtual (none currently are)

**Common mistakes**:
- Adding too many properties (violates Single Responsibility)
- Making validation too complex in the entity
- Adding dependencies on external services
- Forgetting to initialize collection properties

**Refactoring warnings**:
- Changing property types will break database mappings
- Removing properties will break existing data
- Changing validation logic may affect business rules
- Making methods virtual may affect performance

## Failure Modes & Debugging

**Common runtime errors**:
- **NullReferenceException**: If Namespaces collection is accessed after being set to null
- **ArgumentException**: From calling code when validation fails (entity itself doesn't throw)
- **InvalidOperationException**: From Entity Framework if navigation properties are misconfigured

**Null/reference risks**:
- Name and Path can be empty strings but not null (due to initialization)
- Description and Version can be null (intended)
- LastAnalyzedAt can be null (intended)
- Namespaces collection should never be null

**Performance risks**:
- Large Namespaces collections can impact serialization
- String operations in IsValid are minimal impact
- DateTime operations are lightweight

**Logging points**:
- No built-in logging (domain layer separation)
- ToString() from BaseEntity useful for debug output
- IsValid() results should be logged at service layer

**How to debug step-by-step**:
1. Set breakpoint in IsValid() to check validation logic
2. Watch Name and Path properties during validation
3. Monitor LastAnalyzedAt changes during analysis
4. Check Namespaces collection state during operations
5. Use BaseEntity.ToString() for entity identification

## Cross-References

**Related classes**:
- BaseEntity (inherits from)
- ProjectNamespace (contained in Namespaces collection)
- IProjectRepository (persists Project entities)
- ProjectService (contains business logic for projects)

**Upstream callers**:
- ProjectService creates and manages projects
- WorkspaceService manages projects within workspaces
- Analysis services update project analysis status

**Downstream dependencies**:
- ProjectNamespace entities depend on Project
- Database schema includes Project table
- Entity Framework mappings rely on Project structure

**Documents that should be read before/after**:
- Read: BaseEntity documentation (inheritance)
- Read: ProjectNamespace documentation (child entities)
- Read: IProjectRepository documentation (persistence)
- Read: ProjectService documentation (business logic)

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Domain entity pattern with validation
- Aggregate root pattern for entity hierarchies
- Audit trail tracking with timestamps
- Required field validation patterns
- Navigation property management

**What is project-specific**:
- Specific property names (Name, Path, Version, etc.)
- Analysis tracking functionality
- Namespace relationship structure
- Validation rules for project data

**How to recreate this pattern from scratch elsewhere**:
1. Define entity class inheriting from base entity
2. Add properties for domain-specific data
3. Initialize collection properties to avoid nulls
4. Add validation methods for business rules
5. Add behavior methods for entity operations
6. Ensure proper Entity Framework mapping
7. Follow domain-driven design principles

**Key insights for implementation**:
- Always initialize collection properties to avoid null reference exceptions
- Use nullable reference types for optional fields
- Include validation logic in the entity for data integrity
- Keep entities focused on domain concerns only
- Use personalizer tracking for audit purposes
- Consider performance implications of navigation properties
