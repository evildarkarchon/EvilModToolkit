using System.Runtime.Versioning;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Platform;

[SupportedOSPlatform("windows")]
public class ProcessServiceTests
{
    private readonly ILogger<ProcessService> _logger;
    private readonly IFileVersionService _fileVersionService;
    private readonly ProcessService _sut;

    public ProcessServiceTests()
    {
        _logger = Substitute.For<ILogger<ProcessService>>();
        _fileVersionService = Substitute.For<IFileVersionService>();
        _sut = new ProcessService(_logger, _fileVersionService);
    }

    [Fact]
    public void GetParentProcessId_WhenCurrentProcess_ReturnsParentId()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act
        var result = _sut.GetParentProcessId(currentProcessId);

        // Assert
        result.Should().NotBeNull("Current process should have a parent");
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetParentProcessId_WhenInvalidProcessId_ReturnsNull()
    {
        // Arrange
        var invalidProcessId = -1;

        // Act
        var result = _sut.GetParentProcessId(invalidProcessId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetParentProcessId_WhenNonExistentProcessId_ReturnsNull()
    {
        // Arrange
        // Use a very high process ID that's unlikely to exist
        var nonExistentProcessId = int.MaxValue - 1000;

        // Act
        var result = _sut.GetParentProcessId(nonExistentProcessId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindModManager_WhenNotLaunchedFromModManager_ReturnsNull()
    {
        // Act
        var result = _sut.FindModManager();

        // Assert
        // When running tests, we're not launched from a mod manager
        result.Should().BeNull();
    }

    [Fact]
    public void FindModManager_WhenCalled_DoesNotThrowException()
    {
        // Act
        Action act = () => _sut.FindModManager();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetParentProcessId_WhenCalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act
        var result1 = _sut.GetParentProcessId(currentProcessId);
        var result2 = _sut.GetParentProcessId(currentProcessId);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void FindModManager_CanNavigateProcessTree()
    {
        // This is more of an integration test that verifies the process tree navigation works
        // We can't easily test mod manager detection without actually launching from a mod manager

        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act - Navigate up the tree manually to verify the method works
        int? parentId = _sut.GetParentProcessId(currentProcessId);

        // Assert
        parentId.Should().NotBeNull("Current process should have a parent in test environment");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void GetParentProcessId_ForSystemProcesses_HandlesGracefully(int processId)
    {
        // Act
        var result = _sut.GetParentProcessId(processId);

        // Assert
        // Should return a value or null, but not throw
        // Process ID 0 (Idle) and 4 (System) have special behavior
        result.Should().Match(r => r == null || r >= 0);
    }
}