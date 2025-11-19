using System;
using System.IO;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Configuration;

public class SettingsServiceTests : IDisposable
{
    private readonly ILogger<SettingsService> _logger;
    private readonly SettingsService _sut;
    private readonly string _testSettingsPath;

    public SettingsServiceTests()
    {
        _logger = Substitute.For<ILogger<SettingsService>>();
        _sut = new SettingsService(_logger);

        // Use a unique test directory to avoid conflicts
        var testDir = Path.Combine(Path.GetTempPath(), "SettingsTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        _testSettingsPath = Path.Combine(testDir, "settings.json");
    }

    public void Dispose()
    {
        // Clean up test settings file
        var testDir = Path.GetDirectoryName(_testSettingsPath);
        if (testDir != null && Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void GetDefaultSettings_ReturnsDefaultValues()
    {
        // Act
        var result = _sut.GetDefaultSettings();

        // Assert
        result.Should().NotBeNull();
        result.GamePathOverride.Should().BeNull();
        result.WindowWidth.Should().Be(1200);
        result.WindowHeight.Should().Be(800);
        result.Theme.Should().Be("System");
        result.LogLevel.Should().Be("Information");
        result.ScanF4SERecursively.Should().BeFalse();
        result.ShowHiddenFiles.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange - use a testable service with non-existent path
        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, _testSettingsPath);

        // Act
        var result = await testService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(testService.GetDefaultSettings());
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesFileWithCorrectContent()
    {
        // Arrange
        var settings = new AppSettings
        {
            GamePathOverride = @"C:\Games\Fallout4",
            WindowWidth = 1920,
            WindowHeight = 1080,
            Theme = "Dark"
        };

        // Create a temporary settings service that uses our test path
        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, _testSettingsPath);

        // Act
        await testService.SaveSettingsAsync(settings);

        // Assert
        File.Exists(_testSettingsPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(_testSettingsPath);
        // JSON escapes backslashes, so check for the escaped version
        json.Should().Contain("C:\\\\Games\\\\Fallout4");
        json.Should().Contain("1920");
        json.Should().Contain("Dark");
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesAllSettings()
    {
        // Arrange
        var originalSettings = new AppSettings
        {
            GamePathOverride = @"C:\TestPath\Fallout4",
            MO2PathOverride = @"C:\TestPath\MO2",
            VortexPathOverride = @"C:\TestPath\Vortex",
            LastF4SEScanDirectory = @"C:\TestPath\Plugins",
            LastBA2PatchDirectory = @"C:\TestPath\BA2",
            ScanF4SERecursively = true,
            WindowWidth = 1600,
            WindowHeight = 900,
            ShowHiddenFiles = true,
            Theme = "Light",
            LogLevel = "Debug"
        };

        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, _testSettingsPath);

        // Act
        await testService.SaveSettingsAsync(originalSettings);
        var loadedSettings = await testService.LoadSettingsAsync();

        // Assert
        loadedSettings.Should().BeEquivalentTo(originalSettings);
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid());
        var testPath = Path.Combine(nonExistentDir, "settings.json");
        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, testPath);

        var settings = _sut.GetDefaultSettings();

        try
        {
            // Act
            await testService.SaveSettingsAsync(settings);

            // Assert
            Directory.Exists(nonExistentDir).Should().BeTrue();
            File.Exists(testPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(nonExistentDir))
            {
                Directory.Delete(nonExistentDir, true);
            }
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_WithCorruptedFile_ReturnsDefaultSettings()
    {
        // Arrange
        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, _testSettingsPath);

        // Create a corrupted JSON file
        await File.WriteAllTextAsync(_testSettingsPath, "{ invalid json content }");

        // Act
        var result = await testService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(_sut.GetDefaultSettings());
    }

    [Fact]
    public void GetSettingsFilePath_ReturnsValidPath()
    {
        // Act
        var result = _sut.GetSettingsFilePath();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWithEquivalentOf("settings.json");
        result.Should().Contain("EvilModToolkit");
    }

    [Fact]
    public async Task SaveSettingsAsync_WithNullPaths_SavesSuccessfully()
    {
        // Arrange
        var settings = new AppSettings
        {
            GamePathOverride = null,
            MO2PathOverride = null,
            VortexPathOverride = null
        };

        var testLogger = Substitute.For<ILogger<SettingsService>>();
        var testService = new TestableSettingsService(testLogger, _testSettingsPath);

        // Act
        await testService.SaveSettingsAsync(settings);
        var loaded = await testService.LoadSettingsAsync();

        // Assert
        loaded.GamePathOverride.Should().BeNull();
        loaded.MO2PathOverride.Should().BeNull();
        loaded.VortexPathOverride.Should().BeNull();
    }

    // Helper class to allow testing with a custom settings path
    private class TestableSettingsService : SettingsService
    {
        private readonly string _customPath;

        public TestableSettingsService(ILogger<SettingsService> logger, string customPath)
            : base(logger)
        {
            _customPath = customPath;
        }

        public override string GetSettingsFilePath() => _customPath;
    }
}
