using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Platform;

public class FileVersionServiceTests
{
    private readonly ILogger<FileVersionService> _logger;
    private readonly FileVersionService _sut;

    public FileVersionServiceTests()
    {
        _logger = Substitute.For<ILogger<FileVersionService>>();
        _sut = new FileVersionService(_logger);
    }

    [Fact]
    public void GetFileVersion_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\File.exe";

        // Act
        var result = _sut.GetFileVersion(nonExistentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetFileVersion_WhenFileExistsWithVersionInfo_ReturnsVersionInfo()
    {
        // Arrange
        // Use a known Windows system file that should have version info
        var systemFile = Path.Combine(Environment.SystemDirectory, "kernel32.dll");

        // Act
        var result = _sut.GetFileVersion(systemFile);

        // Assert
        result.Should().NotBeNull();
        result!.FileVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetFileVersion_WhenCalledWithValidFile_PopulatesAllProperties()
    {
        // Arrange
        var systemFile = Path.Combine(Environment.SystemDirectory, "notepad.exe");

        // Act
        var result = _sut.GetFileVersion(systemFile);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().NotBeNullOrEmpty();
        result.FileVersion.Should().NotBeNullOrEmpty();
        // ProductVersion might be different from FileVersion in some cases
        result.ProductVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetFileVersion_WhenFileHasNoVersionInfo_ReturnsNull()
    {
        // Arrange
        // Create a temporary file with no version info
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "test content");

            // Act
            var result = _sut.GetFileVersion(tempFile);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileVersion_WhenPathIsNullOrEmpty_ReturnsNull(string? path)
    {
        // Act
        var result = _sut.GetFileVersion(path!);

        // Assert
        result.Should().BeNull();
    }
}
