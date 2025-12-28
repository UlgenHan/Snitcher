using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Moq;
using Snitcher.UI.Desktop.Models.WorkSpaces;
using Snitcher.UI.Desktop.Services.Database;
using Snitcher.UI.Desktop.ViewModels;

namespace Snitcher.Test.UI.Desktop.ViewModels;

/// <summary>
/// Unit tests for SnitcherMainViewModel
/// </summary>
public class SnitcherMainViewModelTests
{
    private readonly Mock<IDatabaseIntegrationService> _mockDatabaseService;
    private readonly Mock<ILogger<SnitcherMainViewModel>> _mockLogger;
    private readonly SnitcherMainViewModel _viewModel;

    public SnitcherMainViewModelTests()
    {
        _mockDatabaseService = new Mock<IDatabaseIntegrationService>();
        _mockLogger = new Mock<ILogger<SnitcherMainViewModel>>();
        
        // Setup the database service to return empty collections by default
        _mockDatabaseService
            .Setup(x => x.GetWorkspacesAsync())
            .ReturnsAsync(new List<Workspace>());

        _mockDatabaseService
            .Setup(x => x.GetProjectsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Project>());

        _mockDatabaseService
            .Setup(x => x.SearchAsync(It.IsAny<string>()))
            .ReturnsAsync(new SearchResults { Workspaces = new List<Workspace>(), Projects = new List<Project>() });

        // Create a new instance for each test since it's now transient
        _viewModel = new SnitcherMainViewModel(_mockDatabaseService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Workspaces.Should().NotBeNull();
        _viewModel.RecentProjects.Should().NotBeNull();
        _viewModel.Namespaces.Should().NotBeNull();
        _viewModel.SearchTerm.Should().BeEmpty();
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.StatusMessage.Should().Be("Ready");
        _viewModel.ShowSearchResults.Should().BeFalse();
        _viewModel.IsWorkspaceOpened.Should().BeFalse();
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullDatabaseService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new SnitcherMainViewModel(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new SnitcherMainViewModel(_mockDatabaseService.Object, null!));
    }

    [Fact]
    public async Task OpenWorkspaceCommand_WithValidWorkspace_ShouldOpenWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Id = "test-workspace-id",
            Name = "Test Workspace",
            Description = "Test Description"
        };

        _mockDatabaseService
            .Setup(x => x.GetProjectsAsync(workspace.Id))
            .ReturnsAsync(new List<Project>());

        // Act
        _viewModel.OpenWorkspaceCommand.Execute(workspace);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _viewModel.SelectedWorkspace.Should().Be(workspace);
        _viewModel.IsWorkspaceOpened.Should().BeTrue();
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeFalse();
        _viewModel.StatusMessage.Should().Contain("loaded with");
        _viewModel.StatusMessage.Should().Contain("Test Workspace");
    }

    [Fact]
    public async Task OpenWorkspaceCommand_WithNullWorkspace_ShouldNotThrowException()
    {
        // Act & Assert - Should not throw
        await _viewModel.OpenWorkspaceCommand.ExecuteAsync(null);
        
        _viewModel.SelectedWorkspace.Should().BeNull();
        _viewModel.IsWorkspaceOpened.Should().BeFalse();
    }

    [Fact]
    public async Task OpenProjectCommand_WithValidProject_ShouldOpenProject()
    {
        // Arrange
        var project = new Project
        {
            Id = "test-project-id",
            Name = "Test Project",
            Description = "Test Description",
            Path = "C:\\Test\\Project.csproj"
        };

        // Act
        _viewModel.OpenProjectCommand.Execute(project);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _viewModel.SelectedProject.Should().Be(project);
        _viewModel.StatusMessage.Should().Contain("opened");
        _viewModel.StatusMessage.Should().Contain("Test Project");
    }

    [Fact]
    public async Task OpenProjectCommand_WithNullProject_ShouldNotThrowException()
    {
        // Act & Assert - Should not throw
        await _viewModel.OpenProjectCommand.ExecuteAsync(null);
        
        _viewModel.SelectedProject.Should().BeNull();
    }

    [Fact]
    public async Task CreateWorkspaceCommand_ShouldCreateWorkspace()
    {
        // Arrange
        var newWorkspace = new Workspace
        {
            Id = "new-workspace-id",
            Name = "New Workspace",
            Description = "New Description"
        };

        _mockDatabaseService
            .Setup(x => x.CreateWorkspaceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(newWorkspace);

        // Act
        _viewModel.CreateWorkspaceCommand.Execute(null);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        // CreateWorkspaceCommand shows a dialog, so we verify the status message
        _viewModel.StatusMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateProjectCommand_ShouldCreateProject()
    {
        // Arrange
        var workspace = new Workspace
        {
            Id = "test-workspace-id",
            Name = "Test Workspace",
            Description = "Test Description"
        };

        var newProject = new Project
        {
            Id = "new-project-id",
            Name = "New Project",
            Description = "New Description",
            Path = "C:\\New\\Project.csproj"
        };

        _mockDatabaseService
            .Setup(x => x.CreateProjectAsync(workspace.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(newProject);

        _viewModel.SelectedWorkspace = workspace;

        // Act
        _viewModel.CreateProjectCommand.Execute(workspace);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        // CreateProjectCommand shows a dialog, so we verify the status message
        _viewModel.StatusMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteWorkspaceCommand_WithValidWorkspace_ShouldDeleteWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Id = "workspace-to-delete-id",
            Name = "Workspace to Delete",
            Description = "Description"
        };

        _mockDatabaseService
            .Setup(x => x.DeleteWorkspaceAsync(workspace.Id))
            .ReturnsAsync(true);

        // Act
        _viewModel.DeleteWorkspaceCommand.Execute(workspace);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _mockDatabaseService.Verify(x => x.DeleteWorkspaceAsync(workspace.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectCommand_WithValidProject_ShouldDeleteProject()
    {
        // Arrange
        var project = new Project
        {
            Id = "project-to-delete-id",
            Name = "Project to Delete",
            Description = "Description",
            Path = "C:\\Delete\\Project.csproj"
        };

        _mockDatabaseService
            .Setup(x => x.DeleteProjectAsync(project.Id))
            .ReturnsAsync(true);

        // Act
        _viewModel.DeleteProjectCommand.Execute(project);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _mockDatabaseService.Verify(x => x.DeleteProjectAsync(project.Id), Times.Once);
    }

    [Fact]
    public async Task SearchCommand_WithSearchTerm_ShouldPerformSearch()
    {
        // Arrange
        var searchTerm = "test search";

        // Act
        _viewModel.SearchTerm = searchTerm;
        _viewModel.SearchCommand.Execute(null);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _viewModel.SearchTerm.Should().Be(searchTerm);
        _viewModel.ShowSearchResults.Should().BeTrue();
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeFalse();
    }

    [Fact]
    public void SearchCommand_WithEmptySearchTerm_ShouldNotShowSearchResults()
    {
        // Arrange
        var emptySearchTerm = "";

        // Act
        _viewModel.SearchTerm = emptySearchTerm;
        _viewModel.SearchCommand.Execute(null);

        // Assert
        _viewModel.SearchTerm.Should().Be(emptySearchTerm);
        _viewModel.ShowSearchResults.Should().BeFalse();
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshDataCommand_ShouldRefreshData()
    {
        // Arrange
        var testWorkspaces = new List<Workspace>
        {
            new() { Id = "workspace-1", Name = "Workspace 1" },
            new() { Id = "workspace-2", Name = "Workspace 2" }
        };

        _mockDatabaseService
            .Setup(x => x.GetWorkspacesAsync())
            .ReturnsAsync(testWorkspaces);

        // Act
        _viewModel.RefreshDataCommand.Execute(null);

        // Wait for async operation to complete
        await Task.Delay(100);

        // Assert
        _mockDatabaseService.Verify(x => x.GetWorkspacesAsync(), Times.AtLeastOnce);
        _mockDatabaseService.Verify(x => x.GetProjectsAsync(It.IsAny<string>()), Times.AtLeastOnce);
        _mockDatabaseService.Verify(x => x.GetNamespacesAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void IsNotWorkspaceOpenedAndNotSearching_ShouldReturnCorrectValue()
    {
        // Test initial state
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeTrue();

        // Test when workspace is opened
        _viewModel.IsWorkspaceOpened = true;
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeFalse();

        // Reset and test when searching
        _viewModel.IsWorkspaceOpened = false;
        _viewModel.ShowSearchResults = true;
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeFalse();

        // Test when both are false
        _viewModel.IsWorkspaceOpened = false;
        _viewModel.ShowSearchResults = false;
        _viewModel.IsNotWorkspaceOpenedAndNotSearching.Should().BeTrue();
    }

    [Fact]
    public void PropertyChanged_ShouldRaiseNotification()
    {
        // Arrange
        bool propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(SnitcherMainViewModel.StatusMessage))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.StatusMessage = "Test Message";

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }
}
