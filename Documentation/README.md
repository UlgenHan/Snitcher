# Snitcher Project Documentation

## Overview

This documentation provides comprehensive technical documentation for the Snitcher project, a network monitoring and debugging application built with .NET 8.0 and Avalonia UI. The documentation covers all architectural layers, from core domain models to the desktop user interface, including testing utilities and development tools.

## Project Architecture

Snitcher follows a clean architecture pattern with clear separation of concerns across multiple layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   UI Desktop    │  │   ViewModels    │  │   Services   │ │
│  │   (Avalonia)    │  │   (MVVM)        │  │   (Bridge)   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │     Core        │  │   Repository    │  │   Service    │ │
│  │  (Domain)       │  │ (Data Access)   │  │ (Business)   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
┌─────────────────────────────────────────────────────────────┐
│                    Library Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │    Sniffer      │  │   HTTP Core     │  │ Certificates │ │
│  │ (Proxy Engine)  │  │ (Protocol)      │  │ (HTTPS)      │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Documentation Structure

### Application Layer (`/Application`)

**Core Domain (`/Application/Snitcher.Core`)**
- [`BaseEntity.md`](Application/Snitcher.Core/BaseEntity.md) - Foundation entity with audit trails and soft delete
- [`Entities/Workspace.md`](Application/Snitcher.Core/Entities/Workspace.md) - Workspace organization entity
- [`Entities/Project.md`](Application/Snitcher.Core/Entities/Project.md) - Project management entity
- [`Interfaces/IRepository.md`](Application/Snitcher.Core/Interfaces/IRepository.md) - Generic repository interface

**Repository Infrastructure (`/Application/Snitcher.Repository`)**
- [`Contexts/SnitcherDbContext.md`](Application/Snitcher.Repository/Contexts/SnitcherDbContext.md) - Entity Framework database context

**Service Layer (`/Application/Snitcher.Service`)**
- [`Services/WorkspaceService.md`](Application/Snitcher.Service/Services/WorkspaceService.md) - Workspace business logic
- [`Configuration/SnitcherConfiguration.md`](Application/Snitcher.Service/Configuration/SnitcherConfiguration.md) - Dependency injection and configuration

### Library Layer (`/Library`)

**Sniffer Network Engine (`/Library/Snitcher.Sniffer`)**
- [`Core/Services/ProxyServer.md`](Library/Snitcher.Sniffer/Core/Services/ProxyServer.md) - Core HTTP/HTTPS proxy server
- [`Core/Models/Flow.md`](Library/Snitcher.Sniffer/Core/Models/Flow.md) - Network transaction data model

### Presentation Layer (`/Presentation`)

**Desktop UI (`/Presentation/Snitcher.UI.Desktop`)**
- [`App.axaml.md`](Presentation/Snitcher.UI.Desktop/App.axaml.md) - Application bootstrap and DI setup
- [`Services/Database/DatabaseIntegrationService.md`](Presentation/Snitcher.UI.Desktop/Services/Database/DatabaseIntegrationService.md) - UI service bridge

### Testing Layer (`/Tests`)

**Unit Tests (`/Tests/Snitcher.Test.Library.Sniffer`)**
- [`Services/ProxyServiceTests.md`](Tests/Snitcher.Test.Library.Sniffer/Services/ProxyServiceTests.md) - Proxy service unit tests

### Tools Layer (`/Tools`)

**Development Utilities (`/Tools/MinimalProxy`)**
- [`Program.md`](Tools/MinimalProxy/Program.md) - Minimal HTTPS proxy for testing

## Technology Stack

### Core Technologies
- **.NET 8.0** - Latest .NET framework with modern C# 12.0 features
- **Entity Framework Core 8.0** - ORM for data persistence with SQLite
- **Microsoft.Extensions.DependencyInjection** - Dependency injection container
- **Microsoft.Extensions.Logging** - Structured logging framework

### User Interface
- **Avalonia UI 11.0** - Cross-platform desktop UI framework
- **MVVM Pattern** - Model-View-ViewModel architecture
- **ReactiveUI** - Reactive programming for UI interactions

### Network & Infrastructure
- **System.Net.Sockets** - Low-level TCP networking
- **HTTP/HTTPS Protocols** - Network traffic interception
- **Certificate Management** - HTTPS decryption support
- **SQLite Database** - Local data persistence

### Testing & Development
- **NUnit 3.0** - Unit testing framework
- **Moq 4.20** - Mocking framework for unit tests
- **Microsoft.Extensions.Hosting** - Application hosting and configuration

## Key Features

### Network Monitoring
- HTTP/HTTPS traffic interception and analysis
- Real-time flow capture and visualization
- Certificate management for HTTPS decryption
- Configurable proxy server with multiple endpoints

### Project Management
- Workspace-based project organization
- Project metadata and version tracking
- Analysis history and timestamp management
- Default workspace management

### User Interface
- Modern cross-platform desktop application
- Responsive MVVM-based architecture
- Real-time data binding and updates
- Comprehensive error handling and logging

### Data Persistence
- Entity Framework Core with SQLite
- Automatic database migrations
- Soft delete support for data recovery
- Audit trail with timestamp tracking

## Documentation Standards

Each component documentation follows a comprehensive 13-section structure:

1. **Overview** - Purpose, rationale, and impact analysis
2. **Tech Stack Identification** - Complete technology inventory
3. **Architectural Role** - Layer placement and responsibilities
4. **Execution Flow** - Detailed call sequences and lifecycle
5. **Public API** - Complete surface area documentation
6. **Internal Logic** - Line-by-line code analysis
7. **Patterns & Principles** - Design patterns and trade-offs
8. **Binding & Configuration** - Setup and wiring information
9. **Example Usage** - Critical examples with best practices
10. **Extension Guide** - Safe modification points and pitfalls
11. **Failure Modes** - Debugging and error handling guidance
12. **Cross-References** - Related component documentation
13. **Knowledge Transfer** - Reusable concepts and recreation guidance

## Getting Started

### Prerequisites
- .NET 8.0 SDK or runtime
- Visual Studio 2022 or compatible IDE
- Git for source control

### Building the Project
```bash
# Clone the repository
git clone <repository-url>
cd Snitcher

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project src/Presentation/Snitcher.UI.Desktop
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/Tests/Snitcher.Test.Library.Sniffer
```

### Development Tools
```bash
# Run minimal proxy for testing
dotnet run --project src/Tools/MinimalProxy

# Test proxy functionality
curl -x http://localhost:8080 --insecure https://httpbin.org/get
```

## Architecture Decisions

### Clean Architecture
The project follows clean architecture principles with clear dependency direction from outer layers (UI) to inner layers (domain). This ensures:
- Testability through dependency injection
- Maintainability through separation of concerns
- Flexibility through interface-based design

### MVVM Pattern
The UI layer implements the MVVM pattern to provide:
- Separation of UI from business logic
- Testable view models with mockable dependencies
- Data binding for reactive UI updates
- Command pattern for user interactions

### Repository Pattern
Data access is abstracted through the repository pattern to enable:
- Swappable data storage implementations
- Unit testing with in-memory databases
- Centralized query logic and optimization
- Consistent data access patterns

### Event-Driven Architecture
Network monitoring uses event-driven patterns for:
- Real-time flow capture notifications
- Loose coupling between components
- Scalable processing of network traffic
- Extensible monitoring capabilities

## Contributing Guidelines

### Code Standards
- Follow C# 12.0 coding conventions
- Use async/await for I/O operations
- Implement comprehensive error handling
- Include XML documentation for public APIs
- Write unit tests for all business logic

### Documentation Requirements
- Document all new components using the 13-section standard
- Update cross-references when adding new components
- Include example usage with both correct and incorrect patterns
- Provide extension guidance and common pitfalls

### Testing Requirements
- Write unit tests for all business logic
- Use mocking frameworks for external dependencies
- Test both positive and negative scenarios
- Include integration tests for critical paths
- Maintain test coverage above 80%

## Support and Troubleshooting

### Common Issues
- **Port conflicts**: Ensure ports 8080 and database paths are available
- **Certificate errors**: Install trusted certificates for HTTPS interception
- **Database issues**: Check file permissions for SQLite database
- **Network problems**: Verify firewall settings and network connectivity

### Debugging Resources
- Enable debug logging in configuration
- Use minimal proxy tool for isolated testing
- Monitor console output for connection logs
- Check database initialization logs
- Use network tools for traffic analysis

### Performance Considerations
- Monitor memory usage with large flow captures
- Optimize database queries for large datasets
- Consider connection pooling for high-load scenarios
- Profile UI responsiveness with real-time updates

## License and Credits

This documentation and the associated codebase are part of the Snitcher project. See the main project repository for license information and contributor credits.

---

*This documentation is maintained as part of the Snitcher project and updated with each major release. For the most current information, please refer to the main project repository.*
