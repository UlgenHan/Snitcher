# Snitcher UI Desktop - Project Structure

This document outlines the reorganized folder structure for the Snitcher UI Desktop application, following domain-driven design principles and clean architecture patterns.

## 📁 Folder Structure

```
Snitcher.UI.Desktop/
├── 📁 Configuration/           # Application configuration and settings
│   └── UIConfiguration.cs      # Centralized configuration with feature flags
├── 📁 Dialogs/                 # Modal dialogs and popups
│   ├── CreateNamespaceDialog.axaml
│   ├── CreateProjectDialog.axaml
│   └── CreateWorkspaceDialog.axaml
├── 📁 Domains/                 # Domain-specific feature modules
│   ├── 📁 Proxy/               # HTTP/HTTPS proxy inspection
│   │   ├── ProxyInspectorViewModel.cs
│   │   ├── ProxyInspectorView.axaml
│   │   ├── ProxyService.cs
│   │   └── FlowMapper.cs
│   ├── 📁 RequestBuilder/      # HTTP request builder and testing
│   │   ├── RequestBuilderViewModel.cs
│   │   ├── RequestBuilderView.axaml
│   │   └── RequestSender.cs
│   ├── 📁 Automation/          # Workflow automation
│   │   ├── AutomationWorkflowViewModel.cs
│   │   └── AutomationWorkflowView.axaml
│   ├── 📁 Collections/         # Request/response collections
│   │   ├── CollectionsExplorerViewModel.cs
│   │   └── CollectionsExplorerView.axaml
│   └── 📁 Workspace/           # Workspace management
│       ├── WorkspaceManagerViewModel.cs
│       ├── WorkspaceManagerView.axaml
│       └── (workspace services)
├── 📁 Models/                  # Shared data models
├── 📁 Services/                # Shared application services
├── 📁 ViewModels/              # Core view models
├── 📁 Views/                   # Core views
├── 📁 Assets/                  # Static assets
└── 📁 Themes/                  # UI themes and styles
```

## 🏗️ Architecture Principles

### **Domain Separation**
Each domain represents a distinct business capability:
- **Proxy**: HTTP/HTTPS traffic interception and inspection
- **RequestBuilder**: HTTP request construction and testing
- **Automation**: Workflow automation and scripting
- **Collections**: Request/response organization and management
- **Workspace**: Project and workspace management

### **Configuration-Driven Features**
The `UIConfiguration` class provides centralized feature flags:
```csharp
public static class Features
{
    public const bool EnableHttpsInterception = true;
    public const bool EnableRequestBuilder = true;
    public const bool EnableAutomation = true;
    public const bool EnableCollections = true;
    public const bool EnableWorkspaceManagement = true;
}
```

### **Dependency Injection**
Services are registered conditionally based on feature flags:
```csharp
if (UIConfiguration.Features.EnableHttpsInterception)
{
    services.AddSingleton<IProxyService, ProxyService>();
    services.AddTransient<ProxyInspectorViewModel>();
}
```

## 🔧 Benefits of This Structure

1. **Clear Separation of Concerns**: Each domain is self-contained
2. **Feature Toggling**: Features can be enabled/disabled via configuration
3. **Maintainability**: Easier to locate and modify domain-specific code
4. **Testability**: Each domain can be tested in isolation
5. **Scalability**: New domains can be added without affecting existing code
6. **Team Collaboration**: Different team members can work on different domains

## 📝 Namespace Conventions

- **ViewModels**: `Snitcher.UI.Desktop.Domains.{DomainName}`
- **Views**: Same folder as their corresponding ViewModels
- **Services**: `Snitcher.UI.Desktop.Domains.{DomainName}`
- **Configuration**: `Snitcher.UI.Desktop.Configuration`

## 🚀 Getting Started

1. **Enable/Disable Features**: Modify `UIConfiguration.Features`
2. **Add New Domain**: Create folder under `Domains/` following the pattern
3. **Register Services**: Add to `ConfigureServices()` in `App.axaml.cs`
4. **Update Namespaces**: Ensure all files use proper domain namespaces

## 📋 Migration Notes

- Dialogs moved from `Views/` to `Dialogs/`
- Domain-specific ViewModels moved to respective domain folders
- Domain-specific services moved to respective domain folders
- Namespaces updated to reflect new structure
- Feature-based service registration implemented

This structure provides a solid foundation for future development while maintaining clean separation of concerns and enabling feature-based development.
