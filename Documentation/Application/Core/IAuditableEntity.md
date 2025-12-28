# IAuditableEntity

## Overview

IAuditableEntity is the contract interface for entities that support comprehensive audit trails in the Snitcher application. It establishes the pattern that auditable entities must track their creation and modification timestamps, enabling complete change history tracking, compliance requirements, and debugging capabilities. This interface is fundamental to maintaining data integrity and providing visibility into entity lifecycle events.

This interface exists to enforce the architectural principle that important domain entities must maintain audit information for security, compliance, and debugging purposes. Without IAuditableEntity, the system would lack standardized audit trail capabilities, making it difficult to track when entities were created or modified, comply with data governance requirements, or debug issues related to entity state changes.

If IAuditableEntity were removed, the system would lose:
- Standardized audit trail capabilities across all entities
- Creation and modification timestamp tracking
- Foundation for compliance and governance reporting
- Debugging capabilities for entity state changes
- Audit log generation and history tracking
- Data integrity verification through timestamp analysis

## Tech Stack Identification

**Languages Used:**
- C# 12.0 (.NET 8.0)

**Frameworks:**
- .NET 8.0 Base Class Library
- System namespace for DateTime handling

**Libraries:**
- System namespace for core functionality
- No external dependencies required

**UI Frameworks:**
- N/A (Infrastructure layer)

**Persistence/Communication Technologies:**
- Framework-agnostic design
- Compatible with any persistence mechanism
- Used by Entity Framework Core for automatic timestamp management

**Build Tools:**
- MSBuild with .NET 8.0 SDK
- No special build requirements

**Runtime Assumptions:**
- Runs on .NET 8.0 runtime or higher
- Requires DateTime support
- UTC timestamp handling for consistency

**Version Hints:**
- Uses modern C# property design
- Compatible with .NET 8.0 and later versions
- Follows standard interface design patterns

## Architectural Role

**Layer:** Domain Layer (Clean Architecture - Core)

**Responsibility Boundaries:**
- MUST define audit trail contract for entities
- MUST provide timestamp access for creation and modification
- MUST NOT specify implementation details for timestamp management
- MUST NOT depend on external infrastructure
- MUST NOT enforce specific timestamp formats

**What it MUST do:**
- Define contract for creation timestamp tracking
- Define contract for modification timestamp tracking
- Enable audit trail generation and reporting
- Support compliance and governance requirements

**What it MUST NOT do:**
- Specify how timestamps are generated or updated
- Enforce specific timezone handling
- Provide validation logic for timestamps
- Depend on persistence frameworks or logging systems

**Dependencies (Incoming):**
- BaseEntity (primary implementation)
- All auditable entity classes
- Repository layer for automatic timestamp management
- Service layer for audit-related operations

**Dependencies (Outgoing):**
- No dependencies (pure contract interface)

## Execution Flow

**Where execution starts:**
- Entity class declarations implementing the interface
- Repository layer automatic timestamp updates
- Audit service operations and reporting

**How control reaches this component:**
1. Entity classes implement IAuditableEntity for audit contract
2. Repository classes use interface for automatic timestamp management
3. Audit services use interface for generating audit reports
4. Framework reflection discovers interface implementations

**Call Sequence (Entity Creation):**
1. Entity instance created through repository or service
2. BaseEntity constructor sets CreatedAt timestamp
3. IAuditableEntity contract satisfied automatically
4. Entity persisted with creation timestamp

**Call Sequence (Entity Modification):**
1. Entity modified through business operations
2. Repository detects entity change
3. UpdatedAt timestamp automatically updated
4. IAuditableEntity interface provides access to new timestamp

**Synchronous vs Asynchronous Behavior:**
- Pure interface contract - no execution behavior
- Implementation behavior depends on concrete classes
- Timestamp updates typically synchronous in Entity Framework

**Threading/Dispatcher Notes:**
- Interface itself is thread-safe (no state)
- Thread safety depends on implementation
- Timestamp updates should be atomic in multi-threaded scenarios

**Lifecycle (Creation → Usage → Disposal):**
1. **Creation:** Interface exists for entire application lifetime
2. **Usage:** Referenced by entity declarations and audit operations
3. **Disposal:** Interface lifetime managed by .NET runtime

## Public API / Surface Area

**Interface Members:**
- `DateTime CreatedAt { get; }`: Read-only property for entity creation timestamp
- `DateTime UpdatedAt { get; }`: Read-only property for last modification timestamp

**Expected Input/Output:**
- **Input:** No input parameters
- **Output:** DateTime values representing audit timestamps

**Side Effects:**
- No side effects - pure contract interface
- Enforces compile-time requirements on implementing classes

**Error Behavior:**
- No exceptions thrown by interface itself
- Implementation classes may throw exceptions
- DateTime values should be valid (non-default values)

## Internal Logic Breakdown

**Interface Design Philosophy:**
```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
}
```
- Minimal contract focusing only on essential audit information
- Read-only properties prevent external timestamp manipulation
- DateTime type provides sufficient precision for audit purposes
- No validation or format constraints to maintain flexibility

**Timestamp Property Design:**
- **CreatedAt**: Immutable timestamp of entity creation
- **UpdatedAt**: Mutable timestamp reflecting last modification
- Both properties use DateTime for consistency
- UTC timestamps recommended for distributed systems

**Contract Enforcement Logic:**
- Compiler enforces property implementation
- Read-only access prevents external modification
- Interface ensures consistent audit capability across entities
- No implementation details specified in contract

**Important Invariants:**
- CreatedAt should never change after entity creation
- UpdatedAt should always be >= CreatedAt
- Timestamps should represent actual events (not default values)
- Interface must remain implementation-agnostic

## Patterns & Principles Used

**Design Patterns:**
- **Interface Segregation Principle:** Minimal, focused audit contract
- **Dependency Inversion Principle:** High-level modules depend on abstraction
- **Marker Interface Pattern:** Marks entities as auditable

**Architectural Patterns:**
- **Domain-Driven Design (DDD):** Audit trail as domain concern
- **Clean Architecture:** Infrastructure-free domain contract
- **Audit Trail Pattern:** Standardized audit information tracking

**Why These Patterns Were Chosen:**
- **Interface Segregation:** Keeps audit contract focused and minimal
- **Dependency Inversion:** Enables flexible audit implementations
- **Marker Interface:** Clearly identifies auditable entities

**Trade-offs:**
- **Pros:** Clear audit contract, flexible implementation, compliance support
- **Cons:** Additional interface layer, timestamp management complexity
- **Decision:** Benefits of audit capability and compliance outweigh costs

**Anti-patterns Avoided:**
- **Fat Interfaces:** Interface contains only essential audit contract
- **Implementation Leakage:** Interface doesn't specify timestamp generation
- **Concrete Dependencies:** Interface has no external dependencies

## Binding / Wiring / Configuration

**Dependency Injection:**
- Interface itself not registered in DI container
- Used by audit services for entity filtering
- Enables automatic timestamp management in repositories

**Data Binding:**
- No data binding requirements
- Used by Entity Framework Core for automatic timestamp updates
- Supports convention-based audit field discovery

**Configuration Sources:**
- No configuration required
- Discovered through reflection by frameworks
- Used by audit reporting tools for entity filtering

**Runtime Wiring:**
- Interface resolution through .NET type system
- Automatic timestamp management by infrastructure
- Runtime type checking for audit operations

**Registration Points:**
- Entity class declarations implement interface
- Repository configurations for automatic timestamp updates
- Audit service registrations for entity filtering

## Example Usage (CRITICAL)

**Minimal Example:**
```csharp
// Entity implementing interface
public class AuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public string Name { get; set; } = string.Empty;
    
    public AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Realistic Example:**
```csharp
// Audit service using interface
public class AuditService
{
    public IEnumerable<AuditRecord> GetAuditTrail<T>(IEnumerable<T> entities) 
        where T : IAuditableEntity
    {
        return entities.Select(e => new AuditRecord
        {
            EntityType = typeof(T).Name,
            CreatedAt = e.CreatedAt,
            LastModified = e.UpdatedAt,
            Age = DateTime.UtcNow - e.CreatedAt
        });
    }
    
    public IEnumerable<T> GetRecentlyModified<T>(IEnumerable<T> entities, TimeSpan threshold)
        where T : IAuditableEntity
    {
        var cutoff = DateTime.UtcNow - threshold;
        return entities.Where(e => e.UpdatedAt >= cutoff);
    }
}

// Repository with automatic timestamp management
public class AuditableRepository<T> : IRepository<T> where T : IAuditableEntity
{
    public async Task UpdateAsync(T entity)
    {
        // Automatic timestamp update
        if (entity is IUpdatableEntity updatable)
        {
            updatable.UpdateTimestamp();
        }
        
        await _context.SaveChangesAsync();
    }
}
```

**Incorrect Usage Example:**
```csharp
// WRONG: Trying to modify timestamps through interface
public class BadEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; set; } // Should be private set
    public DateTime UpdatedAt { get; set; } // Should be private set
}

// WRONG: Assuming interface manages timestamps
public class BadService
{
    public void CreateEntity<T>() where T : IAuditableEntity
    {
        var entity = new T(); // WRONG - Can't instantiate and set timestamps
        // Interface doesn't provide timestamp management
    }
}

// WRONG: Using local time instead of UTC
public class BadAuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; private set; } = DateTime.Now; // Should be Utc
    public DateTime UpdatedAt { get; private set; } = DateTime.Now; // Should be Utc
}
```

**How to Test in Isolation:**
```csharp
[Test]
public void Entity_ShouldImplementIAuditableEntity()
{
    // Arrange
    var entity = new TestAuditableEntity();
    
    // Act & Assert
    Assert.That(entity, Is.InstanceOf<IAuditableEntity>());
    Assert.That(entity.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    Assert.That(entity.UpdatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
}

[Test]
public void AuditService_ShouldGenerateAuditTrail()
{
    // Arrange
    var entities = new[]
    {
        new TestAuditableEntity { Name = "Entity1" },
        new TestAuditableEntity { Name = "Entity2" }
    };
    var auditService = new AuditService();
    
    // Act
    var auditTrail = auditService.GetAuditTrail(entities).ToList();
    
    // Assert
    Assert.That(auditTrail.Count, Is.EqualTo(2));
    Assert.That(auditTrail.All(r => r.CreatedAt > DateTime.MinValue), Is.True);
}

private class TestAuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public string Name { get; set; } = string.Empty;
}
```

**How to Mock or Replace:**
```csharp
// Mock entity for testing
public class MockAuditableEntity : IAuditableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Mock audit service
public class MockAuditService
{
    public IEnumerable<AuditRecord> GetAuditTrail<T>(IEnumerable<T> entities)
        where T : IAuditableEntity
    {
        return entities.Select(e => new AuditRecord
        {
            EntityType = typeof(T).Name,
            CreatedAt = e.CreatedAt,
            LastModified = e.UpdatedAt
        });
    }
}
```

## Extension & Modification Guide

**How to Add New Features Here:**
1. **Extended Audit Information:** Create new interfaces extending IAuditableEntity
2. **Additional Timestamps:** Add interfaces for specific event timestamps
3. **Audit Metadata:** Create interfaces for audit context information

**Where NOT to Add Logic:**
- **Timestamp Generation:** Belongs in concrete implementations
- **Audit Log Writing:** Belongs in infrastructure layer
- **Validation Logic:** Belongs in domain entities or validators
- **Business Rules:** Belongs in service layer

**Safe Extension Points:**
- New interfaces extending IAuditableEntity
- Specialized audit interfaces for specific entity types
- Marker interfaces for audit categorization

**Common Mistakes:**
1. **Adding Implementation:** Interfaces should remain pure contracts
2. **Making Timestamps Writable:** Should remain read-only for integrity
3. **Adding Business Rules:** Belongs in concrete classes
4. **Creating Fat Interfaces:** Keep interfaces focused and minimal

**Refactoring Warnings:**
- Changing interface signature affects all implementing classes
- Adding new properties breaks existing implementations
- Removing interface requires updating all references
- Changing property types affects persistence and reporting

## Failure Modes & Debugging

**Common Runtime Errors:**
- **Default Timestamp Values:** Entities with uninitialized timestamps
- **Timestamp Inconsistencies:** UpdatedAt earlier than CreatedAt
- **Timezone Issues:** Mixed UTC and local timestamps

**Null/Reference Risks:**
- **DateTime Properties:** DateTime is value type, no null risk
- **Interface Implementation:** Missing implementation causes compile errors

**Performance Risks:**
- **Frequent Timestamp Updates:** May cause excessive database writes
- **Audit Query Overhead:** Filtering by timestamps may be expensive
- **DateTime Precision:** High precision timestamps may impact storage

**Logging Points:**
- Interface doesn't provide logging capability
- Implementation classes should log timestamp updates
- Audit services should log audit trail generation

**How to Debug Step-by-Step:**
1. **Timestamp Initialization:** Verify CreatedAt is set on entity creation
2. **Update Logic:** Check that UpdatedAt is properly maintained
3. **Timezone Consistency:** Ensure all timestamps use UTC
4. **Audit Trail Generation:** Verify audit services read timestamps correctly

**Common Debugging Scenarios:**
```csharp
// Debug timestamp initialization
var entity = new TestAuditableEntity();
Debug.WriteLine($"Created At: {entity.CreatedAt} (UTC: {entity.CreatedAt.Kind})");
Debug.WriteLine($"Updated At: {entity.UpdatedAt} (UTC: {entity.UpdatedAt.Kind})");

// Debug timestamp consistency
Debug.WriteLine($"Timestamps consistent: {entity.UpdatedAt >= entity.CreatedAt}");

// Debug audit trail generation
var auditService = new AuditService();
var entities = new[] { entity };
var auditTrail = auditService.GetAuditTrail(entities);
foreach (var record in auditTrail)
{
    Debug.WriteLine($"Entity: {record.EntityType}, Created: {record.CreatedAt}");
}
```

## Cross-References

**Related Classes:**
- `BaseEntity`: Primary implementation of IAuditableEntity
- `Project`: Concrete entity implementing IAuditableEntity
- `Workspace`: Concrete entity implementing IAuditableEntity
- `ISoftDeletable`: Companion interface for soft delete functionality

**Upstream Callers:**
- `Snitcher.Repository.Repositories.EfRepository<T>`: Automatic timestamp management
- Audit services for reporting and compliance
- Compliance and governance systems

**Downstream Dependencies:**
- No dependencies - pure contract interface

**Documents That Should Be Read Before/After:**
- **Before:** `BaseEntity.md` (primary implementation)
- **After:** `ISoftDeletable.md`, `Project.md`, `Workspace.md`
- **Related:** `IEntity.md` (companion identity interface)

## Knowledge Transfer Notes

**What Concepts Here Are Reusable in Other Projects:**
- **Audit Trail Pattern:** Standardized audit information tracking
- **Interface-Based Contracts:** Clean separation of audit concerns
- **Timestamp Management:** Consistent creation and modification tracking
- **Compliance Support:** Foundation for governance and reporting
- **Marker Interface Pattern:** Clear identification of auditable entities

**What Is Project-Specific:**
- **DateTime Choice:** Specific choice of DateTime for timestamps
- **Two-Field Approach:** Specific CreatedAt/UpdatedAt pattern

**How to Recreate This Pattern from Scratch Elsewhere:**
1. **Define Audit Contract:** Create interface with essential timestamp properties
2. **Ensure Read-only Access:** Make timestamp properties get-only for integrity
3. **Use Appropriate Types:** Choose DateTime or DateTimeOffset based on needs
4. **Keep Interface Minimal:** Include only essential audit information
5. **Design for Extensibility:** Allow for additional audit interfaces
6. **Document Timestamp Semantics:** Clearly define timestamp meaning and usage
7. **Consider Timezone Handling:** Specify UTC or local time requirements

**Key Architectural Insights:**
- **Audit is Cross-Cutting:** Audit concerns span multiple layers
- **Interfaces Enable Consistency:** Contracts ensure uniform audit capability
- **Timestamps are Critical:** Essential for debugging and compliance
- **Read-Only Preserves Integrity:** Prevents external audit manipulation
- **Minimal Contracts are Flexible:** Allow for various implementation strategies

**Implementation Checklist for New Projects:**
- [ ] Define audit trail interface with timestamp properties
- [ ] Ensure timestamp properties are read-only
- [ ] Choose appropriate timestamp type (DateTime vs DateTimeOffset)
- [ ] Document timezone handling requirements
- [ ] Design for extensibility with additional audit interfaces
- [ ] Test timestamp initialization and update logic
- [ ] Verify audit service integration
- [ ] Plan for compliance and reporting requirements
