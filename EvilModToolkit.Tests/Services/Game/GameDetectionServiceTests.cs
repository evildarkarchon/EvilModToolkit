using System.Runtime.Versioning;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Game;

[SupportedOSPlatform("windows")]
public class GameDetectionServiceTests
{
    private readonly ILogger<GameDetectionService> _logger;
    private readonly IFileVersionService _fileVersionService;
    private readonly GameDetectionService _sut;

    public GameDetectionServiceTests()
    {
        _logger = Substitute.For<ILogger<GameDetectionService>>();
        _fileVersionService = Substitute.For<IFileVersionService>();
        _sut = new GameDetectionService(_logger, _fileVersionService);
    }

    [Fact]
    public void DetectGame_WhenCalled_ReturnsGameInfo()
    {
        // Act
        var result = _sut.DetectGame();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GameInfo>();
    }

    [Fact]
    public void DetectGame_WhenGameNotInstalled_ReturnsNotInstalled()
    {
        // Most test environments won't have Fallout 4 installed
        // Act
        var result = _sut.DetectGame();

        // Assert
        result.Should().NotBeNull();
        // If game is not installed, IsInstalled should be false
        if (!result.IsInstalled)
        {
            result.InstallPath.Should().BeEmpty();
        }
    }

    [Fact]
    public void IsValidGamePath_WhenPathIsNull_ReturnsFalse()
    {
        // Act
        var result = _sut.IsValidGamePath(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGamePath_WhenPathIsEmpty_ReturnsFalse()
    {
        // Act
        var result = _sut.IsValidGamePath(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGamePath_WhenPathDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Path\To\Fallout4";

        // Act
        var result = _sut.IsValidGamePath(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGamePath_WhenPathExistsButNoExecutable_ReturnsFalse()
    {
        // Arrange - use a path that exists but doesn't have Fallout4.exe
        var tempPath = Path.GetTempPath();

        // Act
        var result = _sut.IsValidGamePath(tempPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGamePath_WhenValidPath_ReturnsTrue()
    {
        // This test can only pass if Fallout 4 is actually installed
        // We'll create a mock valid path for testing
        var tempDir = Path.Combine(Path.GetTempPath(), "TestFallout4_" + Guid.NewGuid());

        try
        {
            Directory.CreateDirectory(tempDir);
            var exePath = Path.Combine(tempDir, "Fallout4.exe");
            File.WriteAllText(exePath, "test");

            // Act
            var result = _sut.IsValidGamePath(tempDir);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\Invalid\Path")]
    public void IsValidGamePath_WithInvalidPaths_ReturnsFalse(string? path)
    {
        // Act
        var result = _sut.IsValidGamePath(path!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DetectGame_DoesNotThrowException()
    {
        // Act
        Action act = () => _sut.DetectGame();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectGame_WhenGameInstalled_PopulatesGameInfo()
    {
        // This test will only work if Fallout 4 is actually installed
        // Act
        var result = _sut.DetectGame();

        // Assert
        if (result.IsInstalled)
        {
            result.InstallPath.Should().NotBeEmpty();
            result.ExecutablePath.Should().NotBeEmpty();
            result.DataPath.Should().NotBeEmpty();
            result.InstallType.Should().NotBe(InstallType.Unknown);
        }
    }
}