using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;
using Snitcher.Repository.Repositories;

namespace Snitcher.Test.Application.Repository.Repositories;

/// <summary>
/// Integration tests for WorkspaceRepository using InMemory database
/// </summary>
public class WorkspaceRepositoryTests : IDisposable
{
    private readonly SnitcherDbContext _context;
    private readonly WorkspaceRepository _repository;
    private readonly Mock<ILogger<WorkspaceRepository>> _mockLogger;

    public WorkspaceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SnitcherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SnitcherDbContext(options);
        _mockLogger = new Mock<ILogger<WorkspaceRepository>>();
        _repository = new WorkspaceRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_WithExistingWorkspace_ShouldReturnWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            Description = "Test Description",
            Path = "C:\\Test\\Path",
            IsDefault = false
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Test Workspace");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workspace.Id);
        result.Name.Should().Be(workspace.Name);
        result.Description.Should().Be(workspace.Description);
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentWorkspace_ShouldReturnNull()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            Description = "Test Description",
            Path = "C:\\Test\\Path",
            IsDefault = false
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync("Non Existent Workspace");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultAsync_WithDefaultWorkspace_ShouldReturnDefaultWorkspace()
    {
        // Arrange
        var workspace1 = new Workspace
        {
            Name = "Workspace 1",
            Description = "Description 1",
            Path = "C:\\Path1",
            IsDefault = false
        };

        var workspace2 = new Workspace
        {
            Name = "Workspace 2",
            Description = "Description 2",
            Path = "C:\\Path2",
            IsDefault = true
        };

        _context.Workspaces.AddRange(workspace1, workspace2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
        result.Name.Should().Be("Workspace 2");
    }

    [Fact]
    public async Task GetDefaultAsync_WithNoDefaultWorkspace_ShouldReturnNull()
    {
        // Arrange
        var workspace1 = new Workspace
        {
            Name = "Workspace 1",
            Description = "Description 1",
            Path = "C:\\Path1",
            IsDefault = false
        };

        var workspace2 = new Workspace
        {
            Name = "Workspace 2",
            Description = "Description 2",
            Path = "C:\\Path2",
            IsDefault = false
        };

        _context.Workspaces.AddRange(workspace1, workspace2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetDefaultAsync_WithValidWorkspaceId_ShouldSetWorkspaceAsDefault()
    {
        // Arrange
        var workspace1 = new Workspace
        {
            Name = "Workspace 1",
            Description = "Description 1",
            Path = "C:\\Path1",
            IsDefault = true
        };

        var workspace2 = new Workspace
        {
            Name = "Workspace 2",
            Description = "Description 2",
            Path = "C:\\Path2",
            IsDefault = false
        };

        _context.Workspaces.AddRange(workspace1, workspace2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SetDefaultAsync(workspace2.Id);

        // Assert
        result.Should().BeTrue();

        // Verify the changes
        var updatedWorkspace1 = await _context.Workspaces.FindAsync(workspace1.Id);
        var updatedWorkspace2 = await _context.Workspaces.FindAsync(workspace2.Id);

        updatedWorkspace1!.IsDefault.Should().BeFalse();
        updatedWorkspace2!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAsync_WithNonExistentWorkspaceId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.SetDefaultAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithValidWorkspace_ShouldAddWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "New Workspace",
            Description = "New Description",
            Path = "C:\\New\\Path",
            IsDefault = false
        };

        // Act
        var result = await _repository.AddAsync(workspace);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(workspace.Name);

        // Verify it was added to database
        var savedWorkspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Name == workspace.Name);
        savedWorkspace.Should().NotBeNull();
        savedWorkspace!.Name.Should().Be(workspace.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkspace_ShouldReturnWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            Description = "Test Description",
            Path = "C:\\Test\\Path",
            IsDefault = false
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(workspace.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(workspace.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentWorkspace_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleWorkspaces_ShouldReturnAllWorkspaces()
    {
        // Arrange
        var workspaces = new List<Workspace>
        {
            new() { Name = "Workspace 1", Description = "Description 1", Path = "C:\\Path1", IsDefault = false },
            new() { Name = "Workspace 2", Description = "Description 2", Path = "C:\\Path2", IsDefault = false },
            new() { Name = "Workspace 3", Description = "Description 3", Path = "C:\\Path3", IsDefault = true }
        };

        _context.Workspaces.AddRange(workspaces);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(w => w.Name == "Workspace 1");
        result.Should().Contain(w => w.Name == "Workspace 2");
        result.Should().Contain(w => w.Name == "Workspace 3");
    }

    [Fact]
    public async Task UpdateAsync_WithValidWorkspace_ShouldUpdateWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "Original Name",
            Description = "Original Description",
            Path = "C:\\Original\\Path",
            IsDefault = false
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Update the workspace
        workspace.Name = "Updated Name";
        workspace.Description = "Updated Description";

        // Act
        var result = await _repository.UpdateAsync(workspace);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");

        // Verify it was updated in database
        var updatedWorkspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Name == "Updated Name");
        updatedWorkspace.Should().NotBeNull();
        updatedWorkspace!.Name.Should().Be("Updated Name");
        updatedWorkspace.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingWorkspace_ShouldDeleteWorkspace()
    {
        // Arrange
        var workspace = new Workspace
        {
            Name = "Workspace to Delete",
            Description = "Description",
            Path = "C:\\Delete\\Path",
            IsDefault = false
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(workspace.Id);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();

        // Verify it was deleted from database
        var deletedWorkspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Name == "Workspace to Delete");
        deletedWorkspace.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
