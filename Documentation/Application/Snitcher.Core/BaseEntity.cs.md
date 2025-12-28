# BaseEntity.cs

## Overview

`BaseEntity.cs` is the foundational abstract base class for all domain entities in the Snitcher application's clean architecture. It implements essential entity infrastructure including identity management, audit trails, soft delete functionality, and equality semantics. This class ensures consistency across all entities and provides core behaviors required by the domain model.

**Why it exists**: To eliminate code duplication across entities, provide consistent identity and audit behavior, enable soft delete patterns for data recovery, and establish proper equality semantics for domain objects.

**What problem it solves**: Prevents inconsistent entity implementations, provides automatic audit trail management, enables data recovery through soft deletes, ensures proper entity comparison, and standardizes timestamp handling across the domain model.

**What would break if removed**: All entities would lose identity management, audit trails, soft delete capability, and proper equality semantics, requiring duplicated code and potentially inconsistent behavior across the domain.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 Base Class Library

**Libraries**: None (pure domain logic)

**Persistence**: Framework-agnostic (used by EF Core but not dependent)

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: Standard .NET runtime with GUID support

## Architectural Role

**Layer**: Domain Layer (Core Entity)

**Responsibility Boundaries**:
- MUST provide identity management for all entities
- MUST implement audit trail functionality
- MUST support soft delete operations
- MUST NOT contain business logic specific to entities
- MUST NOT access external services or databases

**What it MUST do**:
- Generate unique identifiers for entities
- Track creation and modification timestamps
- Provide soft delete and restore operations
- Implement proper equality based on identity
- Update timestamps on modifications

**What it MUST NOT do**:
- Implement business validation rules
- Access persistence mechanisms
- Handle domain-specific logic
- Depend on infrastructure services

**Dependencies (Incoming)**: All concrete entity classes

**Dependencies (Outgoing**: Core interfaces (IEntity, IAuditableEntity, ISoftDeletable)

## Execution Flow

**Where execution starts**: Constructor called when concrete entity instances are created

**How control reaches this component**:
1. Concrete entity constructor calls base constructor
2. BaseEntity constructor initializes identity and timestamps
3. Entity properties set by calling code
4. Soft delete operations called by business logic
5. Equality methods used by collections and comparisons

**Constructor Flow** (lines 39-45):
1. Generate new GUID for Id
2. Set CreatedAt to UTC now
3. Set UpdatedAt to UTC now
4. Set IsDeleted to false

**Soft Delete Flow** (lines 50-54):
1. Set IsDeleted to true
2. Call UpdateTimestamp() to refresh UpdatedAt

**Equality Flow** (lines 80-91):
1. Check object type compatibility
2. Check reference equality
3. Validate non-empty GUIDs
4. Compare GUID values

**Synchronous vs asynchronous behavior**: All operations are synchronous - this is pure domain logic

**Threading/Dispatcher notes**: No threading concerns - pure in-memory operations

**Lifecycle**: Created by concrete entities → Lives as part of entity lifecycle → Garbage collected with entity

## Public API / Surface Area

**Inheritance**: `abstract class BaseEntity : IEntity, IAuditableEntity, ISoftDeletable`

**Properties**:
- `Guid Id` - Unique entity identifier (protected setter)
- `DateTime CreatedAt` - Creation timestamp (protected setter)
- `DateTime UpdatedAt` - Last modification timestamp (protected setter)
- `bool IsDeleted` - Soft delete flag (protected setter)

**Constructors**:
- `protected BaseEntity()` - Initializes new entity with identity and timestamps

**Public Methods**:
- `void MarkAsDeleted()` - Soft deletes the entity
- `void Restore()` - Restores soft-deleted entity
- `void UpdateTimestamp()` - Updates the modification timestamp
- `bool Equals(object? obj)` - Equality comparison
- `int GetHashCode()` - Hash code generation
- `string ToString()` - String representation

**Expected Input/Output**: Methods manipulate entity state, properties provide identity and audit information.

**Side Effects**:
- MarkAsDeleted() sets IsDeleted and updates timestamp
- Restore() clears IsDeleted and updates timestamp
- UpdateTimestamp() refreshes UpdatedAt
- Entity identity established in constructor

**Error Behavior**: No explicit error handling - relies on .NET framework exceptions for invalid operations.

## Internal Logic Breakdown

**Constructor Implementation** (lines 39-45):
```csharp
protected BaseEntity()
{
    Id = Guid.NewGuid();
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    IsDeleted = false;
}
```
- Uses GUID for globally unique identity
- UTC timestamps ensure consistency across timezones
- Protected setters prevent external modification
- Initial state is active (not deleted)

**Soft Delete Pattern** (lines 50-63):
```csharp
public virtual void MarkAsDeleted()
{
    IsDeleted = true;
    UpdateTimestamp();
}

public virtual void Restore()
{
    IsDeleted = false;
    UpdateTimestamp();
}
```
- Virtual methods allow override in derived classes
- Always updates timestamp to track deletion/restoration
- Simple boolean flag for soft delete state

**Equality Implementation** (lines 80-91):
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
- Type-safe comparison with pattern matching
- Reference equality optimization
- Rejects comparison with transient entities (empty GUIDs)
- Identity-based equality for domain entities

**Hash Code Implementation** (lines 98-101):
```csharp
public override int GetHashCode()
{
    return Id.GetHashCode();
}
```
- Consistent with Equals() implementation
- Based solely on identity GUID
- Ensures proper behavior in hash-based collections

**Timestamp Management** (lines 69-72):
```csharp
public virtual void UpdateTimestamp()
{
    UpdatedAt = DateTime.UtcNow;
}
```
- Centralized timestamp update logic
- Virtual for potential override in derived classes
- Always uses UTC for consistency

## Patterns & Principles Used

**Template Method Pattern**: Base class provides common structure, derived classes provide specific behavior

**Identity Pattern**: Entities have unique identifiers that define equality

**Audit Trail Pattern**: Automatic tracking of creation and modification timestamps

**Soft Delete Pattern**: Logical deletion with recovery capability

**Value Object Pattern**: Immutable identity through protected setters

**Why these patterns were chosen**:
- Template method for consistent entity structure
- Identity pattern for proper domain semantics
- Audit trail for compliance and debugging
- Soft delete for data recovery and audit
- Value object for entity immutability

**Trade-offs**:
- Base class coupling reduces flexibility
- GUID overhead compared to integer identities
- UTC timestamps may need conversion for display
- Soft delete adds query complexity

**Anti-patterns avoided**:
- No anemic domain model (has behavior)
- No primitive obsession (uses proper types)
- No violation of encapsulation (protected setters)
- No inconsistent equality semantics

## Binding / Wiring / Configuration

**Data Binding**: Not applicable (domain entity)

**Configuration Sources**: No external configuration needed

**Runtime Wiring**: Used by Entity Framework Core for entity mapping

**Registration Points**: No registration - base class for inheritance

**Framework Integration**:
- Mapped by EF Core Fluent API
- Used by repository pattern implementations
- Recognized by domain services

## Example Usage

**Minimal Example**:
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; } = "";
}

// Create entity
var product = new Product();
Console.WriteLine(product.Id); // New GUID
```

**Realistic Example**:
```csharp
public class Order : BaseEntity
{
    public string CustomerName { get; set; } = "";
    public bool IsDeleted => base.IsDeleted;
    
    public override void MarkAsDeleted()
    {
        // Custom delete logic
        base.MarkAsDeleted();
    }
}

// Usage
var order = new Order { CustomerName = "John" };
order.MarkAsDeleted();
order.Restore();
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't modify protected properties
var entity = new BaseEntity();
entity.Id = Guid.NewGuid(); // Won't compile

// BAD - Don't compare transient entities
var entity1 = new Product();
var entity2 = new Product();
bool equal = entity1.Equals(entity2); // False (different GUIDs)
```

**How to test in isolation**:
```csharp
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = "";
}

[Test]
public void BaseEntity_ShouldHaveUniqueIdentity()
{
    var entity1 = new TestEntity();
    var entity2 = new TestEntity();
    
    Assert.AreNotEqual(entity1.Id, entity2.Id);
    Assert.AreNotEqual(entity1, entity2);
}

[Test]
public void BaseEntity_ShouldSupportSoftDelete()
{
    var entity = new TestEntity();
    
    Assert.IsFalse(entity.IsDeleted);
    
    entity.MarkAsDeleted();
    Assert.IsTrue(entity.IsDeleted);
    
    entity.Restore();
    Assert.IsFalse(entity.IsDeleted);
}
```

**How to mock or replace**:
- Create test entities inheriting from BaseEntity
- Use factory methods for test data creation
- Mock only if testing interfaces that depend on BaseEntity

## Extension & Modification Guide

**How to add new entity behavior**:
1. Inherit from BaseEntity for new entity types
2. Override virtual methods if custom behavior needed
3. Add entity-specific properties and methods
4. Implement domain validation in derived classes

**Where NOT to add logic**:
- Don't add business rules to BaseEntity
- Don't add persistence-specific code
- Don't add UI-specific properties

**Safe extension points**:
- Override virtual methods for custom behavior
- Add domain-specific methods in derived classes
- Extend with additional interfaces
- Add domain events in derived classes

**Common mistakes**:
- Forgetting to call base methods in overrides
- Making properties public that should be protected
- Adding business logic that varies between entities
- Not considering impact on equality and hashing

**Refactoring warnings**:
- Changing equality logic affects all entities
- Modifying timestamp behavior affects audit trails
- Adding new properties affects database mappings
- Consider performance impact of GUID operations

## Failure Modes & Debugging

**Common runtime errors**:
- InvalidOperationException if GUID operations fail
- OverflowException with timestamp manipulations
- NullReferenceException in Equals if obj is null

**Null/reference risks**:
- Equals method handles null obj parameter
- GetHashCode never returns null
- Properties always initialized in constructor

**Performance risks**:
- GUID generation overhead for many entities
- Hash code calculation in large collections
- Memory usage from GUID fields

**Logging points**: None in base class - logging handled by calling code or infrastructure

**How to debug step-by-step**:
1. Set breakpoint in constructor to verify identity generation
2. Monitor timestamp updates during entity lifecycle
3. Test equality behavior with different entity states
4. Verify soft delete operations work correctly
5. Check hash code consistency in collections

## Cross-References

**Related classes**:
- All concrete entity classes (inherit from this)
- `IEntity` interface (implemented)
- `IAuditableEntity` interface (implemented)
- `ISoftDeletable` interface (implemented)

**Upstream callers**:
- Entity Framework Core (materialization)
- Repository implementations (CRUD operations)
- Domain services (business logic)

**Downstream dependencies**:
- Core interfaces (contracts)
- Derived entity classes (inheritance)

**Documents to read before/after**:
- Before: Interface definitions (IEntity, etc.)
- After: Concrete entity implementations
- After: Repository pattern documentation

## Knowledge Transfer Notes

**Reusable concepts**:
- Base entity pattern for domain models
- Identity management with GUIDs
- Audit trail implementation
- Soft delete pattern
- Equality semantics for entities

**Project-specific elements**:
- Snitcher domain entity hierarchy
- UTC timestamp standardization
- Soft delete for data recovery
- Entity identity for repository patterns

**How to recreate pattern elsewhere**:
1. Create abstract base class for entities
2. Implement identity generation in constructor
3. Add audit fields with protected setters
4. Implement soft delete methods
5. Override Equals and GetHashCode for identity
6. Provide ToString override for debugging

**Key insights**:
- Use GUIDs for globally unique identity
- Always use UTC for timestamps
- Implement soft delete for data recovery
- Base equality on identity, not properties
- Protect setters to maintain encapsulation
- Provide virtual methods for extensibility
- Consider performance implications of identity operations
