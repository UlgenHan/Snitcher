# Snitcher Architecture Documentation

## Overview

This document describes the clean architecture implementation for the Snitcher desktop application's metadata persistence layer. The architecture follows SOLID principles, dependency injection patterns, and clean separation of concerns.

## Architecture Layers

### 1. Core Layer (Snitcher.Core)

**Purpose**: Domain entities, interfaces, and business abstractions
**Dependencies**: None (dependency-free)
**Key Components**:

#### Entities
- `BaseEntity`: Base class with audit fields and soft delete support
- `Project`: Main project entity with analysis tracking
- `ProjectNamespace`: Hierarchical namespace organization
- `MetadataEntry`: Flexible key-value metadata storage

#### Interfaces
- `IEntity<TId>`: Base entity contract
- `IAuditableEntity`: Audit trail support
- `ISoftDeletable`: Soft delete functionality
- `IRepository<T, TId>`: Generic repository pattern
- `IUnitOfWork`: Transaction management

#### Enums & Value Objects
- `MetadataScope`: Defines metadata categorization
- `ProjectPath`: Validated file system path value object

### 2. Repository Layer (Snitcher.Repository)

**Purpose**: Data access infrastructure and EF Core implementation
**Dependencies**: Snitcher.Core, EF Core, SQLite
**Key Components**:

#### Context & Configuration
- `SnitcherDbContext`: EF Core context with SQLite provider
- Entity configurations with Fluent API
- Automatic timestamp management
- Soft delete query filters
- Performance-optimized indexes

#### Repositories
- `EfRepository<T>`: Generic repository base class
- `ProjectRepository`: Project-specific data operations
- `MetadataRepository`: Metadata-specific data operations
- `UnitOfWork`: Transaction coordination

#### Extensions
- `ServiceCollectionExtensions`: DI registration helpers
- Database initialization and migration support

### 3. Service Layer (Snitcher.Service)

**Purpose**: Business logic, validation, and application coordination
**Dependencies**: Snitcher.Core only (no EF Core awareness)
**Key Components**:

#### Services
- `ProjectService`: Project management business logic
- `MetadataService`: Metadata management business logic
- Input validation and business rule enforcement
- Transaction coordination

#### DTOs
- Read/Write models for data transfer
- Validation DTOs for input handling
- Type-safe metadata operations

#### Configuration
- `SnitcherConfiguration`: Centralized application setup
- `SnitcherOptions`: Configuration options management
- Database provider abstraction

## Key Architectural Principles

### 1. Dependency Inversion
- High-level modules don't depend on low-level modules
- Both depend on abstractions (interfaces)
- Service layer depends only on Core interfaces

### 2. Single Responsibility
- Each class has one reason to change
- Clear separation between data access, business logic, and domain modeling

### 3. Open/Closed Principle
- Open for extension, closed for modification
- Generic repository allows easy addition of new entities
- Service interfaces support implementation swapping

### 4. Interface Segregation
- Clients depend only on interfaces they use
- Specific repository interfaces for domain-specific operations

### 5. Dependency Injection
- Constructor injection throughout
- Service provider manages object lifetimes
- Testable and maintainable code

## Database Design

### SQLite Configuration
- File-based storage in user's AppData directory
- Automatic database creation and migration support
- Optimized for desktop application scenarios

### Entity Relationships
```
Project (1) -----> (N) ProjectNamespace
ProjectNamespace (1) -----> (N) ProjectNamespace (self-referencing)
MetadataEntry (N) -----> (1) Project (optional)
MetadataEntry (N) -----> (1) ProjectNamespace (optional)
```

### Indexing Strategy
- Unique constraints on business keys
- Composite indexes for common query patterns
- Performance-optimized for metadata lookups

## Usage Patterns

### 1. Basic CRUD Operations
```csharp
// Create
var project = await projectService.CreateProjectAsync(new CreateProjectDto { ... });

// Read
var project = await projectService.GetProjectByIdAsync(id);

// Update
var updated = await projectService.UpdateProjectAsync(id, new UpdateProjectDto { ... });

// Delete
await projectService.DeleteProjectAsync(id);
```

### 2. Transaction Management
```csharp
await unitOfWork.BeginTransactionAsync();
try
{
    // Multiple operations
    await projectService.CreateProjectAsync(...);
    await metadataService.SetTypedMetadataAsync(...);
    
    await unitOfWork.CommitAsync();
}
catch
{
    await unitOfWork.RollbackAsync();
}
```

### 3. Metadata Operations
```csharp
// Typed metadata access
await metadataService.SetTypedMetadataAsync(MetadataScope.Global, "theme", "dark");
var theme = await metadataService.GetTypedMetadataAsync<string>(MetadataScope.Global, "theme");

// Entity-scoped metadata
await metadataService.SetTypedMetadataAsync(MetadataScope.Project, "lastAnalysis", DateTime.UtcNow, projectId, "Project");
```

## Testing Strategy

### Unit Testing
- Mock repository interfaces
- Test business logic in isolation
- Validate input handling and error cases

### Integration Testing
- In-memory database provider
- Full repository and service testing
- Transaction rollback testing

### Configuration Testing
- Multiple database providers
- Connection string variations
- Migration and initialization testing

## Performance Considerations

### Database Optimizations
- Strategic indexing for common queries
- Query filters for soft delete
- Batch operations for bulk updates

### Memory Management
- Proper disposal of DbContext
- Scoped service lifetimes
- Efficient DTO mapping

### Caching Strategy
- Application-level caching for frequently accessed metadata
- Read-only metadata optimization
- Lazy loading for navigation properties

## Extensibility Points

### 1. New Entities
- Create entity class inheriting from BaseEntity
- Add EF Core configuration
- Implement repository if needed
- Add service layer operations

### 2. Additional Database Providers
- Implement new DbContext configuration
- Add provider-specific extensions
- Update configuration options

### 3. Enhanced Metadata Types
- Extend MetadataEntry with custom properties
- Add validation for new value types
- Implement type-specific service methods

## Security Considerations

### Data Protection
- SQLite file permissions
- Sensitive data logging controls
- Input validation and sanitization

### Access Control
- Service-level authorization hooks
- Entity-level security checks
- Audit trail maintenance

## Deployment Considerations

### Database Migrations
- EF Core migration strategy
- Version compatibility handling
- Data backup and restore procedures

### Configuration Management
- Environment-specific settings
- Connection string security
- Feature flag support

## Conclusion

This architecture provides a solid foundation for the Snitcher application's metadata persistence needs. It balances simplicity with extensibility, ensuring the system can grow and evolve over time while maintaining clean, maintainable code.

The layered approach ensures that each component has clear responsibilities and minimal dependencies, making the system easier to test, maintain, and extend for future requirements.
