# Workspace.cs (UI Model)

## Overview

`Workspace.cs` is a UI model representing a workspace in the Snitcher application. This class implements the ObservableObject pattern from CommunityToolkit.Mvvm to provide reactive property change notifications for UI binding. It serves as the primary data container for workspace information in the user interface, including both persistent data and UI-specific state.

**Why it exists**: To provide a clean, bindable data model for workspace entities that supports both persistent properties and UI-specific state management while enabling reactive UI updates through the observer pattern.

**What problem it solves**: Eliminates the need for manual UI update code, provides a centralized location for workspace-related data and UI state, and enables proper data binding in Avalonia UI through property change notifications.

**What would break if removed**: The UI would have no bindable workspace representation, requiring manual UI updates and breaking the MVVM pattern's data binding capabilities.

## Tech Stack Identification

**Languages**: C# 12.0 (.NET 8.0)

**Frameworks**:
- .NET 8.0
- CommunityToolkit.Mvvm 8.2.1

**Libraries**:
- System.Collections.ObjectModel (ObservableCollection)
- System (DateTime operations)

**UI Framework**: Avalonia UI (via MVVM binding)

**Persistence**: UI model only - persistence handled by database layer

**Build Tools**: MSBuild with .NET SDK 8.0

**Runtime Assumptions**: UI binding framework with INotifyPropertyChanged support

## Architectural Role

**Layer**: Presentation Layer (UI Model)

**Responsibility Boundaries**:
- MUST hold workspace data for UI display
- MUST provide property change notifications
- MUST NOT implement business logic
- MUST NOT access database directly
- MUST NOT handle validation rules

**What it MUST do**:
- Store workspace properties (name, description, dates)
- Maintain UI state (selection, loading)
- Provide computed display properties
- Support property change notifications
- Enable collection management for child objects

**What it MUST NOT do**:
- Implement data persistence logic
- Perform business validations
- Handle user interactions directly
- Access services or databases

**Dependencies (Incoming)**: ViewModels (create and manipulate instances)

**Dependencies (Outgoing**: CommunityToolkit.Mvvm (ObservableObject base)

## Execution Flow

**Where execution starts**: Created by DatabaseIntegrationService when mapping database entities to UI models

**How control reaches this component**:
1. Database service loads workspace entity from database
2. Mapping function creates Workspace UI model
3. Properties populated from entity data
4. Model added to observable collection in ViewModel
5. UI binds to model properties

**Property Change Flow**:
1. UI modifies property (via binding or direct assignment)
2. [ObservableProperty] generates SetProperty call
3. Property change notification sent
4. UI updates automatically
5. Computed properties may recalculate

**Synchronous vs asynchronous behavior**: All operations are synchronous - this is a pure data model

**Threading/Dispatcher notes**: Property changes automatically marshal to UI thread by ObservableProperty attribute

**Lifecycle**: Created by service → Lives in ViewModel collections → Garbage collected when removed

## Public API / Surface Area

**Inheritance**: `partial class Workspace : ObservableObject`

**Persistent Properties** (Database-backed):
- `string Id` - Unique identifier
- `string Name` - Workspace display name
- `string Description` - Workspace description
- `DateTime CreatedAt` - Creation timestamp
- `DateTime UpdatedAt` - Last update timestamp
- `bool IsDefault` - Default workspace flag

**UI State Properties**:
- `bool IsSelected` - UI selection state
- `bool IsLoading` - Loading operation state

**Collection Properties**:
- `ObservableCollection<Project> Projects` - Child projects
- `ObservableCollection<Namespace> Namespaces` - Child namespaces (deprecated)

**Computed Properties** (Read-only):
- `string DisplayName` - Safe display name with fallback
- `string ProjectCountText` - Formatted project count
- `string UpdatedAtFormatted` - Human-readable date
- `string UpdatedAtRelative` - Relative time description

**Utility Methods**:
- `Workspace Clone()` - Create copy for editing
- `void UpdateFrom(Workspace other)` - Update from another instance

**Expected Input/Output**: Properties store data, methods manipulate state, computed properties provide formatted display values.

**Side Effects**:
- Property changes trigger UI updates via INotifyPropertyChanged
- Collection modifications affect bound UI elements
- Clone creates new independent instance
- UpdateFrom modifies current instance properties

**Error Behavior**: No explicit error handling - relies on property type validation and null checks in computed properties.

## Internal Logic Breakdown

**Observable Property Pattern**:
```csharp
[ObservableProperty]
private string _name = "";

// Generated by source generator:
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}
```

**Computed Property Implementation** (lines 48-54):
```csharp
public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unnamed Workspace" : Name;
```
- Provides safe fallback for empty names
- Used in UI binding to avoid displaying empty strings

**Relative Time Calculation** (lines 63-77):
```csharp
public string UpdatedAtRelative
{
    get
    {
        var timeSince = DateTime.Now - UpdatedAt;
        if (timeSince.TotalDays < 1)
            return "Updated today";
        else if (timeSince.TotalDays < 7)
            return $"Updated {timeSince.Days} day{(timeSince.Days != 1 ? "s" : "")} ago";
        // ... more time calculations
    }
}
```
- Provides human-readable relative timestamps
- Handles singular/plural formatting correctly
- Falls back to absolute date for older items

**Clone Implementation** (lines 83-96):
```csharp
public Workspace Clone()
{
    return new Workspace
    {
        Id = Id,
        Name = Name,
        // ... copy all properties
        Projects = new ObservableCollection<Project>(Projects),
        Namespaces = new ObservableCollection<Namespace>(Namespaces)
    };
}
```
- Creates deep copy of workspace
- Copies collections to new instances
- Used for edit scenarios to avoid modifying original

**UpdateFrom Implementation** (lines 102-123):
```csharp
public void UpdateFrom(Workspace other)
{
    if (other == null) return;
    
    Name = other.Name;
    Description = other.Description;
    UpdatedAt = other.UpdatedAt;
    
    // Update collections
    Projects.Clear();
    foreach (var project in other.Projects)
    {
        Projects.Add(project);
    }
}
```
- Updates properties from source instance
- Replaces collections entirely (not merge)
- Used for applying changes from edited copy

## Patterns & Principles Used

**Observer Pattern**: Implements INotifyPropertyChanged via ObservableObject for reactive UI updates

**Data Transfer Object Pattern**: Carries data between layers (database to UI)

**Value Object Pattern**: Immutable identity through Id property

**Clone Pattern**: Supports copy-on-write semantics for editing

**Why these patterns were chosen**:
- Observer for automatic UI synchronization
- DTO for clean data transfer between layers
- Value object for identity and equality
- Clone for safe editing operations

**Trade-offs**:
- Large class with multiple responsibilities
- Collection management complexity
- Memory overhead from observable collections

**Anti-patterns avoided**:
- No business logic in model
- No service dependencies
- No direct database access
- No validation logic

## Binding / Wiring / Configuration

**Data Binding**:
- All [ObservableProperty] members bindable in XAML
- Computed properties bindable as read-only
- Collections support full binding scenarios

**Configuration Sources**:
- No external configuration needed
- Behavior driven by property assignments

**Runtime Wiring**:
- Created by DatabaseIntegrationService mapping
- Properties populated from database entities
- Added to ViewModel collections for binding

**Registration Points**:
- No registration needed - pure data class
- Used by any code that needs workspace representation

## Example Usage

**Minimal Example**:
```csharp
// Create workspace
var workspace = new Workspace
{
    Name = "My Workspace",
    Description = "Description"
};

// Bind in XAML
<TextBlock Text="{Binding Name}" />
<TextBlock Text="{Binding DisplayName}" />
```

**Realistic Example**:
```csharp
// Edit workflow
var original = workspace;
var editCopy = original.Clone();
editCopy.Name = "New Name";
// ... user edits
if (userConfirmed)
{
    original.UpdateFrom(editCopy);
}
```

**Incorrect Usage Example**:
```csharp
// BAD - Don't modify collections directly in loops
foreach (var project in workspace.Projects)
{
    workspace.Projects.Remove(project); // Will throw exception
}

// BAD - Don't access database from model
var db = new SnitcherDbContext();
```

**How to test in isolation**:
```csharp
var workspace = new Workspace { Name = "Test" };

// Test property changes
workspace.Name = "Updated";
Assert.Equal("Updated", workspace.Name);

// Test computed properties
Assert.Equal("Test", workspace.DisplayName);
```

**How to mock or replace**:
- Create test instances directly
- Use factory methods for test data
- Mock only if needed for interfaces

## Extension & Modification Guide

**How to add new property**:
1. Add private backing field with [ObservableProperty]
2. Property automatically generated by source generator
3. Update Clone() method to copy new property
4. Update UpdateFrom() method to transfer new property

**Where NOT to add logic**:
- Don't add business validation
- Don't add service calls
- Don't add database operations
- Don't add complex calculations

**Safe extension points**:
- New observable properties for UI state
- Additional computed properties for display
- Helper methods for common operations
- Validation helpers (if needed)

**Common mistakes**:
- Forgetting to update Clone() method
- Not handling null values in computed properties
- Modifying collections while iterating
- Adding business logic to model

**Refactoring warnings**:
- Large class may benefit from splitting
- Collection management could be extracted
- Computed properties may become complex
- Consider immutable pattern for some properties

## Failure Modes & Debugging

**Common runtime errors**:
- InvalidOperationException when modifying collections during enumeration
- NullReferenceException in computed properties if null checks missing
- ArgumentException from property setters if validation added later

**Null/reference risks**:
- Projects collection can be null initially - initialized in constructor
- Namespace collection deprecated but still present
- String properties can be null - handled by computed properties

**Performance risks**:
- Large project collections can slow UI updates
- Frequent property changes can cause excessive UI updates
- Memory usage grows with large collections

**Logging points**: None in model - logging handled by calling code

**How to debug step-by-step**:
1. Set property breakpoints to monitor changes
2. Use UI debugging tools to verify binding
3. Check collection state before modifications
4. Verify computed property calculations
5. Test clone and update operations separately

## Cross-References

**Related classes**:
- `Project` - Child collection items
- `Namespace` - Deprecated child items
- `DatabaseIntegrationService` - Creates instances
- `SnitcherMainViewModel` - Uses instances

**Upstream callers**:
- `DatabaseIntegrationService` (mapping)
- `SnitcherMainViewModel` (manipulation)
- Dialog classes (creation)

**Downstream dependencies**:
- `ObservableObject` (base class)
- `Project` (collection items)
- `Namespace` (collection items)

**Documents to read before/after**:
- Before: `DatabaseIntegrationService.cs` (creation)
- After: `Project.cs` (child items)
- After: `SnitcherMainViewModel.cs` (usage)

## Knowledge Transfer Notes

**Reusable concepts**:
- ObservableObject pattern for reactive UI
- Computed properties for formatted display
- Clone pattern for safe editing
- Collection management in UI models
- Relative time calculation patterns

**Project-specific elements**:
- Snitcher workspace domain model
- Project and namespace relationships
- Default workspace concept
- UI state management (selection, loading)

**How to recreate pattern elsewhere**:
1. Inherit from ObservableObject
2. Use [ObservableProperty] for bindable properties
3. Create computed properties for display formatting
4. Implement Clone() for edit scenarios
5. Add UpdateFrom() for applying changes
6. Handle collection initialization in constructor

**Key insights**:
- Keep models focused on data representation
- Use computed properties for UI-specific formatting
- Provide both absolute and relative time displays
- Implement proper collection management
- Support edit workflows with clone/apply pattern
- Always handle null values in computed properties
- Consider performance with large collections
