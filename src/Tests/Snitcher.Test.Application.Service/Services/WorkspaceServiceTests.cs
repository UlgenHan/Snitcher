using Microsoft.Extensions.Logging;
using Moq;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Services;

namespace Snitcher.Test.Application.Service.Services;

/// <summary>
/// Unit tests for WorkspaceService
/// </summary>
public class WorkspaceServiceTests
{
    private readonly Mock<IWorkspaceRepository> _mockWorkspaceRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<WorkspaceService>> _mockLogger;
    private readonly WorkspaceService _workspaceService;

    public WorkspaceServiceTests()
    {
        _mockWorkspaceRepository = new Mock<IWorkspaceRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WorkspaceService>>();
        
        _workspaceService = new WorkspaceService(
            _mockWorkspaceRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithValidData_ShouldCreateWorkspace()
    {
        // Arrange
        var workspaceName = "Test Workspace";
        var workspaceDescription = "Test Description";
        var workspacePath = "C:\\Test\\Path";
        var expectedWorkspace = new Workspace
        {
            Name = workspaceName,
            Description = workspaceDescription,
            Path = workspacePath,
            IsDefault = false
        };

        _mockWorkspaceRepository
            .Setup(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        _mockWorkspaceRepository
            .Setup(x => x.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWorkspace);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _workspaceService.CreateWorkspaceAsync(workspaceName, workspaceDescription, workspacePath);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(workspaceName);
        result.Description.Should().Be(workspaceDescription);
        result.Path.Should().Be(workspacePath);
        result.IsDefault.Should().BeFalse();

        _mockWorkspaceRepository.Verify(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()), Times.Once);
        _mockWorkspaceRepository.Verify(x => x.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithEmptyPath_ShouldUseDefaultPath()
    {
        // Arrange
        var workspaceName = "Test Workspace";
        var expectedWorkspace = new Workspace
        {
            Name = workspaceName,
            Path = $"C:\\Snitcher\\Workspaces\\{workspaceName}",
            IsDefault = false
        };

        _mockWorkspaceRepository
            .Setup(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        _mockWorkspaceRepository
            .Setup(x => x.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWorkspace);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _workspaceService.CreateWorkspaceAsync(workspaceName);

        // Assert
        result.Should().NotBeNull();
        result.Path.Should().Contain(workspaceName);
        result.Path.Should().StartWith("C:\\Snitcher\\Workspaces");
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var workspaceName = "Existing Workspace";
        var existingWorkspace = new Workspace
        {
            Name = workspaceName,
            Description = "Existing Description",
            Path = "C:\\Existing\\Path",
            IsDefault = false
        };

        _mockWorkspaceRepository
            .Setup(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWorkspace);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workspaceService.CreateWorkspaceAsync(workspaceName));

        exception.Message.Should().Contain("already exists");
        
        _mockWorkspaceRepository.Verify(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()), Times.Once);
        _mockWorkspaceRepository.Verify(x => x.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WithNullName_ShouldThrowNullReferenceException()
    {
        // Arrange
        var workspacePath = "C:\\Test\\Path";

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _workspaceService.CreateWorkspaceAsync(null!, workspacePath));
    }

    [Fact]
    public async Task GetAllWorkspacesAsync_ShouldReturnAllWorkspaces()
    {
        // Arrange
        var expectedWorkspaces = new List<Workspace>
        {
            new() { Name = "Workspace 1", Description = "Description 1", Path = "C:\\Path1" },
            new() { Name = "Workspace 2", Description = "Description 2", Path = "C:\\Path2" },
            new() { Name = "Workspace 3", Description = "Description 3", Path = "C:\\Path3" }
        };

        _mockWorkspaceRepository
            .Setup(x => x.GetWithRelatedDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWorkspaces);

        // Act
        var result = await _workspaceService.GetAllWorkspacesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedWorkspaces);

        _mockWorkspaceRepository.Verify(x => x.GetWithRelatedDataAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllWorkspacesAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockWorkspaceRepository
            .Setup(x => x.GetWithRelatedDataAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workspaceService.GetAllWorkspacesAsync());

        exception.Should().Be(expectedException);
    }

    [Fact]
    public void Constructor_WithNullWorkspaceRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new WorkspaceService(null!, _mockUnitOfWork.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new WorkspaceService(_mockWorkspaceRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new WorkspaceService(_mockWorkspaceRepository.Object, _mockUnitOfWork.Object, null!));
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WhenRepositoryThrowsException_ShouldLogAndPropagateException()
    {
        // Arrange
        var workspaceName = "Test Workspace";
        var expectedException = new InvalidOperationException("Database error");

        _mockWorkspaceRepository
            .Setup(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workspaceService.CreateWorkspaceAsync(workspaceName));

        exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var workspaceName = "Test Workspace";
        var workspace = new Workspace { Name = workspaceName };

        _mockWorkspaceRepository
            .Setup(x => x.GetByNameAsync(workspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        _mockWorkspaceRepository
            .Setup(x => x.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workspaceService.CreateWorkspaceAsync(workspaceName));
    }
}
