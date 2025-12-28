# BaseEntity

## Overview

BaseEntity is the foundational abstract class for all domain entities in the Snitcher application. It provides common infrastructure functionality including identity management, audit trails, soft delete capabilities, and standard entity behavior. This class ensures consistency across all entities and implements core domain patterns.

**Why it exists**: To eliminate code duplication and enforce consistent behavior across all domain entities, providing automatic audit trails and soft delete functionality without requiring manual implementation in each entity.

**Problem it solves**: Without BaseEntity, each entity would need to implement its own identity, audit fields, and soft delete logic, leading to code duplication and potential inconsistencies. It centralizes common entity behavior and ensures all entities follow the same patterns.

**What would break if removed**: All entity classes would lose their identity management, audit trail functionality, and soft delete capabilities. The entire domain model would need to be refactored to implement these features individually, breaking existing database mappings and business logic.

## Tech Stack Identification

**Languages used**: C# 12.0

**Frameworks**: .NET 8.0

**Libraries**: None (pure domain model)

**UI frameworks**: N/A (domain layer)

**Persistence / communication technologies**: Entity Framework Core (implicit through attributes and conventions)

**Build tools**: MSBuild

**Runtime assumptions**: .NET 8.0 runtime, Entity Framework Core for persistence

**Version hints**: Uses modern C# features like expression-bodied members and nullable reference types

## Architectural Role

**Layer**: Domain Layer (Core)

**Responsibility boundaries**:
- MUST provide identity management for all entities
- MUST maintain audit trail information (CreatedAt, UpdatedAt)
- MUST support soft delete functionality
- MUST NOT contain business logic specific to any entity type
- MUST NOT depend on external services or infrastructure

**What it MUST do**:
- Generate unique identifiers for new entities
- Track creation and modification timestamps
- Support soft delete operations
- Provide standard equality comparison based on ID

**What it MUST NOT do**:
- Contain business rules or validation logic
- Access external resources like databases or services
- Implement entity-specific behavior
- Handle persistence concerns directly

**Dependencies (incoming)**: All concrete entity classes inherit from this class

**Dependencies (outgoing)**: IEntity, IAuditableEntity, ISoftDeletable interfaces

## Execution Flow

**Where execution starts**: BaseEntity is instantiated when concrete entities are created through constructors or Entity Framework materialization.

**How control reaches this component**: 
1. Application layer creates new entity instances
2. Entity Framework loads entities from database
3. Repository layer performs CRUD operations

**Call sequence (step-by-step)**:
1. BaseEntity constructor is called (automatically)
2. Id is assigned using Guid.NewGuid()
3. CreatedAt and UpdatedAt are set to DateTime.UtcNow
4. IsDeleted is set to false
5. Entity instance is returned to caller

**Synchronous vs asynchronous behavior**: Synchronous - all operations are in-memory and immediate

**Threading / dispatcher / event loop notes**: Thread-safe for read operations, but entity instances should not be shared across threads without synchronization

**Lifecycle (creation → usage → disposal)**:
1. Creation: Constructor sets initial state
2. Usage: Properties are accessed and modified
3. Update: UpdateTimestamp() is called to track changes
4. Soft delete: MarkAsDeleted() or Restore() methods called
5. Disposal: Garbage collected when no longer referenced

## Public API / Surface Area

**Constructors**:
- `protected BaseEntity()`: Creates new entity with generated ID and timestamps

**Public methods**:
- `virtual void MarkAsDeleted()`: Marks entity as soft-deleted and updates timestamp
- `virtual void Restore()`: Restores a soft-deleted entity and updates timestamp  
- `virtual void UpdateTimestamp()`: Updates the UpdatedAt field to current time
- `override bool Equals(object? obj)`: Equality comparison based on entity ID
- `override int GetHashCode()`: Hash code based on entity ID
- `override string ToString()`: String representation showing type and ID

**Properties**:
- `Guid Id`: Unique identifier (protected setter)
- `DateTime CreatedAt`: Creation timestamp (protected setter)
- `DateTime UpdatedAt`: Last modification timestamp (protected setter)
- `bool IsDeleted`: Soft delete flag (protected setter)

**Events**: None

**Expected input/output**:
- Input: None (constructor) or method calls for state changes
- Output: Entity instance with proper state management

**Side effects**:
- Updates timestamps when state changes
- Modifies soft delete status
- No external side effects

**Error behavior**: 
- ArgumentNullException not thrown (no null parameters)
- InvalidOperationException not thrown under normal operation
- All operations are designed to be safe and idempotent

## Internal Logic Breakdown

**Line-by-line or block-by-block explanation**:

**Constructor (lines 39-45)**:
```csharp
protected BaseEntity()
{
    Id = Guid.NewGuid();
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    IsDeleted = false;
}
```
- Generates new unique identifier using GUID
- Sets both creation and update timestamps to current UTC time
- Initializes entity as not deleted
- Protected to ensure only derived classes can instantiate

**MarkAsDeleted method (lines 50-54)**:
```csharp
public virtual void MarkAsDeleted()
{
    IsDeleted = true;
    UpdateTimestamp();
}
```
- Sets IsDeleted flag to true
- Calls UpdateTimestamp to record when deletion occurred
- Virtual to allow derived classes to override behavior

**Restore method (lines 59-63)**:
```csharp
public virtual void Restore()
{
    IsDeleted = false;
    UpdateTimestamp();
}
```
- Sets IsDeleted flag to false
- Updates timestamp to track restoration
- Virtual for extensibility

**UpdateTimestamp method (lines 69-72)**:
```csharp
public virtual void UpdateTimestamp()
{
    UpdatedAt = DateTime.UtcNow;
}
```
- Simple timestamp update to current UTC time
- Virtual to allow custom timestamp logic

**Equals method (lines 80-92)**:
```csharp
public override bool Equals(object? obj)
{
    if (obj is not BaseEntity other)
        return false;
        
    if (ReferenceEquals(this, other))
        return true;
        
    if (Id == Guid.Empty || other.Id == Guid.Empty)
        return false;
        
    return Id.Equals(other.Id);
}
```
- Type checks and null handling
- Reference equality check for performance
- Rejects comparison with default/empty GUIDs
- Final equality based on GUID comparison

**GetHashCode method (lines 98-101)**:
```csharp
public override int GetHashCode()
{
    return Id.GetHashCode();
}
```
- Hash code based solely on ID for consistency with Equals

**ToString method (lines 107-110)**:
```csharp
public override string ToString()
{
    return $"{GetType().Name}[Id={Id}]";
}
```
- Provides readable string representation
- Useful for debugging and logging

**Algorithms used**: 
- GUID generation for unique identification
- UTC timestamp tracking for audit trails
- Standard object equality patterns

**Conditional logic explanation**:
- Type checking in Equals method ensures type safety
- Reference equality optimization prevents unnecessary GUID comparison
- Empty GUID checks prevent false positives with unsaved entities

**State transitions**:
- Created → Modified (via UpdateTimestamp)
- Created → Soft Deleted (via MarkAsDeleted)
- Soft Deleted → Restored (via Restore)
- Any state → Modified (continuous updates)

**Important invariants**:
- Id never changes after creation
- CreatedAt never changes after creation  
- UpdatedAt always reflects last modification
- IsDeleted transitions are tracked via timestamps

## Patterns & Principles Used

**Design patterns (explicit or implicit)**:
- **Template Method Pattern**: BaseEntity provides template for entity behavior, derived classes can override specific methods
- **Identity Field Pattern**: Uses GUID as identity field for entities
- **Active Record Pattern (partial)**: Entity contains behavior for state changes

**Architectural patterns**:
- **Domain-Driven Design (DDD)**: BaseEntity as domain entity foundation
- **Clean Architecture**: Pure domain model with no infrastructure dependencies

**Why these patterns were chosen (inferred)**:
- Template Method allows consistent behavior with customization points
- GUID identity ensures uniqueness across distributed systems
- DDD principles enforce rich domain model with behavior

**Trade-offs**:
- GUID vs int identity: More storage but ensures uniqueness and no sequence dependencies
- Timestamps in UTC: Avoids timezone issues but requires conversion for local display
- Soft delete: Increases storage and query complexity but provides data recovery

**Anti-patterns avoided or possibly introduced**:
- Avoided: Anemic domain model (entities contain behavior)
- Avoided: God object (focused on common concerns only)
- Possible risk: Base class bloat if too much functionality added

## Binding / Wiring / Configuration

**Dependency injection**: None - BaseEntity is not registered in DI container

**Data binding (if UI)**: N/A - domain layer

**Configuration sources**: None - behavior is hardcoded

**Runtime wiring**: Entity Framework automatically maps properties to database columns

**Registration points**: None - base classes are not registered separately

## Example Usage (CRITICAL)

**Minimal example**:
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Create new entity
var product = new Product { Name = "Widget", Price = 9.99m };
// product.Id is automatically set
// product.CreatedAt and product.UpdatedAt are set to now
```

**Realistic example**:
```csharp
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsDeleted { get; private set; } // Hide base property
}

// Create and modify
var order = new Order 
{ 
    OrderNumber = "ORD-001", 
    OrderDate = DateTime.UtcNow,
    TotalAmount = 199.99m
};

// Later modification
order.TotalAmount = 249.99m;
order.UpdateTimestamp(); // Manually track the change

// Soft delete
order.MarkAsDeleted();
```

**Incorrect usage example (and why it is wrong)**:
```csharp
// WRONG: Trying to set ID manually
var product = new Product();
product.Id = Guid.NewGuid(); // Won't compile - setter is protected

// WRONG: Assuming CreatedAt changes
var product = new Product();
// ... later
product.CreatedAt = DateTime.UtcNow; // Won't compile - setter is protected

// WRONG: Not using virtual methods for custom behavior
public class BadEntity : BaseEntity
{
    public new void MarkAsDeleted() // Hides base method - bad practice
    {
        // Custom logic without calling base
    }
}
```

**How to test this in isolation**:
```csharp
[Test]
public void BaseEntity_ShouldInitializeWithCorrectValues()
{
    // Arrange & Act
    var entity = new TestEntity();
    
    // Assert
    Assert.That(entity.Id, Is.Not.EqualTo(Guid.Empty));
    Assert.That(entity.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    Assert.That(entity.UpdatedAt, Is.EqualTo(entity.CreatedAt));
    Assert.That(entity.IsDeleted, Is.False);
}

[Test]
public void BaseEntity_ShouldHandleSoftDelete()
{
    // Arrange
    var entity = new TestEntity();
    var originalUpdatedAt = entity.UpdatedAt;
    
    // Act
    entity.MarkAsDeleted();
    
    // Assert
    Assert.That(entity.IsDeleted, Is.True);
    Assert.That(entity.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
}

private class TestEntity : BaseEntity { }
```

**How to mock or replace it**:
```csharp
// For testing derived classes, you can inherit from BaseEntity
public class MockEntity : BaseEntity
{
    public string TestProperty { get; set; } = string.Empty;
}

// Or use a test double that implements the same interfaces
public class TestEntity : IEntity, IAuditableEntity, ISoftDeletable
{
    // Manual implementation for specific test scenarios
}
```

## Extension & Modification Guide

**How to add a new feature here**:
1. Add new virtual methods to BaseEntity for common entity behavior
2. Add new properties with protected setters for shared data
3. Implement default behavior in BaseEntity
4. Allow derived classes to override as needed

**Where NOT to add logic**:
- Don't add business rules specific to certain entity types
- Don't add infrastructure concerns like database access
- Don't add validation logic that varies by entity type
- Don't add UI-specific properties or methods

**Safe extension points**:
- Virtual methods can be overridden by derived classes
- Protected properties can be accessed by derived classes
- Constructor logic can be extended via derived class constructors

**Common mistakes**:
- Adding too much functionality to BaseEntity (violates Single Responsibility)
- Making properties public when they should be protected
- Forgetting to call base methods in overrides
- Adding dependencies on external services

**Refactoring warnings**:
- Changing property visibility will break derived classes
- Removing virtual methods will break overrides
- Changing constructor signature will break inheritance chains
- Modifying Equals/GetHashCode logic can break collections and dictionaries

## Failure Modes & Debugging

**Common runtime errors**:
- **InvalidOperationException**: Rare, usually from derived class misuse
- **NullReferenceException**: Possible if derived classes don't initialize properly
- **ArgumentException**: Can occur if GUID operations fail (very rare)

**Null/reference risks**:
- Equals method safely handles null input
- ToString handles null GetType() (should never happen)
- All properties are value types or strings, minimal null risk

**Performance risks**:
- GUID generation overhead (minimal)
- GetHashCode called frequently in collections
- ToString called during logging/debugging

**Logging points**:
- No built-in logging (domain layer separation)
- ToString() useful for debug output
- Entity state changes should be logged at application layer

**How to debug step-by-step**:
1. Set breakpoint in constructor to verify initialization
2. Use ToString() in debug windows to see entity state
3. Watch Id property to verify uniqueness
4. Monitor timestamp changes during updates
5. Check IsDeleted flag during soft delete operations

## Cross-References

**Related classes**:
- All concrete entity classes inherit from BaseEntity
- IEntity, IAuditableEntity, ISoftDeletable interfaces
- Entity Framework DbContext classes map BaseEntity properties

**Upstream callers**:
- Repository layer creates and manages entities
- Service layer performs business operations on entities
- Application layer orchestrates entity workflows

**Downstream dependencies**:
- Database schema includes BaseEntity columns
- Entity Framework mappings rely on BaseEntity structure
- UI models may map BaseEntity properties

**Documents that should be read before/after**:
- Read: IEntity, IAuditableEntity, ISoftDeletable interface documentation
- Read: Entity Framework configuration documentation
- Read: Specific entity class documentation (Project, Workspace, etc.)
- Read: Repository pattern documentation

## Knowledge Transfer Notes

**What concepts here are reusable in other projects**:
- Base entity pattern with audit trails and soft delete
- GUID-based identity management
- Timestamp tracking for audit purposes
- Equality and hash code patterns for entities
- Template method pattern for base classes

**What is project-specific**:
- Specific property names (CreatedAt, UpdatedAt, IsDeleted)
- UTC timestamp requirement (may vary by project)
- GUID vs alternative identity choices
- Soft delete implementation (some projects use hard delete)

**How to recreate this pattern from scratch elsewhere**:
1. Define base entity class with common properties
2. Implement identity management (GUID, int, etc.)
3. Add audit trail fields and logic
4. Implement soft delete functionality
5. Override Equals, GetHashCode, and ToString
6. Make methods virtual for extensibility
7. Use protected setters for encapsulation
8. Follow domain-driven design principles

**Key insights for implementation**:
- Always use UTC timestamps to avoid timezone issues
- Make base class constructor protected to control instantiation
- Implement proper equality semantics for entity collections
- Consider performance implications of GetHashCode implementation
- Balance between base class functionality and YAGNI principle
