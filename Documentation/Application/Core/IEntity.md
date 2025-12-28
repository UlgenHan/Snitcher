# IEntity

## Overview

IEntity is the fundamental identity contract interface for all domain entities in the Snitcher application. It establishes the pattern that every entity must have a unique identifier, enabling proper entity lifecycle management, equality comparison, and repository pattern implementation. The interface exists in both generic and non-generic forms to provide flexibility while maintaining type safety.

This interface exists to enforce the architectural principle that all domain entities must have proper identity semantics. Without IEntity, entities would lack a standardized way to identify themselves, leading to inconsistent equality implementations, repository pattern complications, and potential data integrity issues. The interface serves as the foundation for entity tracking, persistence, and domain modeling.

If IEntity were removed, the system would lose:
- Standardized entity identification across all domain objects
- Type-safe repository pattern implementation
- Consistent equality comparison semantics
- Foundation for audit trails and entity lifecycle management
- Ability to create generic repositories and services
- Clear contract for entity identity in the domain model

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- .NET 8.0 Base Class Library
- Generic type system for type safety

**Libraries:**
- System namespace for core functionality
- No external dependencies required

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- Framework-agnostic design
- Compatible with any persistence mechanism
- Used by Entity Framework Core for entity mapping

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- No special build requirements

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires generic type system support
- No special runtime requirements

**Version Hints:**
- Uses modern C# generic constraints
- Compatible with .NET 8.0 and later versions
- Follows standard interface design patterns

## Architectural Role

**Layer:** Domain Layer (Clean Architecture - Core)

**Responsibility Boundaries:**
- MUST define identity contract for entities
- MUST provide type-safe identifier access
- MUST NOT contain implementation details
- MUST NOT depend on external infrastructure
- MUST NOT enforce specific identifier types

**What it MUST do:**
- Define contract for entity identification
- Support generic and non-generic usage patterns
- Enable type-safe repository implementations
- Provide foundation for entity equality

**What it MUST NOT do:**
- Specify implementation details for identifier generation
- Enforce specific identifier types beyond generic constraints
- Provide validation logic for identifiers
- Depend on persistence frameworks

**Dependencies (Incoming):**
- All concrete entities (Project, Workspace, etc.)
- Repository layer for generic repository implementations
- Service layer for generic service patterns

**Dependencies (Outgoing):**
- No dependencies (pure contract interface)

## Execution Flow

**Where execution starts:**
- Entity class declarations implementing the interface
- Generic repository type parameter constraints
- Service layer generic type constraints

**How control reaches this component:**
1. Entity classes implement IEntity for identity contract
2. Repository classes use IEntity<T> for generic constraints
3. Service classes use IEntity for generic operations
4. Framework reflection discovers interface implementations

**Call Sequence (Entity Declaration):**
1. Developer creates entity class
2. Entity class inherits from BaseEntity (which implements IEntity)
3. Interface contract enforced by compiler
4. Entity must provide Id property implementation

**Call Sequence (Repository Usage):**
1. Generic repository declared as IRepository<T> where T : IEntity
2. Type system ensures T has Id property
3. Repository can safely use entity.Id for operations
4. Compile-time type safety guaranteed

**Synchronous vs Asynchronous Behavior:**
- Pure interface contract - no execution behavior
- Implementation behavior depends on concrete classes
- No inherent blocking or non-blocking characteristics

**Threading/Dispatcher Notes:**
- Interface itself is thread-safe (no state)
- Thread safety depends on implementation
- No synchronization requirements for interface itself

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Interface exists for entire application lifetime
2. **Usage:** Referenced by entity declarations and generic types
3. **Disposal:** Interface lifetime managed by .NET runtime

## Public API / Surface Area

**Interfaces:**
- `IEntity<TId>`: Generic interface for entities with specific identifier types
- `IEntity`: Non-generic interface for entities with Guid identifiers

**Generic Interface Members:**
- `TId Id { get; }`: Read-only property for entity identifier

**Non-generic Interface Members:**
- `Guid Id { get; }`: Read-only property for Guid-based entity identifier

**Expected Input/Output:**
- **Input:** Type parameter for generic version
- **Output:** Contract requiring Id property implementation

**Side Effects:**
- No side effects - pure contract interface
- Enforces compile-time requirements on implementing classes

**Error Behavior:**
- No exceptions thrown by interface itself
- Implementation classes may throw exceptions
- Compiler enforces interface contract compliance

## Internal Logic Breakdown

**Generic Interface Design:**
```csharp
public interface IEntity<TId>
{
    TId Id { get; }
}
```
- Provides type-safe identifier access
- Allows any identifier type (int, Guid, string, custom)
- Read-only property prevents external identifier modification
- Generic constraint enables flexible entity design

**Non-generic Interface Design:**
```csharp
public interface IEntity : IEntity<Guid>
{
}
```
- Inherits from generic interface with Guid constraint
- Provides convenient interface for most common use case
- Maintains consistency with generic version
- Reduces boilerplate for Guid-based entities

**Interface Hierarchy Logic:**
- Non-generic interface extends generic with specific type
- Enables both flexible and convenient usage patterns
- Maintains backward compatibility
- Supports evolution toward more specific identifier types

**Important Invariants:**
- Id property must be read-only (get only)
- Interface must not specify implementation details
- Generic version must support any reasonable identifier type
- Non-generic version must specialize to Guid for common case

## Patterns & Principles Used

**Design Patterns:**
- **Interface Segregation Principle:** Minimal, focused contract
- **Dependency Inversion Principle:** High-level modules depend on abstraction
- **Generic Programming:** Type-safe operations across entity types

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** Entity identity as fundamental concept
- **Clean Architecture:** Infrastructure-free domain contract
- **Repository Pattern Support:** Enables generic repository implementations

**Why These Patterns Were Chosen:**
- **Interface Segregation:** Keeps contract minimal and focused
- **Dependency Inversion:** Enables flexible implementations and testing
- **Generic Programming:** Provides type safety while maintaining flexibility

**Trade-offs:**
- **Pros:** Type safety, flexibility, testability, clean separation
- **Cons:** Additional interface layer, slight complexity overhead
- **Decision:** Benefits of type safety and flexibility outweigh costs

**Anti-patterns Avoided:**
- **Fat Interfaces:** Interface contains only essential identity contract
- **Concrete Dependencies:** Interface has no external dependencies
- **Implementation Leakage:** Interface doesn't expose implementation details

## Binding / Wiring / Configuration

**Dependency Injection:**
- Interface itself not registered in DI container
- Used as generic constraint for service registrations
- Enables generic service and repository registrations

**Data Binding:**
- No data binding requirements
- Used by Entity Framework Core for entity mapping
- Supports convention-based entity discovery

**Configuration Sources:**
- No configuration required
- Discovered through reflection by frameworks
- Used by code analysis tools for entity detection

**Runtime Wiring:**
- Interface resolution through .NET type system
- Generic constraint enforcement at compile time
- Runtime type checking for generic operations

**Registration Points:**
- Entity class declarations implement interface
- Generic repository declarations use interface as constraint
- Generic service declarations use interface as constraint

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Entity implementing interface
public class MyEntity : IEntity<Guid>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

// Using interface in generic repository
public interface IRepository<T> where T : IEntity<Guid>
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
}
```

**Realistic Example:**
```csharp
// Domain entities with different identifier types
public class User : IEntity<int>
{
    public int Id { get; private set; }
    public string Username { get; set; } = string.Empty;
}

public class Product : IEntity<Guid>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

// Generic repository using interface constraint
public class EfRepository<T> : IRepository<T> where T : IEntity
{
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }
    
    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }
}

// Generic service using interface
public class BaseService<T> where T : IEntity
{
    public async Task<T?> GetEntityAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Trying to modify Id through interface
public class BadEntity : IEntity<Guid>
{
    public Guid Id { get; set; } // Should be private or protected set
}

// WRONG: Interface with implementation details
public interface IBadEntity : IEntity<Guid>
{
    Guid Id { get; set; } // Setter allows external modification
    void Save(); // Implementation detail in interface
}

// WRONG: Assuming interface creates entities
public class BadService
{
    public T CreateEntity<T>() where T : IEntity<Guid>
    {
        return new T(); // WRONG - Can't instantiate generic type
    }
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void Entity_ShouldImplementIEntity()
{
    // Arrange
    var entity = new TestEntity();
    
    // Act & Assert
    Assert.That(entity, Is.InstanceOf<IEntity<Guid>>());
    Assert.That(entity.Id, Is.Not.EqualTo(Guid.Empty));
}

[Test]
public void GenericRepository_ShouldWorkWithIEntity()
{
    // Arrange
    var repository = new TestRepository<TestEntity>();
    var entityId = Guid.NewGuid();
    
    // Act
    var result = repository.GetById(entityId);
    
    // Assert
    Assert.That(result, Is.Null); // Repository is empty
}

private class TestEntity : IEntity<Guid>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

private class TestRepository<T> where T : IEntity<Guid>
{
    public T? GetById(Guid id) => default;
}
```

**How to Mock or Replace:**
```csharp
// Mock entity for testing
public class MockEntity : IEntity<Guid>
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

// Mock repository using interface constraint
public class MockRepository<T> : IRepository<T> where T : IEntity<Guid>
{
    private readonly Dictionary<Guid, T> _items = new();
    
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return _items.TryGetValue(id, out var item) ? item : default;
    }
    
    public async Task AddAsync(T entity)
    {
        _items[entity.Id] = entity;
    }
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **New Identifier Types:** Create new generic interfaces for specific types
2. **Additional Contracts:** Add new interfaces extending IEntity
3. **Validation Interfaces:** Create interfaces for entity validation contracts

**Where NOT to Add Logic:**
- **Implementation Details:** Interface should remain contract-only
- **Business Logic:** Belongs in entity classes or services
- **Persistence Logic:** Belongs in repository layer
- **Validation Rules:** Belongs in domain entities or validators

**Safe Extension Points:**
- New generic interfaces extending IEntity
- Specialized interfaces for specific entity types
- Marker interfaces for entity categorization

**Common Mistakes:**
1. **Adding Implementation:** Interfaces should remain pure contracts
2. **Making Id Writable:** Should remain read-only for encapsulation
3. **Adding Business Rules:** Belongs in concrete classes
4. **Creating Fat Interfaces:** Keep interfaces focused and minimal

**Refactoring Warnings:**
- Changing interface signature affects all implementing classes
- Adding new methods breaks existing implementations
- Removing interface requires updating all references
- Changing generic constraints affects repository and service layers

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Type Mismatch:** Generic type constraints not satisfied
- **Missing Implementation:** Entity class doesn't implement interface properly
- **Identifier Type Conflicts:** Using wrong identifier type in generic context

**Null/Reference Risks:**
- **Id Property:** May return default value if not properly initialized
- **Generic Type Parameters:** May cause runtime type errors if misused

**Performance Risks:**
- **Generic Instantiation:** Minimal overhead from generic constraints
- **Interface Dispatch:** Virtual method dispatch overhead (negligible)
- **Type Checking:** Runtime type checking in generic contexts

**Logging Points:**
- Interface doesn't provide logging capability
- Implementation classes should log entity operations
- Generic repositories can log entity access patterns

**How to Debug Step-by-Step:**
1. **Interface Implementation:** Verify entity classes properly implement interface
2. **Generic Constraints:** Check that generic type parameters satisfy constraints
3. **Type Resolution:** Ensure correct types are used in generic contexts
4. **Identifier Access:** Verify Id property is properly implemented

**Common Debugging Scenarios:**
```csharp
// Debug interface implementation
var entity = new TestEntity();
Debug.WriteLine($"Entity type: {entity.GetType().Name}");
Debug.WriteLine($"Implements IEntity: {entity is IEntity<Guid>}");
Debug.WriteLine($"Entity ID: {entity.Id}");

// Debug generic constraints
public void ProcessEntity<T>(T entity) where T : IEntity<Guid>
{
    Debug.WriteLine($"Processing entity of type: {typeof(T).Name}");
    Debug.WriteLine($"Entity ID: {entity.Id}");
}

// Debug type resolution
var entityType = typeof(TestEntity);
var implementsInterface = typeof(IEntity<Guid>).IsAssignableFrom(entityType);
Debug.WriteLine($"{entityType.Name} implements IEntity<Guid>: {implementsInterface}");
```

## Cross-References

**Related Classes:**
- `BaseEntity`: Primary implementation of IEntity interface
- `Project`: Concrete entity implementing IEntity
- `Workspace`: Concrete entity implementing IEntity
- `ProjectNamespace`: Concrete entity implementing IEntity

**Upstream Callers:**
- `Snitcher.Repository.Repositories.EfRepository<T>`: Generic repository implementation
- `Snitcher.Service.Services.BaseService<T>`: Generic service patterns
- Entity Framework Core for entity mapping and tracking

**Downstream Dependencies:**
- No dependencies - pure contract interface

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md` (primary implementation)
- **After:** `IRepository.md`, `Project.md`, `Workspace.md`
- **Related:** `IAuditableEntity.md`, `ISoftDeletable.md`

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Entity Identity Pattern:** Standardized entity identification approach
- **Generic Interface Design:** Type-safe contracts for domain entities
- **Interface Segregation:** Minimal, focused interface design
- **Repository Pattern Support:** Enabling generic repository implementations
- **Clean Architecture Contracts:** Infrastructure-free domain interfaces

**What Is Project-Specific:**
- **Guid Default:** Choice of Guid as default identifier type
- **Non-generic Convenience:** Specific convenience interface for Guid entities

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Generic Contract:** Create IEntity<TId> with Id property
2. **Add Convenience Interface:** Create non-generic version for common case
3. **Ensure Read-only Access:** Make Id property get-only for encapsulation
4. **Keep Interface Minimal:** Include only essential identity contract
5. **Support Generic Constraints:** Design for use in generic types
6. **Maintain Type Safety:** Use generic type parameters appropriately
7. **Document Constraints:** Clearly specify interface contract and usage

**Key Architectural Insights:**
- **Identity is Fundamental:** Every entity must have proper identity
- **Contracts Enable Flexibility:** Interfaces allow multiple implementations
- **Generic Constraints Provide Safety:** Type system prevents many errors
- **Minimal Interfaces are Best:** Keep contracts focused and essential
- **Convenience Improves Adoption:** Non-generic versions reduce boilerplate

**Implementation Checklist for New Projects:**
- [ ] Define generic entity identity interface
- [ ] Create convenience interface for common identifier type
- [ ] Ensure Id property is read-only
- [ ] Design for generic constraint usage
- [ ] Keep interface minimal and focused
- [ ] Document interface contract clearly
- [ ] Test interface with multiple identifier types
- [ ] Verify generic repository compatibility
