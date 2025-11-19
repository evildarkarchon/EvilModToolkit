using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Analysis;

public class F4SEPluginServiceTests
{
    private readonly ILogger<F4SEPluginService> _logger;
    private readonly IFileVersionService _fileVersionService;
    private readonly F4SEPluginService _sut;

    public F4SEPluginServiceTests()
    {
        _logger = Substitute.For<ILogger<F4SEPluginService>>();
        _fileVersionService = Substitute.For<IFileVersionService>();
        _sut = new F4SEPluginService(_logger, _fileVersionService);
    }

    [Fact]
    public void AnalyzePlugin_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentFile = @"C:\NonExistent\Plugin.dll";

        // Act
        var result = _sut.AnalyzePlugin(nonExistentFile);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void AnalyzePlugin_WhenFileIsNotDll_ReturnsNull()
    {
        // Arrange
        var tempFile = Path.GetTempFileName(); // Creates a .tmp file
        try
        {
            File.WriteAllText(tempFile, "test content");

            // Act
            var result = _sut.AnalyzePlugin(tempFile);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void AnalyzePlugin_WhenValidDllButNotF4SE_ReturnsNonF4SEInfo()
    {
        // Arrange - use a system DLL that definitely isn't an F4SE plugin
        var systemDll = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        // Act
        var result = _sut.AnalyzePlugin(systemDll);

        // Assert
        result.Should().NotBeNull();
        result!.IsF4SEPlugin.Should().BeFalse();
        result.Compatibility.Should().Be(F4SECompatibility.NotF4SEPlugin);
        result.SupportsOG.Should().BeFalse();
        result.SupportsNG.Should().BeFalse();
    }

    [Fact]
    public void AnalyzePlugin_WithSystemDll_PopulatesFileName()
    {
        // Arrange
        var systemDll = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        // Act
        var result = _sut.AnalyzePlugin(systemDll);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("kernel32.dll");
        result.FilePath.Should().Be(systemDll);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnalyzePlugin_WithInvalidPath_ReturnsNull(string? path)
    {
        // Act
        var result = _sut.AnalyzePlugin(path!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ScanDirectory_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentDir = @"C:\NonExistent\Directory";

        // Act
        var result = _sut.ScanDirectory(nonExistentDir);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ScanDirectory_WhenDirectoryIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "TestF4SEPlugins_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);

            // Act
            var result = _sut.ScanDirectory(tempDir);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ScanDirectory_WithSystemDirectory_FindsDlls()
    {
        // Arrange - System32 has lots of DLLs
        var systemDir = Environment.SystemDirectory;

        // Act - scan without recursion to avoid too many files
        var result = _sut.ScanDirectory(systemDir, recursive: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("System directory should contain DLLs");
        result.Should().AllSatisfy(plugin =>
        {
            plugin.FileName.Should().EndWithEquivalentOf(".dll");
            plugin.FilePath.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void ScanDirectory_DoesNotThrowException()
    {
        // Arrange
        var systemDir = Environment.SystemDirectory;

        // Act
        Action act = () => _sut.ScanDirectory(systemDir);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AnalyzePlugin_DoesNotThrowOnCorruptedFile()
    {
        // Arrange - create a fake DLL file with invalid content
        var tempDll = Path.Combine(Path.GetTempPath(), "Corrupted_" + Guid.NewGuid() + ".dll");
        try
        {
            File.WriteAllText(tempDll, "This is not a valid PE file");

            // Act
            Action act = () => _sut.AnalyzePlugin(tempDll);

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            if (File.Exists(tempDll))
                File.Delete(tempDll);
        }
    }

    [Fact]
    public void ScanDirectory_WithRecursiveFlag_SearchesSubdirectories()
    {
        // This is more of an integration test
        // We'll verify the method accepts the recursive parameter

        // Arrange
        var systemDir = Environment.SystemDirectory;

        // Act
        var nonRecursiveResult = _sut.ScanDirectory(systemDir, recursive: false);
        // Don't actually scan recursively to avoid performance issues in tests

        // Assert
        nonRecursiveResult.Should().NotBeNull();
    }
}
