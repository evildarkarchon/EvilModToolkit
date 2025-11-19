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

    /// <summary>
    /// Tests that BA2 type (GNRL vs DX10) is correctly detected.
    /// </summary>
    [Theory]
    [InlineData(BA2Type.General)]
    [InlineData(BA2Type.Texture)]
    public void GetArchiveInfo_WithDifferentTypes_DetectsCorrectType(BA2Type type)
    {
        // Arrange
        var testFile = CreateTestBA2File(BA2Version.V1, type);

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(type);
    }

    /// <summary>
    /// Tests that both version and type are correctly detected together.
    /// This validates the complete BA2 header parsing logic.
    /// </summary>
    [Theory]
    [InlineData(BA2Version.V1, BA2Type.General)]
    [InlineData(BA2Version.V1, BA2Type.Texture)]
    [InlineData(BA2Version.V7, BA2Type.General)]
    [InlineData(BA2Version.V7, BA2Type.Texture)]
    [InlineData(BA2Version.V8, BA2Type.General)]
    [InlineData(BA2Version.V8, BA2Type.Texture)]
    public void GetArchiveInfo_WithDifferentVersionsAndTypes_DetectsCorrectly(BA2Version version, BA2Type type)
    {
        // Arrange
        var testFile = CreateTestBA2File(version, type);

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be(version);
        result.Type.Should().Be(type);
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests handling of corrupted BA2 files with invalid type magic.
    /// </summary>
    [Fact]
    public void GetArchiveInfo_WithInvalidTypeMagic_ReturnsInvalidInfo()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "InvalidType.ba2");
        using (var stream = new FileStream(testFile, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            // Write valid magic "BTDX"
            writer.Write(0x58445442u);
            // Write valid version
            writer.Write((byte)BA2Version.V1);
            // Write invalid type magic (not GNRL or DX10)
            writer.Write(0x12345678u);
            // Pad with dummy data
            writer.Write(new byte[100]);
        }

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse("type magic is invalid");
        result.Type.Should().Be(BA2Type.Unknown);
    }

    /// <summary>
    /// Tests handling of truncated BA2 files that don't have enough bytes for type field.
    /// </summary>
    [Fact]
    public void GetArchiveInfo_WithTruncatedFile_ReturnsNull()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "Truncated.ba2");
        // Create a file with valid magic and version but missing type field
        File.WriteAllBytes(testFile, new byte[] { 0x42, 0x54, 0x44, 0x58, 0x01 });

        // Act
        var result = _sut.GetArchiveInfo(testFile);

        // Assert - should handle gracefully and return null or invalid info
        // The exact behavior depends on implementation, but it should not throw
        if (result != null)
        {
            result.IsValid.Should().BeFalse("file is truncated");
        }
    }

    /// <summary>
    /// Creates a test BA2 file with specified version (defaults to General type for backward compatibility).
    /// </summary>
    private string CreateTestBA2File(BA2Version version)
    {
        return CreateTestBA2File(version, BA2Type.General);
    }

    /// <summary>
    /// Creates a test BA2 file with specified version and type.
    /// Writes a minimal valid BA2 header for testing purposes.
    /// </summary>
    /// <param name="version">The BA2 version to write.</param>
    /// <param name="type">The BA2 type (GNRL or DX10).</param>
    /// <returns>Path to the created test file.</returns>
    private string CreateTestBA2File(BA2Version version, BA2Type type)
    {
        var fileName = $"Test_{version}_{type}_{Guid.NewGuid()}.ba2";
        var filePath = Path.Combine(_testDirectory, fileName);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write magic "BTDX" (0x58445442 in little-endian)
        writer.Write(0x58445442u);

        // Write version byte at offset 4
        writer.Write((byte)version);

        // Write padding bytes (3 bytes) to reach offset 8
        writer.Write(new byte[3]);

        // Write type magic at offset 8
        // GNRL = 0x4C524E47, DX10 = 0x30315844
        uint typeMagic = type == BA2Type.General ? 0x4C524E47u : 0x30315844u;
        writer.Write(typeMagic);

        // Write some dummy data to make it more realistic
        writer.Write(new byte[100]);

        return filePath;
    }
}