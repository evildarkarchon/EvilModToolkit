using System;
using System.IO;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Patching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Patching;

public class BA2ArchiveServiceTests : IDisposable
{
    private readonly ILogger<BA2ArchiveService> _logger;
    private readonly BA2ArchiveService _sut;
    private readonly string _testDirectory;

    public BA2ArchiveServiceTests()
    {
        _logger = Substitute.For<ILogger<BA2ArchiveService>>();
        _sut = new BA2ArchiveService(_logger);
        _testDirectory = Path.Combine(Path.GetTempPath(), "BA2Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            // Remove read-only attributes before deleting
            foreach (var file in Directory.GetFiles(_testDirectory))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.IsReadOnly)
                    fileInfo.IsReadOnly = false;
            }
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetArchiveInfo_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "NonExistent.ba2");

        // Act
        var result = _sut.GetArchiveInfo(nonExistentFile);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsValidBA2_WhenFileHasCorrectMagic_ReturnsTrue()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);

        // Act
        var result = _sut.IsValidBA2(testFile);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidBA2_WhenFileHasInvalidMagic_ReturnsFalse()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "Invalid.ba2");
        File.WriteAllBytes(testFile, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01 });

        // Act
        var result = _sut.IsValidBA2(testFile);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetArchiveInfo_WithValidBA2_ReturnsCorrectInfo()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Version.Should().Be(BA2Version.V1);
        result.FilePath.Should().Be(testFile);
        result.FileName.Should().EndWithEquivalentOf(".ba2");
        result.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PatchArchiveVersion_FromV1ToV8_SucceedsAndChangesVersion()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);

        // Act
        var result = _sut.PatchArchiveVersion(testFile, BA2Version.V8);

        // Assert
        result.Should().BeTrue();
        var info = _sut.GetArchiveInfo(testFile);
        info!.Version.Should().Be(BA2Version.V8);
    }

    [Fact]
    public void PatchArchiveVersion_FromV8ToV1_SucceedsAndChangesVersion()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V8);

        // Act
        var result = _sut.PatchArchiveVersion(testFile, BA2Version.V1);

        // Assert
        result.Should().BeTrue();
        var info = _sut.GetArchiveInfo(testFile);
        info!.Version.Should().Be(BA2Version.V1);
    }

    [Fact]
    public void PatchArchiveVersion_WhenAlreadyAtTargetVersion_ReturnsTrue()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);

        // Act
        var result = _sut.PatchArchiveVersion(testFile, BA2Version.V1);

        // Assert
        result.Should().BeTrue();
        var info = _sut.GetArchiveInfo(testFile);
        info!.Version.Should().Be(BA2Version.V1);
    }

    [Fact]
    public void PatchArchiveVersion_WhenFileIsReadOnly_TemporarilyRemovesReadOnlyAndRestores()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);
        var fileInfo = new FileInfo(testFile);
        fileInfo.IsReadOnly = true;

        // Act
        var result = _sut.PatchArchiveVersion(testFile, BA2Version.V8);

        // Assert
        result.Should().BeTrue();
        fileInfo.Refresh();
        fileInfo.IsReadOnly.Should().BeTrue("read-only flag should be restored");
        var info = _sut.GetArchiveInfo(testFile);
        info!.Version.Should().Be(BA2Version.V8);
    }

    [Fact]
    public void PatchArchiveVersion_WithInvalidFile_ReturnsFalse()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "Invalid.ba2");
        File.WriteAllBytes(testFile, new byte[] { 0x00, 0x00, 0x00, 0x00 });

        // Act
        var result = _sut.PatchArchiveVersion(testFile, BA2Version.V8);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetArchiveInfo_WithReadOnlyFile_IndicatesReadOnly()
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1);
        var fileInfo = new FileInfo(testFile);
        fileInfo.IsReadOnly = true;

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.IsReadOnly.Should().BeTrue();
    }

    [Theory]
    [InlineData(BA2Version.V1)]
    [InlineData(BA2Version.V7)]
    [InlineData(BA2Version.V8)]
    public void GetArchiveInfo_WithDifferentVersions_DetectsCorrectVersion(BA2Version version)
    {
        // Arrange
        var testFile = CreateTestBA2File(version);

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(version);
    }

    [Fact]
    public void PatchArchiveVersion_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "NonExistent.ba2");

        // Act
        var result = _sut.PatchArchiveVersion(nonExistentFile, BA2Version.V8);

        // Assert
        result.Should().BeFalse();
    }

    private string CreateTestBA2File(BA2Version version)
    {
        var fileName = $"Test_{version}_{Guid.NewGuid()}.ba2";
        var filePath = Path.Combine(_testDirectory, fileName);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write magic "BTDX" (0x58445442 in little-endian)
        writer.Write(0x58445442u);

        // Write version byte at offset 4
        writer.Write((byte)version);

        // Write some dummy data to make it more realistic
        writer.Write(new byte[100]);

        return filePath;
    }
}
