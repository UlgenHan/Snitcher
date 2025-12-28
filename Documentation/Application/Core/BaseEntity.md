# BaseEntity

## Overview

BaseEntity is the foundational abstract class for all domain entities in the Snitcher application. It provides common infrastructure functionality including unique identification, audit trails, soft delete capabilities, and standard entity lifecycle management. This class implements the core domain patterns that ensure consistency across all entities in the system.

The class exists to eliminate code duplication and enforce architectural consistency. All entities inherit from this base class to automatically gain audit fields, soft delete functionality, and proper equality semantics. Without BaseEntity, each entity would need to implement this boilerplate code independently, leading to inconsistency and maintenance overhead.

If BaseEntity were removed, the system would lose:
- Automatic audit trail functionality (CreatedAt, UpdatedAt)
- Soft delete capabilities for data recovery
- Consistent equality comparison based on entity identity
- Standardized entity lifecycle management
- Built-in timestamp tracking for change detection

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- .NET 8.0 Base Class Library
- Entity Framework Core (implicit through annotations)

**Libraries:**
- System namespace for core functionality
- Snitcher.Core.Interfaces for domain contracts

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- Designed for Entity Framework Core integration
- Supports both relational and non-relational storage through abstraction

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- NuGet package management

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires System.Guid support
- UTC DateTime handling for timestamp consistency

**Version Hints:**
- Uses modern C# features (nullable reference types, expression-bodied members)
- Compatible with .NET 8.0 and later versions

## Architectural Role

**Layer:** Domain Layer (Clean Architecture - Core)

**Responsibility Boundaries:**
- MUST provide common entity infrastructure
- MUST maintain audit trail integrity
- MUST support soft delete operations
- MUST NOT contain business logic specific to any entity type
- MUST NOT depend on external infrastructure (databases, services)

**What it MUST do:**
- Generate unique identifiers for new entities
- Track creation and modification timestamps
- Provide soft delete functionality
- Implement proper equality semantics
- Support entity lifecycle management

**What it MUST NOT do:**
- Persist data directly (no database dependencies)
- Validate business rules (delegated to specific entities)
- Handle external service communication
- Contain domain-specific logic

**Dependencies (Incoming):**
- All concrete entities (Project, Workspace, etc.)
- Repository layer for persistence
- Service layer for business operations

**Dependencies (Outgoing):**
- IEntity interface for identity contract
- IAuditableEntity interface for audit capabilities
- ISoftDeletable interface for delete functionality

## Execution Flow

**Where execution starts:**
- Entity instantiation through constructors of derived classes
- Direct method calls for soft delete operations
- Equality comparisons in collections and repositories

**How control reaches this component:**
1. Derived entity constructor calls protected BaseEntity constructor
2. Repository layer calls UpdateTimestamp() during entity modifications
3. Business logic calls MarkAsDeleted() or Restore() for soft delete operations
4. Collection operations trigger Equals() and GetHashCode() for identity comparison

**Call Sequence (Entity Creation):**
1. Derived entity constructor invoked
2. BaseEntity protected constructor executes
3. Guid.NewGuid() generates unique Id
4. DateTime.UtcNow sets CreatedAt and UpdatedAt
5. IsDeleted initialized to false
6. Control returns to derived constructor

**Call Sequence (Soft Delete):**
1. Business logic calls MarkAsDeleted()
2. IsDeleted property set to true
3. UpdateTimestamp() called to refresh UpdatedAt
4. Entity marked as deleted but not physically removed

**Synchronous vs Asynchronous Behavior:**
- Entirely synchronous operations
- No I/O operations or external dependencies
- All method calls complete immediately

**Threading/Dispatcher Notes:**
- Thread-safe for read operations
- Write operations should be synchronized in multi-threaded scenarios
- Guid generation is thread-safe by design

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Instantiated through derived class constructors
2. **Usage:** Modified through business operations, tracked for audit purposes
3. **Soft Delete:** Optionally marked as deleted but retained
4. **Physical Deletion:** Eventually removed by repository layer if needed

## Public API / Surface Area

**Constructors:**
- `protected BaseEntity()`: Initializes entity with new GUID and timestamps

**Public Methods:**
- `void MarkAsDeleted()`: Marks entity as soft-deleted and updates timestamp
- `void Restore()`: Restores a soft-deleted entity and updates timestamp
- `void UpdateTimestamp()`: Updates the UpdatedAt field to current UTC time
- `bool Equals(object? obj)`: Equality comparison based on entity Id
- `int GetHashCode()`: Hash code generation based on entity Id
- `string ToString()`: String representation showing type and Id

**Public Properties:**
- `Guid Id`: Unique identifier for the entity (protected setter)
- `DateTime CreatedAt`: Entity creation timestamp (protected setter)
- `DateTime UpdatedAt`: Last modification timestamp (protected setter)
- `bool IsDeleted`: Soft delete flag (protected setter)

**Expected Input/Output:**
- **Input:** No parameters for constructor; optional object for Equals()
- **Output:** Properly initialized entity with audit fields set

**Side Effects:**
- Automatically generates GUID on creation
- Updates timestamps on modification
- Changes soft delete state when MarkAsDeleted() or Restore() called

**Error Behavior:**
- No exceptions thrown directly
- Derived classes may add validation logic
- Equals() handles null references gracefully

## Internal Logic Breakdown

**Constructor Logic (Lines 39-45):**
```csharp
protected BaseEntity()
{
    Id = Guid.NewGuid();           // Generate unique identifier
    CreatedAt = DateTime.UtcNow;   // Set creation timestamp
    UpdatedAt = DateTime.UtcNow;   // Initialize modification timestamp
    IsDeleted = false;             // Initialize as not deleted
}
```
- Uses Guid.NewGuid() for guaranteed uniqueness
- DateTime.UtcNow ensures consistent timezone handling
- All entities start in non-deleted state

**Soft Delete Logic (Lines 50-54):**
```csharp
public virtual void MarkAsDeleted()
{
    IsDeleted = true;              // Mark as deleted
    UpdateTimestamp();             // Update audit trail
}
```
- Virtual method allows override in derived classes
- Automatically updates modification timestamp
- Does not physically remove the entity

**Equality Logic (Lines 80-92):**
```csharp
public override bool Equals(object? obj)
{
    if (obj is not BaseEntity other)  // Type checking
        return false;
        
    if (ReferenceEquals(this, other)) // Reference equality
        return true;
        
    if (Id == Guid.Empty || other.Id == Guid.Empty) // Uninitialized check
        return false;
        
    return Id.Equals(other.Id);       // Identity-based equality
}
```
- Implements identity-based equality pattern
- Handles null and uninitialized entities
- Reference equality optimization for performance

**Hash Code Logic (Lines 98-101):**
```csharp
public override int GetHashCode()
{
    return Id.GetHashCode();
}
```
- Simple hash code based on entity identifier
- Consistent with Equals() implementation

**Timestamp Update Logic (Lines 69-72):**
```csharp
public virtual void UpdateTimestamp()
{
    UpdatedAt = DateTime.UtcNow;
}
```
- Updates modification timestamp to current UTC time
- Virtual method allows for custom timestamp logic

**Important Invariants:**
- Id is immutable after construction (protected setter)
- CreatedAt never changes after entity creation
- UpdatedAt always reflects last modification time
- Soft delete is reversible through Restore()

## Patterns & Principles Used

**Design Patterns:**
- **Template Method Pattern:** Base class provides structure, derived classes provide specifics
- **Active Record Pattern (Partial):** Entity contains both data and behavior
- **Identity Pattern:** Entity identity is separate from object reference

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** BaseEntity as a domain foundation
- **Clean Architecture:** Infrastructure-free domain layer
- **Repository Pattern Support:** Designed to work with repository abstraction

**Why These Patterns Were Chosen:**
- **Template Method:** Ensures consistent entity behavior across the domain
- **Active Record:** Reduces anemic domain model anti-pattern
- **Identity Pattern:** Essential for proper entity lifecycle management

**Trade-offs:**
- **Pros:** Consistency, reduced boilerplate, built-in audit capabilities
- **Cons:** Base class coupling, potential for bloated base class over time
- **Decision:** Benefits outweigh costs for this type of application

**Anti-patterns Avoided:**
- **Anemic Domain Model:** Entities contain behavior, not just data
- **Primitive Obsession:** Uses proper types (Guid, DateTime) instead of primitives
- **God Object:** Focused responsibility on entity infrastructure only

## Binding / Wiring / Configuration

**Dependency Injection:**
- Not directly registered in DI container
- Used as base class by registered entities
- No constructor injection (infrastructure-free design)

**Data Binding:**
- Properties designed for Entity Framework Core binding
- Navigation properties support lazy loading
- Audit fields automatically managed by EF Core interceptors

**Configuration Sources:**
- EF Core configuration in Repository layer
- Fluent API configurations for entity mappings
- Convention-based configuration for common properties

**Runtime Wiring:**
- Discovered through reflection by EF Core
- Automatically mapped to database schema
- Soft delete filters applied at query level

**Registration Points:**
- Indirect registration through derived entity types
- Model builder configuration in SnitcherDbContext
- Global query filters for soft delete functionality

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
public class MyEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

// Create entity
var entity = new MyEntity { Name = "Test" };
// Id, CreatedAt, UpdatedAt automatically set
Console.WriteLine(entity.Id); // Shows generated GUID
```

**Realistic Example:**
```csharp
public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    
    public void UpdateContent(string newContent)
    {
        Content = newContent;
        Version++;
        UpdateTimestamp(); // Manually update audit trail
    }
}

// Usage in business logic
var doc = new Document { Title = "Spec", Content = "Initial" };
// Later...
doc.UpdateContent("Updated content");
doc.MarkAsDeleted(); // Soft delete
```

**Incorrect Usage Example:**
```csharp
// WRONG: Trying to change entity identity
var entity = new MyEntity();
entity.Id = Guid.NewGuid(); // Compilation error - protected setter

// WRONG: Assuming reference equality works like identity
var entity1 = new MyEntity { Name = "Test" };
var entity2 = new MyEntity { Name = "Test" };
bool same = entity1 == entity2; // False - different instances
bool equal = entity1.Equals(entity2); // False - different IDs

// WRONG: Forgetting to update timestamp
public class BadEntity : BaseEntity
{
    public string Data { get; set; }
    
    public void ChangeData(string newData)
    {
        Data = newData;
        // Missing UpdateTimestamp() call!
    }
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void BaseEntity_ShouldGenerateUniqueIds()
{
    // Arrange & Act
    var entity1 = new TestEntity();
    var entity2 = new TestEntity();
    
    // Assert
    Assert.That(entity1.Id, Is.Not.EqualTo(entity2.Id));
}

[Test]
public void BaseEntity_ShouldHandleSoftDelete()
{
    // Arrange
    var entity = new TestEntity();
    var originalTime = entity.UpdatedAt;
    
    // Act
    entity.MarkAsDeleted();
    
    // Assert
    Assert.That(entity.IsDeleted, Is.True);
    Assert.That(entity.UpdatedAt, Is.GreaterThan(originalTime));
}

private class TestEntity : BaseEntity { }
```

**How to Mock or Replace:**
```csharp
// For testing derived classes, create test doubles
public class MockEntity : BaseEntity
{
    public string TestProperty { get; set; } = string.Empty;
    
    // Override for testing specific behavior
    public override void UpdateTimestamp()
    {
        // Custom timestamp logic for testing
        base.UpdateTimestamp();
    }
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **New Audit Fields:** Add properties with protected setters
2. **Custom Timestamp Logic:** Override UpdateTimestamp() in derived classes
3. **Extended Soft Delete:** Override MarkAsDeleted() for additional logic

**Where NOT to Add Logic:**
- **Business Rules:** Belong in specific entity classes or services
- **Database Operations:** Belong in repository layer
- **Validation Logic:** Belong in domain entities or validators
- **External Service Calls:** Violates clean architecture principles

**Safe Extension Points:**
- Virtual methods (MarkAsDeleted, Restore, UpdateTimestamp)
- Protected constructor for custom initialization
- ToString() override for custom string representation

**Common Mistakes:**
1. **Adding Business Logic:** BaseEntity should remain infrastructure-focused
2. **Making Properties Public:** Audit fields should remain protected
3. **Adding Dependencies:** Base class must remain dependency-free
4. **Complex Inheritance Hierarchies:** Keep inheritance shallow and focused

**Refactoring Warnings:**
- Changing Id type would require database migration
- Removing soft delete would break existing queries
- Adding new audit fields requires database schema changes
- Making methods non-virtual could break derived implementations

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Duplicate GUIDs:** Extremely rare but possible with GUID generation issues
- **Timestamp Inconsistencies:** If system clock changes during entity lifecycle
- **Equality Issues:** If GetHashCode() and Equals() become inconsistent

**Null/Reference Risks:**
- **Null Id:** Protected by constructor initialization
- **Null Timestamps:** Protected by constructor initialization
- **Null References:** Equals() method handles null input gracefully

**Performance Risks:**
- **GUID Generation:** Minimal overhead, not a performance concern
- **Hash Code Collisions:** Unlikely with GUID-based hashing
- **String Conversion:** ToString() called frequently in debugging

**Logging Points:**
- Entity creation (constructor entry)
- Soft delete operations (MarkAsDeleted, Restore)
- Timestamp updates (UpdateTimestamp)
- Equality comparisons (for debugging complex scenarios)

**How to Debug Step-by-Step:**
1. **Entity Creation:** Set breakpoint in BaseEntity constructor
2. **Soft Delete:** Step through MarkAsDeleted() to verify state changes
3. **Equality Issues:** Use Equals() method with logging to trace comparison logic
4. **Timestamp Problems:** Monitor UpdateTimestamp() calls and system clock

**Common Debugging Scenarios:**
```csharp
// Debug entity identity
var entity = new MyEntity();
Debug.WriteLine($"Entity created: {entity}"); // Uses ToString()

// Debug soft delete cycle
entity.MarkAsDeleted();
Debug.WriteLine($"After delete: IsDeleted={entity.IsDeleted}, UpdatedAt={entity.UpdatedAt}");

entity.Restore();
Debug.WriteLine($"After restore: IsDeleted={entity.IsDeleted}, UpdatedAt={entity.UpdatedAt}");
```

## Cross-References

**Related Classes:**
- `Project`: Concrete entity inheriting from BaseEntity
- `Workspace`: Concrete entity inheriting from BaseEntity
- `ProjectNamespace`: Concrete entity inheriting from BaseEntity
- `ProjectEntity`: Concrete entity inheriting from BaseEntity

**Upstream Callers:**
- `Snitcher.Repository.Repositories.EfRepository<T>`: Base repository for all entities
- `Snitcher.Service.Services.ProjectService`: Business logic using entities
- `Snitcher.Service.Services.WorkspaceService`: Business logic using entities

**Downstream Dependencies:**
- `Snitcher.Core.Interfaces.IEntity`: Identity contract interface
- `Snitcher.Core.Interfaces.IAuditableEntity`: Audit contract interface
- `Snitcher.Core.Interfaces.ISoftDeletable`: Soft delete contract interface

**Documents That Should Be Read Before/After:**
- **Before:** `IEntity.md`, `IAuditableEntity.md`, `ISoftDeletable.md`
- **After:** `Project.md`, `Workspace.md`, `ProjectEntity.md`
- **Related:** `SnitcherDbContext.md` (for persistence configuration)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Base Entity Pattern:** Foundation for any domain-driven design
- **Audit Trail Implementation:** Automatic timestamp tracking
- **Soft Delete Pattern:** Data recovery and audit capabilities
- **Identity-Based Equality:** Proper entity comparison semantics
- **Template Method Pattern:** Consistent behavior across entity hierarchy

**What Is Project-Specific:**
- **Guid as Primary Key:** Specific choice for this application's needs
- **UTC Timestamp Handling:** Specific to this application's requirements
- **Soft Delete Implementation:** Tailored to this application's data retention policies

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Core Interfaces:** Create IEntity, IAuditableEntity, ISoftDeletable
2. **Implement Base Class:** Provide concrete implementation with protected constructors
3. **Add Audit Fields:** Include Id, CreatedAt, UpdatedAt, IsDeleted
4. **Implement Equality:** Override Equals(), GetHashCode(), ToString()
5. **Add Soft Delete Methods:** MarkAsDeleted(), Restore(), UpdateTimestamp()
6. **Ensure Infrastructure Independence:** Keep base class free of external dependencies
7. **Make Methods Virtual:** Allow customization in derived classes
8. **Document Invariants:** Clearly define what must remain true

**Key Architectural Insights:**
- **Infrastructure Independence:** Base entities must not depend on persistence frameworks
- **Consistency Over Convenience:** Standardized patterns reduce cognitive load
- **Audit Trail Non-Negotiable:** Every entity change must be traceable
- **Soft Delete by Default:** Physical deletion should be exceptional, not normal
- **Identity Matters:** Entity identity is fundamental to domain modeling

**Implementation Checklist for New Projects:**
- [ ] Define identity interfaces before implementing base class
- [ ] Choose primary key type based on application requirements
- [ ] Implement proper equality semantics
- [ ] Add comprehensive audit trail capabilities
- [ ] Consider soft delete requirements early
- [ ] Ensure thread safety for multi-threaded scenarios
- [ ] Add comprehensive unit tests for base functionality
- [ ] Document all invariants and contracts clearly
