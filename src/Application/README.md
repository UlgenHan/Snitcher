# Snitcher Application Architecture

A clean, scalable architecture for desktop application metadata persistence using Entity Framework Core and SQLite.

## Quick Start

### 1. Setup Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Snitcher.Service.Configuration;

// Basic setup with default SQLite database
var services = new ServiceCollection();
services.ConfigureSnitcher();

// Or with custom options
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "sqlite";
    options.DatabasePath = "custom_path.db";
});

var serviceProvider = services.BuildServiceProvider();
```

### 2. Initialize Database

```csharp
await SnitcherConfiguration.InitializeDatabaseAsync(serviceProvider);
```

### 3. Use Services

```csharp
var projectService = serviceProvider.GetRequiredService<IProjectService>();
var metadataService = serviceProvider.GetRequiredService<IMetadataService>();

// Create a project
var project = await projectService.CreateProjectAsync(new CreateProjectDto
{
    Name = "MyProject",
    Path = @"C:\Projects\MyProject"
});

// Store metadata
await metadataService.SetTypedMetadataAsync(MetadataScope.Global, "app.theme", "dark");
```

## Architecture Overview

```
┌─────────────────┐
│   Presentation  │  (UI Layer)
└─────────────────┘
         │
┌─────────────────┐
│     Service     │  (Business Logic)
└─────────────────┘
         │
┌─────────────────┐
│   Repository    │  (Data Access)
└─────────────────┘
         │
┌─────────────────┐
│      Core       │  (Domain Model)
└─────────────────┘
```

## Key Features

- **Clean Architecture**: Layered separation with dependency inversion
- **SQLite Storage**: File-based database for desktop applications
- **Metadata System**: Flexible key-value storage with scoping
- **Soft Delete**: Data recovery and audit trails
- **Transaction Support**: Atomic operations across multiple entities
- **Validation**: Input validation and business rule enforcement
- **Extensible**: Easy to add new entities and features

## Project Structure

```
Application/
├── Snitcher.Core/           # Domain entities and interfaces
│   ├── Entities/           # Domain models
│   ├── Interfaces/         # Repository and service contracts
│   ├── Enums/             # Domain enumerations
│   └── ValueObjects/      # Value objects
├── Snitcher.Repository/    # Data access layer
│   ├── Contexts/          # EF Core DbContext
│   ├── Repositories/      # Repository implementations
│   ├── Configurations/    # Entity configurations
│   └── Extensions/        # DI registration
└── Snitcher.Service/       # Business logic layer
    ├── Services/          # Service implementations
    ├── DTOs/              # Data transfer objects
    ├── Interfaces/        # Service contracts
    ├── Validators/        # Input validation
    └── Configuration/     # Application setup
```

## Database Configuration

### SQLite (Default)
```csharp
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "sqlite";
    options.DatabasePath = "snitcher.db"; // Optional, uses AppData by default
});
```

### In-Memory (Testing)
```csharp
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "inmemory";
    options.DatabaseName = "TestDb";
});
```

### Custom Configuration
```csharp
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "custom";
    options.ConfigureDbContext = builder => builder.UseSqlServer(connectionString);
});
```

## Common Usage Patterns

### Project Management
```csharp
// Create project
var project = await projectService.CreateProjectAsync(new CreateProjectDto
{
    Name = "MyProject",
    Description = "Sample project",
    Path = @"C:\Projects\MyProject",
    Version = "1.0.0"
});

// Search projects
var results = await projectService.SearchProjectsAsync("My");

// Update analysis timestamp
await projectService.UpdateLastAnalyzedAsync(project.Id);
```

### Metadata Operations
```csharp
// Global settings
await metadataService.SetTypedMetadataAsync(MetadataScope.Global, "theme", "dark");
var theme = await metadataService.GetTypedMetadataAsync<string>(MetadataScope.Global, "theme");

// Project-specific metadata
await metadataService.SetTypedMetadataAsync(MetadataScope.Project, "lastAnalysis", DateTime.UtcNow, projectId, "Project");

// Get metadata by category
var settings = await metadataService.GetMetadataByCategoryAsync("UI");
```

### Transaction Management
```csharp
var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

await unitOfWork.BeginTransactionAsync();
try
{
    await projectService.CreateProjectAsync(project1);
    await projectService.CreateProjectAsync(project2);
    await metadataService.SetTypedMetadataAsync(...);
    
    await unitOfWork.CommitAsync();
}
catch
{
    await unitOfWork.RollbackAsync();
}
```

## Validation

### Input Validation
All service methods include comprehensive validation:

```csharp
try
{
    await projectService.CreateProjectAsync(invalidData);
}
catch (ArgumentException ex)
{
    // Handle validation errors
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

### Path Validation
```csharp
var validation = await projectService.ValidateProjectPathAsync(@"C:\Path");
if (!validation.IsValid)
{
    Console.WriteLine(validation.ErrorMessage);
}
```

## Testing

### Unit Testing with Mocks
```csharp
var mockRepo = new Mock<IProjectRepository>();
var service = new ProjectService(mockRepo.Object, Mock.Of<IUnitOfWork>());
```

### Integration Testing
```csharp
services.ConfigureSnitcher(options =>
{
    options.DatabaseProvider = "inmemory";
    options.DatabaseName = "TestDb";
});
```

## Migration and Deployment

### Create Migrations
```bash
dotnet ef migrations add InitialCreate --project Snitcher.Repository
```

### Apply Migrations
```csharp
await SnitcherConfiguration.InitializeDatabaseAsync(
    serviceProvider, 
    ensureCreated: false, 
    applyMigrations: true);
```

## Best Practices

1. **Always use transactions** for multi-entity operations
2. **Validate inputs** before processing
3. **Use typed metadata** for type safety
4. **Handle soft deletes** appropriately in queries
5. **Dispose DbContext** properly (handled by DI)
6. **Use appropriate service lifetimes** (Scoped recommended)

## Extending the Architecture

### Adding New Entities
1. Create entity class inheriting from `BaseEntity`
2. Add EF Core configuration
3. Create repository interface and implementation
4. Add service layer operations
5. Register in DI container

### Adding New Metadata Types
1. Extend validation rules
2. Add type conversion helpers
3. Implement service methods for new types

## Troubleshooting

### Common Issues

**Database file not found**: Ensure the directory exists and permissions are correct
**Migration conflicts**: Check for pending migrations and database version
**Performance issues**: Review indexing strategy and query patterns

### Debugging

Enable detailed logging:
```csharp
services.ConfigureSnitcher(options =>
{
    options.EnableSensitiveDataLogging = true;
    options.EnableDetailedErrors = true;
});
```

## Support

For detailed architecture documentation, see [ARCHITECTURE.md](ARCHITECTURE.md).

This architecture is designed for long-term maintainability and extensibility, following clean architecture principles and modern .NET best practices.
