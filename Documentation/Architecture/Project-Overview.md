# Snitcher Project Architecture Overview

## Overview

Snitcher is a comprehensive desktop application for HTTP traffic interception, analysis, and workspace management. Built with .NET 8.0 and Avalonia UI, the application implements clean architecture principles with distinct layers for presentation, application logic, domain modeling, and infrastructure concerns. The system combines a modern MVVM desktop interface with a powerful proxy engine for HTTP traffic monitoring and manipulation.

**Why it exists**: To provide developers with a professional-grade tool for analyzing HTTP traffic, managing development workspaces, and inspecting network communications with an extensible, modular architecture.

**What problem it solves**: Eliminates the need for multiple disparate tools by combining workspace management, HTTP interception, and traffic analysis in a single, cohesive application with clean separation of concerns and extensible architecture.

**What would break if removed**: The entire application ecosystem would cease to exist, losing the integrated workspace management, HTTP interception capabilities, and extensible architecture that enables powerful network analysis and development workflow management.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0 (Core framework)
- Avalonia UI 11.3.10 (Cross-platform desktop UI)
- Entity Framework Core 8.0 (Data persistence)
- Microsoft.Extensions.Hosting 8.0.1 (Application hosting)
- Microsoft.Extensions.DependencyInjection 8.0.1 (DI container)

**UI Framework**: Avalonia UI with MVVM pattern using CommunityToolkit.Mvvm

**Libraries**:
- CommunityToolkit.Mvvm 8.2.1 (MVVM helpers)
- FluentIcons.Avalonia 2.0.316.1 (Icon library)
- SQLite (Database provider)
- System.Threading.Channels (For proxy operations)

**Persistence**: SQLite database with Entity Framework Core, file-based storage in user AppData

**Build Tools**: MSBuild with .NET SDK 8.0, Visual Studio 2022

**Runtime Assumptions**: Windows desktop environment (primary), .NET 8.0 runtime, SQLite support

## Architectural Role

**Layer**: Application Architecture (Overall System Design)

**Responsibility Boundaries**:
- MUST define overall system structure and layer boundaries
- MUST establish communication patterns between layers
- MUST enforce dependency direction and separation of concerns
- MUST NOT contain implementation details of specific components
- MUST NOT dictate specific technology choices beyond framework

**What it MUST do**:
- Define clean architecture layer separation
- Establish dependency inversion principles
- Provide patterns for cross-cutting concerns
- Enable extensibility and maintainability
- Support testing and development workflows

**What it MUST NOT do**:
- Implement specific business logic
- Handle UI-specific concerns
- Manage database schema details
- Implement HTTP protocol handling

**Dependencies (Incoming**: All application layers and components

**Dependencies (Outgoing**: None (defining architecture)

## Execution Flow

**Where execution starts**: Application entry point in Program.cs creates AppBuilder and configures Avalonia application

**How control flows through architecture**:
1. **Presentation Layer** (UI): User interactions trigger commands in ViewModels
2. **Application Layer** (Services): ViewModels coordinate with application services for business operations
3. **Domain Layer** (Core): Services interact with domain entities and interfaces for business rules
4. **Infrastructure Layer** (Repository): Repository implementations handle data persistence and external service integration
5. **Library Layer** (Sniffer): HTTP proxy engine handles network traffic interception and processing

**Layer Communication Flow**:
- **Downward**: UI → Application → Domain → Infrastructure
- **Upward**: Infrastructure → Domain → Application → UI (through events and observables)
- **Cross-cutting**: Library layer provides HTTP interception services to all layers

**Request Processing Flow**:
1. User action in UI triggers ViewModel command
2. ViewModel calls application service method
3. Service validates business rules using domain entities
4. Service calls repository for data operations
5. Repository executes database operations
6. Results flow back through layers to UI

**HTTP Interception Flow**:
1. HTTP request intercepted by Sniffer library
2. Request flows through interceptor pipeline
3. Interceptors modify/log requests as needed
4. Request forwarded to target server
5. Response intercepted and processed through pipeline
6. Results displayed in UI through data binding

**Synchronous vs asynchronous behavior**: UI operations use async/await throughout, HTTP interception uses async patterns for network operations, database operations are async for responsiveness.

**Threading/Dispatcher notes**: UI operations on UI thread, background operations on thread pool, HTTP interception on dedicated threads, database operations on background threads.

**Lifecycle**: Application startup → Layer initialization → Service registration → UI display → User interactions → Application shutdown

## Public API / Surface Area

**Layer Definitions**:
- **Presentation Layer**: UI components, ViewModels, Views
- **Application Layer**: Services, DTOs, business coordination
- **Domain Layer**: Entities, interfaces, business rules
- **Infrastructure Layer**: Repositories, data access, external services
- **Library Layer**: HTTP proxy engine, interceptors, networking

**Key Interfaces**:
- `IRepository<T, TId>` - Generic data access contract
- `IWorkspaceService` - Workspace business operations
- `IProjectService` - Project business operations
- `IRequestInterceptor` - HTTP request interception contract
- `IResponseInterceptor` - HTTP response interception contract

**Key Classes**:
- `SnitcherMainViewModel` - Main UI coordination
- `DatabaseIntegrationService` - UI-database bridge
- `BaseEntity` - Common entity behavior
- `InterceptorManager` - HTTP interceptor orchestration
- `Workspace` - Core domain entity

**Expected Input/Output**: Each layer provides specific contracts for interaction, maintains separation of concerns, and enables independent testing and development.

**Side Effects**: Architecture enables modular development, independent testing, clean separation of concerns, and extensibility through interface-based design.

**Error Behavior**: Each layer handles errors appropriately, with infrastructure layer handling persistence errors, application layer handling business rule violations, and presentation layer handling user interaction errors.

## Internal Logic Breakdown

**Clean Architecture Implementation**:
```csharp
// Dependency direction: Inward → Core
Presentation → Application → Domain ← Infrastructure
                                     ↑
                                   Library
```
- Dependencies point inward toward the domain
- Domain layer has no external dependencies
- Infrastructure implements domain interfaces
- Presentation depends on application abstractions

**Layer Responsibilities**:
- **Presentation**: UI rendering, user interactions, state management
- **Application**: Business orchestration, use case implementation, transaction coordination
- **Domain**: Business entities, rules, interfaces, core abstractions
- **Infrastructure**: Data persistence, external services, technical concerns
- **Library**: Specialized capabilities (HTTP interception)

**Dependency Injection Structure**:
```csharp
// Service registration in App.axaml.cs
services.ConfigureSnitcher(options => { /* Database config */ });
services.AddScoped<IDatabaseIntegrationService, DatabaseIntegrationService>();
services.AddTransient<SnitcherMainViewModel>();
// ... other service registrations
```

**MVVM Pattern Implementation**:
- Views bind to ViewModel properties and commands
- ViewModels coordinate with application services
- Models represent data structures for UI binding
- Commands encapsulate user interactions

**Repository Pattern Implementation**:
- Generic repository interface for common CRUD operations
- Specific repositories for domain-specific operations
- Unit of Work pattern for transaction management
- Entity Framework Core for ORM implementation

**Interceptor Pipeline Architecture**:
- Chain of Responsibility pattern for HTTP processing
- Priority-based execution ordering
- Error isolation between interceptors
- Extensible through interface-based design

## Patterns & Principles Used

**Clean Architecture**: Layer separation with dependency inversion

**MVVM Pattern**: Separation of UI and logic through ViewModels

**Repository Pattern**: Abstraction of data access operations

**Unit of Work Pattern**: Transaction management across repositories

**Interceptor Pattern**: Cross-cutting concern processing for HTTP traffic

**Dependency Injection**: Loose coupling and testability

**Async/Await Pattern**: Non-blocking operations throughout application

**Observer Pattern**: Reactive UI updates through data binding

**Strategy Pattern**: Pluggable interceptor implementations

**Factory Pattern**: Service creation and configuration

**Why these patterns were chosen**:
- Clean Architecture for maintainability and testability
- MVVM for separation of UI and business logic
- Repository for data access abstraction
- Unit of Work for transaction consistency
- Interceptor for composable HTTP processing
- DI for loose coupling and testability
- Async for responsive user experience
- Observer for reactive UI updates
- Strategy for extensible HTTP processing
- Factory for flexible object creation

**Trade-offs**:
- Complexity overhead from multiple patterns
- Learning curve for developers
- Performance overhead from abstraction layers
- Initial development time investment

**Anti-patterns avoided**:
- No tight coupling between layers
- No business logic in UI layer
- No data access in domain layer
- No synchronous blocking operations
- No hardcoded dependencies

## Binding / Wiring / Configuration

**Dependency Injection Configuration**:
- Services registered in App.axaml.cs
- Scoped, transient, and singleton lifetimes as appropriate
- Interface-based registration for testability
- Configuration options for database and features

**Data Binding Setup**:
- Avalonia UI binding to ViewModel properties
- Observable properties for reactive updates
- Commands for user interaction handling
- Collection binding for lists and grids

**Database Configuration**:
- SQLite database in user AppData
- Entity Framework Core migrations
- Connection string management
- Environment-specific configuration

**HTTP Proxy Configuration**:
- Certificate management for HTTPS interception
- Proxy server settings and startup
- Interceptor registration and ordering
- Network binding and port configuration

## Example Usage

**Architecture Layer Interaction**:
```csharp
// UI Layer (ViewModel)
public async Task CreateWorkspace()
{
    // Application Layer (Service)
    var workspace = await _workspaceService.CreateWorkspaceAsync(name, description);
    
    // Update UI through data binding
    Workspaces.Add(workspace);
}

// Application Layer (Service)
public async Task<Workspace> CreateWorkspaceAsync(string name, string description)
{
    // Domain Layer (Validation)
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name required");
    
    // Infrastructure Layer (Repository)
    var entity = await _repository.AddAsync(new Workspace { Name = name, Description = description });
    return _mapper.MapToWorkspace(entity);
}
```

**HTTP Interceptor Pipeline**:
```csharp
// Library Layer (Interceptor)
public class AuthInterceptor : IRequestInterceptor
{
    public int Priority => 50;
    
    public async Task<HttpRequestMessage> InterceptAsync(HttpRequestMessage request, Flow flow, CancellationToken cancellationToken = default)
    {
        request.Headers["Authorization"] = $"Bearer {GetToken()}";
        return request;
    }
}
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't violate dependency direction
public class ViewModel
{
    private readonly SnitcherDbContext _context; // Should depend on repository
}

// BAD - Don't put business logic in UI
public void Button_Click()
{
    if (string.IsNullOrWhiteSpace(name)) // Should be in service layer
        return;
}
```

**How to test architecture**:
```csharp
// Test each layer independently
[Test]
public void Service_ShouldValidateBusinessRules()
{
    var mockRepository = new Mock<IWorkspaceRepository>();
    var service = new WorkspaceService(mockRepository.Object);
    
    Assert.ThrowsAsync<ArgumentException>(() => service.CreateWorkspaceAsync("", ""));
}

[Test]
public void ViewModel_ShouldCallService()
{
    var mockService = new Mock<IWorkspaceService>();
    var viewModel = new SnitcherMainViewModel(mockService.Object, logger);
    
    viewModel.CreateWorkspaceCommand.Execute("Test");
    
    mockService.Verify(x => x.CreateWorkspaceAsync("Test", ""), Times.Once);
}
```

## Extension & Modification Guide

**How to add new features**:
1. Define domain entities and interfaces in Core layer
2. Implement repositories in Infrastructure layer
3. Create application services in Application layer
4. Add ViewModels and Views in Presentation layer
5. Register dependencies in DI container

**Where NOT to add logic**:
- Don't add business logic to Presentation layer
- Don't add data access to Domain layer
- Don't add UI concerns to Infrastructure layer
- Don't violate dependency direction

**Safe extension points**:
- New entities in Domain layer
- Additional services in Application layer
- New ViewModels in Presentation layer
- Custom interceptors in Library layer

**Common mistakes**:
- Violating dependency direction
- Adding business logic to wrong layer
- Creating tight coupling between layers
- Forgetting to register new services

**Refactoring warnings**:
- Changes to domain interfaces affect all layers
- Database schema changes require migrations
- UI changes may require ViewModel updates
- Interceptor priority changes affect HTTP processing

## Failure Modes & Debugging

**Common runtime errors**:
- Dependency injection failures
- Database connection issues
- HTTP proxy binding problems
- UI binding errors

**Layer-specific failures**:
- **Presentation**: UI not responding, binding errors
- **Application**: Business rule violations, service failures
- **Domain**: Validation errors, entity state issues
- **Infrastructure**: Database errors, external service failures
- **Library**: Network errors, HTTP protocol issues

**Performance risks**:
- Large database queries
- HTTP interception overhead
- UI thread blocking
- Memory leaks in services

**Logging points**: Each layer implements appropriate logging for debugging and monitoring

**How to debug architecture**:
1. Check dependency injection registration
2. Verify layer boundaries are respected
3. Test each layer independently
4. Monitor cross-layer communication
5. Validate architectural principles

## Cross-References

**Related Documentation**:
- Layer-specific documentation (Presentation, Application, Domain, Infrastructure, Library)
- Component documentation for major classes and interfaces
- Setup and configuration guides
- Testing strategies for each layer

**Upstream Dependencies**:
- .NET 8.0 framework
- Avalonia UI framework
- Entity Framework Core
- External libraries and dependencies

**Downstream Dependencies**:
- Business requirements and use cases
- User interface specifications
- Database schema definitions
- HTTP protocol requirements

**Documents to read before/after**:
- Before: Technology stack documentation
- After: Layer-specific documentation
- After: Component implementation details

## Knowledge Transfer Notes

**Reusable concepts**:
- Clean architecture implementation
- MVVM pattern with modern frameworks
- Repository and Unit of Work patterns
- Dependency injection configuration
- Async/await patterns throughout application
- HTTP interception and processing pipelines

**Project-specific elements**:
- Snitcher workspace management domain
- HTTP traffic interception capabilities
- SQLite database integration
- Avalonia UI desktop implementation
- Cross-cutting concern handling through interceptors

**How to recreate architecture elsewhere**:
1. Define clear layer boundaries and responsibilities
2. Implement dependency inversion with interfaces
3. Use dependency injection for loose coupling
4. Apply appropriate design patterns for each concern
5. Maintain separation of concerns across layers
6. Implement comprehensive error handling and logging
7. Use async/await for responsive operations
8. Design for testability at each layer

**Key insights**:
- Architecture should enable independent development and testing
- Dependencies should point inward toward the domain
- Each layer should have a single, clear responsibility
- Cross-cutting concerns should be handled through patterns
- Interface-based design enables testability and flexibility
- Async operations are essential for responsive applications
- Proper error handling and logging are crucial for maintainability
- Architecture should support both current requirements and future growth
