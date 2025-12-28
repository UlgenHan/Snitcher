using Microsoft.Extensions.Logging;
using Moq;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Services;

namespace Snitcher.Test.Application.Service.Services;

/// <summary>
/// Unit tests for ProjectService
/// </summary>
public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ProjectService>> _mockLogger;
    private readonly ProjectService _projectService;

    public ProjectServiceTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ProjectService>>();
        
        _projectService = new ProjectService(
            _mockProjectRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateProjectAsync_WithValidData_ShouldCreateProject()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Test Project";
        var projectDescription = "Test Description";
        var projectPath = "C:\\Test\\Project";
        var projectVersion = "1.0.0";
        var expectedProject = new ProjectEntity
        {
            WorkspaceId = workspaceId,
            Name = projectName,
            Description = projectDescription,
            Path = projectPath,
            Version = projectVersion
        };

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.ExistsByPathAsync(workspaceId, projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProject);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _projectService.CreateProjectAsync(workspaceId, projectName, projectDescription, projectPath, projectVersion);

        // Assert
        result.Should().NotBeNull();
        result.WorkspaceId.Should().Be(workspaceId);
        result.Name.Should().Be(projectName);
        result.Description.Should().Be(projectDescription);
        result.Path.Should().Be(projectPath);
        result.Version.Should().Be(projectVersion);

        _mockProjectRepository.Verify(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()), Times.Once);
        _mockProjectRepository.Verify(x => x.ExistsByPathAsync(workspaceId, projectPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockProjectRepository.Verify(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.CreateProjectAsync(workspaceId, ""));

        exception.Message.Should().Contain("Project name is required");
        exception.ParamName.Should().Be("name");
    }

    [Fact]
    public async Task CreateProjectAsync_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.CreateProjectAsync(workspaceId, null!));

        exception.Message.Should().Contain("Project name is required");
        exception.ParamName.Should().Be("name");
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmptyWorkspaceId_ShouldThrowArgumentException()
    {
        // Arrange
        var workspaceId = Guid.Empty;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.CreateProjectAsync(workspaceId, "Test Project"));

        exception.Message.Should().Contain("Workspace ID is required");
        exception.ParamName.Should().Be("workspaceId");
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Duplicate Project";

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _projectService.CreateProjectAsync(workspaceId, projectName));

        exception.Message.Should().Contain("already exists in this workspace");

        _mockProjectRepository.Verify(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()), Times.Once);
        _mockProjectRepository.Verify(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProjectAsync_WithDuplicatePath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Test Project";
        var projectPath = "C:\\Duplicate\\Path";

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.ExistsByPathAsync(workspaceId, projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _projectService.CreateProjectAsync(workspaceId, projectName, path: projectPath));

        exception.Message.Should().Contain("already exists in this workspace");

        _mockProjectRepository.Verify(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()), Times.Once);
        _mockProjectRepository.Verify(x => x.ExistsByPathAsync(workspaceId, projectPath, It.IsAny<CancellationToken>()), Times.Once);
        _mockProjectRepository.Verify(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProjectAsync_WithWhitespaceName_ShouldTrimName()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "  Test Project  ";
        var trimmedName = projectName.Trim();

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, trimmedName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.ExistsByPathAsync(workspaceId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var createdProject = new ProjectEntity { Name = trimmedName };
        _mockProjectRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProject);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _projectService.CreateProjectAsync(workspaceId, projectName);

        // Assert
        result.Name.Should().Be(trimmedName);
    }

    [Fact]
    public async Task CreateProjectAsync_WithWhitespaceDescription_ShouldTrimDescription()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Test Project";
        var projectDescription = "  Test Description  ";
        var trimmedDescription = projectDescription.Trim();

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.ExistsByPathAsync(workspaceId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var createdProject = new ProjectEntity { Description = trimmedDescription };
        _mockProjectRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProject);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _projectService.CreateProjectAsync(workspaceId, projectName, projectDescription);

        // Assert
        result.Description.Should().Be(trimmedDescription);
    }

    [Fact]
    public void Constructor_WithNullProjectRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ProjectService(null!, _mockUnitOfWork.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ProjectService(_mockProjectRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ProjectService(_mockProjectRepository.Object, _mockUnitOfWork.Object, null!));
    }

    [Fact]
    public async Task CreateProjectAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Test Project";
        var expectedException = new InvalidOperationException("Database error");

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _projectService.CreateProjectAsync(workspaceId, projectName));

        exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task CreateProjectAsync_WhenUnitOfWorkFails_ShouldPropagateException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var projectName = "Test Project";

        _mockProjectRepository
            .Setup(x => x.ExistsByNameAsync(workspaceId, projectName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.ExistsByPathAsync(workspaceId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockProjectRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectEntity());

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _projectService.CreateProjectAsync(workspaceId, projectName));
    }
}
