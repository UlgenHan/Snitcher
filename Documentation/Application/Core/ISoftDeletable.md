# ISoftDeletable

## Overview

ISoftDeletable is the contract interface for entities that support soft deletion functionality in the Snitcher application. It establishes the pattern that soft-deletable entities must maintain a deletion flag, enabling data recovery, audit trail preservation, and safe data removal practices. This interface is fundamental to implementing data retention policies, maintaining referential integrity, and providing undo capabilities for user operations.

This interface exists to enforce the architectural principle that important domain entities should support soft deletion instead of immediate physical deletion. Without ISoftDeletable, the system would lose the ability to recover accidentally deleted data, maintain complete audit trails, or implement data retention policies. Soft deletion provides a safety net for user operations and supports compliance requirements for data preservation.

If ISoftDeletable were removed, the system would lose:
- Soft deletion capabilities across all entities
- Data recovery and undo functionality
- Complete audit trail preservation
- Referential integrity during deletion operations
- Data retention policy implementation
- Safe deletion practices with recovery options

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- .NET 8.0 Base Class Library
- System namespace for boolean handling

**Libraries:**
- System namespace for core functionality
- No external dependencies required

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- Framework-agnostic design
- Compatible with any persistence mechanism
- Used by Entity Framework Core for query filtering

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- No special build requirements

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires boolean type support
- No special runtime requirements

**Version Hints:**
- Uses modern C# property design
- Compatible with .NET 8.0 and later versions
- Follows standard interface design patterns

## Architectural Role

**Layer:** Domain Layer (Clean Architecture - Core)

**Responsibility Boundaries:**
- MUST define soft deletion contract for entities
- MUST provide deletion status access
- MUST NOT specify implementation details for deletion logic
- MUST NOT depend on external infrastructure
- MUST NOT enforce specific deletion workflows

**What it MUST do:**
- Define contract for soft deletion status tracking
- Enable data recovery and undo operations
- Support audit trail preservation
- Provide foundation for data retention policies

**What it MUST NOT do:**
- Specify how soft deletion is implemented
- Provide deletion or restoration methods
- Enforce specific deletion workflows
- Depend on persistence frameworks or recovery systems

**Dependencies (Incoming):**
- BaseEntity (primary implementation)
- All soft-deletable entity classes
- Repository layer for query filtering and restoration
- Service layer for soft deletion operations

**Dependencies (Outgoing):**
- No dependencies (pure contract interface)

## Execution Flow

**Where execution starts:**
- Entity class declarations implementing the interface
- Repository layer query filtering for soft-deleted entities
- Service layer soft deletion and restoration operations

**How control reaches this component:**
1. Entity classes implement ISoftDeletable for soft deletion contract
2. Repository classes use interface for automatic query filtering
3. Service classes use interface for soft deletion operations
4. Framework reflection discovers interface implementations

**Call Sequence (Soft Deletion):**
1. User initiates delete operation through UI
2. Service layer calls entity soft deletion method
3. Entity sets IsDeleted flag to true
4. Repository automatically excludes entity from default queries
5. Entity remains in database for recovery purposes

**Call Sequence (Query Filtering):**
1. Repository executes query for entities
2. Query filter checks ISoftDeletable.IsDeleted property
3. Soft-deleted entities excluded from results
4. Only active entities returned to caller

**Synchronous vs Asynchronous Behavior:**
- Pure interface contract - no execution behavior
- Implementation behavior depends on concrete classes
- Soft deletion operations typically synchronous

**Threading/Dispatcher Notes:**
- Interface itself is thread-safe (no state)
- Thread safety depends on implementation
- Soft deletion operations should be atomic

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Interface exists for entire application lifetime
2. **Usage:** Referenced by entity declarations and query operations
3. **Disposal:** Interface lifetime managed by .NET runtime

## Public API / Surface Area

**Interface Members:**
- `bool IsDeleted { get; }`: Read-only property indicating deletion status

**Expected Input/Output:**
- **Input:** No input parameters
- **Output:** Boolean value indicating if entity is soft-deleted

**Side Effects:**
- No side effects - pure contract interface
- Enforces compile-time requirements on implementing classes

**Error Behavior:**
- No exceptions thrown by interface itself
- Implementation classes may throw exceptions
- Boolean value should be properly initialized

## Internal Logic Breakdown

**Interface Design Philosophy:**
```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; }
}
```
- Minimal contract focusing only on deletion status
- Read-only property prevents external status manipulation
- Boolean type provides clear deletion state indication
- No methods specified to maintain interface simplicity

**Deletion Status Property Design:**
- **IsDeleted**: Boolean flag indicating soft deletion state
- True value indicates entity has been soft-deleted
- False value indicates entity is active
- Read-only access preserves encapsulation

**Contract Enforcement Logic:**
- Compiler enforces property implementation
- Read-only access prevents external status changes
- Interface ensures consistent soft deletion capability
- No implementation details specified in contract

**Important Invariants:**
- IsDeleted should default to false for new entities
- Once set to true, should remain true until explicitly restored
- Interface must remain implementation-agnostic
- Property should reflect actual deletion state

## Patterns & Principles Used

**Design Patterns:**
- **Interface Segregation Principle:** Minimal, focused deletion contract
- **Dependency Inversion Principle:** High-level modules depend on abstraction
- **Marker Interface Pattern:** Marks entities as soft-deletable

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** Soft deletion as domain concern
- **Clean Architecture:** Infrastructure-free domain contract
- **Soft Delete Pattern:** Safe data removal with recovery capability

**Why These Patterns Were Chosen:**
- **Interface Segregation:** Keeps deletion contract focused and minimal
- **Dependency Inversion:** Enables flexible soft deletion implementations
- **Marker Interface:** Clearly identifies soft-deletable entities

**Trade-offs:**
- **Pros:** Data recovery capability, audit trail preservation, safety net
- **Cons:** Storage overhead, query complexity, potential data bloat
- **Decision:** Benefits of data safety and recovery outweigh storage costs

**Anti-patterns Avoided:**
- **Fat Interfaces:** Interface contains only essential deletion contract
- **Implementation Leakage:** Interface doesn't specify deletion logic
- **Concrete Dependencies:** Interface has no external dependencies

## Binding / Wiring / Configuration

**Dependency Injection:**
- Interface itself not registered in DI container
- Used by repositories for automatic query filtering
- Enables soft deletion service operations

**Data Binding:**
- No data binding requirements
- Used by Entity Framework Core for global query filters
- Supports convention-based soft deletion discovery

**Configuration Sources:**
- No configuration required
- Discovered through reflection by frameworks
- Used by data management tools for entity filtering

**Runtime Wiring:**
- Interface resolution through .NET type system
- Automatic query filtering by infrastructure
- Runtime type checking for soft deletion operations

**Registration Points:**
- Entity class declarations implement interface
- Repository configurations for automatic query filtering
- Soft deletion service registrations

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Entity implementing interface
public class SoftDeletableEntity : ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public string Name { get; set; } = string.Empty;
    
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
    
    public void Restore()
    {
        IsDeleted = false;
    }
}
```

**Realistic Example:**
```csharp
// Repository with automatic soft deletion filtering
public class SoftDeletableRepository<T> : IRepository<T> where T : ISoftDeletable
{
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // Automatically excludes soft-deleted entities
        return await _context.Set<T>()
            .Where(e => !e.IsDeleted)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<T>> GetAllIncludingDeletedAsync()
    {
        // Includes soft-deleted entities for admin purposes
        return await _context.Set<T>().ToListAsync();
    }
    
    public async Task SoftDeleteAsync(T entity)
    {
        if (entity is IDeletableEntity deletable)
        {
            deletable.MarkAsDeleted();
        }
        await _context.SaveChangesAsync();
    }
}

// Service for soft deletion operations
public class SoftDeletionService
{
    public async Task DeleteEntityAsync<T>(T entity) where T : ISoftDeletable
    {
        if (entity is IDeletableEntity deletable)
        {
            deletable.MarkAsDeleted();
            await _repository.UpdateAsync(entity);
        }
    }
    
    public async Task RestoreEntityAsync<T>(T entity) where T : ISoftDeletable
    {
        if (entity is IDeletableEntity deletable)
        {
            deletable.Restore();
            await _repository.UpdateAsync(entity);
        }
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Trying to modify deletion status through interface
public class BadEntity : ISoftDeletable
{
    public bool IsDeleted { get; set; } // Should be private set
}

// WRONG: Assuming interface manages deletion
public class BadService
{
    public void DeleteEntity<T>(T entity) where T : ISoftDeletable
    {
        entity.IsDeleted = true; // WRONG - Can't modify through interface
    }
}

// WRONG: Not checking deletion status
public class BadRepository<T> where T : ISoftDeletable
{
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync(); // WRONG - Includes deleted entities
    }
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void Entity_ShouldImplementISoftDeletable()
{
    // Arrange
    var entity = new TestSoftDeletableEntity();
    
    // Act & Assert
    Assert.That(entity, Is.InstanceOf<ISoftDeletable>());
    Assert.That(entity.IsDeleted, Is.False); // Default state
}

[Test]
public void SoftDeletion_ShouldBeReversible()
{
    // Arrange
    var entity = new TestSoftDeletableEntity();
    
    // Act
    entity.MarkAsDeleted();
    var isDeletedAfterMark = entity.IsDeleted;
    
    entity.Restore();
    var isDeletedAfterRestore = entity.IsDeleted;
    
    // Assert
    Assert.That(isDeletedAfterMark, Is.True);
    Assert.That(isDeletedAfterRestore, Is.False);
}

[Test]
public void Repository_ShouldExcludeDeletedEntities()
{
    // Arrange
    var repository = new TestSoftDeletableRepository<TestSoftDeletableEntity>();
    var activeEntity = new TestSoftDeletableEntity { Name = "Active" };
    var deletedEntity = new TestSoftDeletableEntity { Name = "Deleted" };
    deletedEntity.MarkAsDeleted();
    
    // Act
    var allEntities = repository.GetAllAsync().Result;
    
    // Assert
    Assert.That(allEntities.Count(), Is.EqualTo(1));
    Assert.That(allEntities.First().Name, Is.EqualTo("Active"));
}

private class TestSoftDeletableEntity : ISoftDeletable, IDeletableEntity
{
    public bool IsDeleted { get; private set; }
    public string Name { get; set; } = string.Empty;
    
    public void MarkAsDeleted() => IsDeleted = true;
    public void Restore() => IsDeleted = false;
}
```

**How to Mock or Replace:**
```csharp
// Mock entity for testing
public class MockSoftDeletableEntity : ISoftDeletable
{
    public bool IsDeleted { get; set; }
}

// Mock repository with soft deletion filtering
public class MockSoftDeletableRepository<T> : IRepository<T> where T : ISoftDeletable
{
    private readonly List<T> _items = new();
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return _items.Where(item => !item.IsDeleted);
    }
    
    public async Task<IEnumerable<T>> GetAllIncludingDeletedAsync()
    {
        return _items;
    }
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **Extended Deletion Information:** Create new interfaces extending ISoftDeletable
2. **Deletion Metadata:** Add interfaces for deletion context information
3. **Cascading Deletion:** Create interfaces for related entity deletion

**Where NOT to Add Logic:**
- **Deletion Implementation:** Belongs in concrete implementations
- **Deletion Workflows:** Belongs in service layer
- **Validation Logic:** Belongs in domain entities or validators
- **Business Rules:** Belongs in service layer

**Safe Extension Points:**
- New interfaces extending ISoftDeletable
- Specialized deletion interfaces for specific entity types
- Marker interfaces for deletion categorization

**Common Mistakes:**
1. **Adding Implementation:** Interfaces should remain pure contracts
2. **Making Status Writable:** Should remain read-only for encapsulation
3. **Adding Business Rules:** Belongs in concrete classes
4. **Creating Fat Interfaces:** Keep interfaces focused and minimal

**Refactoring Warnings:**
- Changing interface signature affects all implementing classes
- Adding new properties breaks existing implementations
- Removing interface requires updating all references
- Changing property types affects persistence and querying

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Inconsistent Deletion State:** Entities with incorrect IsDeleted values
- **Query Filter Issues:** Soft-deleted entities appearing in results
- **Restoration Problems:** Unable to restore soft-deleted entities

**Null/Reference Risks:**
- **Boolean Property:** Boolean is value type, no null risk
- **Interface Implementation:** Missing implementation causes compile errors

**Performance Risks:**
- **Query Overhead:** Additional WHERE clause in all queries
- **Index Bloat:** Soft-deleted entities consume storage and index space
- **Data Bloat:** Accumulation of soft-deleted records over time

**Logging Points:**
- Interface doesn't provide logging capability
- Implementation classes should log deletion and restoration operations
- Repositories should log query filtering results

**How to Debug Step-by-Step:**
1. **Deletion Status:** Verify IsDeleted flag is properly set and maintained
2. **Query Filtering:** Check that repositories properly filter deleted entities
3. **Restoration Logic:** Ensure restoration operations work correctly
4. **Data Consistency:** Verify deletion state consistency across related entities

**Common Debugging Scenarios:**
```csharp
// Debug deletion status
var entity = new TestSoftDeletableEntity();
Debug.WriteLine($"Initial deletion status: {entity.IsDeleted}");

entity.MarkAsDeleted();
Debug.WriteLine($"After marking deleted: {entity.IsDeleted}");

entity.Restore();
Debug.WriteLine($"After restoration: {entity.IsDeleted}");

// Debug query filtering
var repository = new TestSoftDeletableRepository<TestSoftDeletableEntity>();
var allEntities = repository.GetAllAsync().Result;
Debug.WriteLine($"Active entities count: {allEntities.Count()}");

var allIncludingDeleted = repository.GetAllIncludingDeletedAsync().Result;
Debug.WriteLine($"All entities count: {allIncludingDeleted.Count()}");
```

## Cross-References

**Related Classes:**
- `BaseEntity`: Primary implementation of ISoftDeletable
- `Project`: Concrete entity implementing ISoftDeletable
- `Workspace`: Concrete entity implementing ISoftDeletable
- `IAuditableEntity`: Companion interface for audit functionality

**Upstream Callers:**
- `Snitcher.Repository.Repositories.EfRepository<T>`: Automatic query filtering
- Soft deletion services for deletion and restoration operations
- Data management and cleanup systems

**Downstream Dependencies:**
- No dependencies - pure contract interface

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md` (primary implementation)
- **After:** `IAuditableEntity.md`, `Project.md`, `Workspace.md`
- **Related:** `IEntity.md` (companion identity interface)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Soft Delete Pattern:** Safe data removal with recovery capability
- **Interface-Based Contracts:** Clean separation of deletion concerns
- **Data Recovery Support:** Foundation for undo and restoration features
- **Audit Trail Preservation:** Maintaining complete data history
- **Marker Interface Pattern:** Clear identification of soft-deletable entities

**What Is Project-Specific:**
- **Boolean Flag Choice:** Specific choice of boolean for deletion status
- **Simple Interface Design:** Minimal contract approach

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Deletion Contract:** Create interface with deletion status property
2. **Ensure Read-only Access:** Make deletion status property get-only for integrity
3. **Use Appropriate Type:** Choose boolean or enum for deletion status
4. **Keep Interface Minimal:** Include only essential deletion information
5. **Design for Extensibility:** Allow for additional deletion interfaces
6. **Document Deletion Semantics:** Clearly define deletion meaning and usage
7. **Plan for Query Filtering:** Design repository integration for automatic filtering

**Key Architectural Insights:**
- **Soft Deletion is Safe:** Provides safety net for user operations
- **Interfaces Enable Consistency:** Contracts ensure uniform deletion capability
- **Query Filtering is Essential:** Automatic filtering prevents accidental data access
- **Read-Only Preserves Integrity:** Prevents external deletion status manipulation
- **Minimal Contracts are Flexible:** Allow for various implementation strategies

**Implementation Checklist for New Projects:**
- [ ] Define soft deletion interface with status property
- [ ] Ensure deletion status property is read-only
- [ ] Choose appropriate status type (boolean vs enum)
- [ ] Document deletion and restoration workflows
- [ ] Design for extensibility with additional deletion interfaces
- [ ] Test deletion and restoration logic
- [ ] Verify repository query filtering integration
- [ ] Plan for data cleanup and retention policies
