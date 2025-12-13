using System;
using System.IO;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Patching;

/// <summary>
/// Service for manipulating BA2 archive files.
/// BA2 format: Magic "BTDX" at offset 0, version byte at offset 4.
/// </summary>
public class BA2ArchiveService : IBA2ArchiveService
{
    private const int MagicOffset = 0;
    private const int VersionOffset = 4;
    private const int TypeOffset = 8;
    private const uint ExpectedMagic = 0x58445442; // "BTDX" in little-endian
    private const uint GeneralMagic = 0x4C524E47; // "GNRL" in little-endian
    private const uint TextureMagic = 0x30315844; // "DX10" in little-endian

    private readonly ILogger<BA2ArchiveService> _logger;

    public BA2ArchiveService(ILogger<BA2ArchiveService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public BA2ArchiveInfo? GetArchiveInfo(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("BA2 file not found: {FilePath}", filePath);
                return null;
            }

            var fileInfo = new FileInfo(filePath);

            // Read all header data in a single file operation for performance
            // Header layout: Magic (4 bytes), Version (4 bytes), Type (4 bytes)
            var (version, type) = ReadHeaderInfo(filePath);

            return new BA2ArchiveInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                IsValid = version != BA2Version.Unknown && type != BA2Type.Unknown,
                Version = version,
                Type = type,
                FileSizeBytes = fileInfo.Length,
                IsReadOnly = fileInfo.IsReadOnly
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading BA2 archive info: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Reads the magic, version, and type from a BA2 file in a single file operation.
    /// </summary>
    /// <param name="filePath">The path to the BA2 file.</param>
    /// <returns>A tuple of (version, type). Returns (Unknown, Unknown) if the file is invalid.</returns>
    private (BA2Version version, BA2Type type) ReadHeaderInfo(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < TypeOffset + 4)
                return (BA2Version.Unknown, BA2Type.Unknown);

            // Read magic, version, and type in one buffer read
            var buffer = new byte[12];
            var bytesRead = stream.Read(buffer, 0, 12);
            if (bytesRead < 12)
                return (BA2Version.Unknown, BA2Type.Unknown);

            // Validate magic
            var magic = BitConverter.ToUInt32(buffer, MagicOffset);
            if (magic != ExpectedMagic)
                return (BA2Version.Unknown, BA2Type.Unknown);

            // Parse version
            var versionByte = buffer[VersionOffset];
            var version = versionByte switch
            {
                0x01 => BA2Version.V1,
                0x07 => BA2Version.V7,
                0x08 => BA2Version.V8,
                _ => BA2Version.Unknown
            };

            // Parse type
            var typeMagic = BitConverter.ToUInt32(buffer, TypeOffset);
            var type = typeMagic switch
            {
                GeneralMagic => BA2Type.General,
                TextureMagic => BA2Type.Texture,
                _ => BA2Type.Unknown
            };

            return (version, type);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading BA2 header: {FilePath}", filePath);
            return (BA2Version.Unknown, BA2Type.Unknown);
        }
    }

    /// <inheritdoc />
    public bool PatchArchiveVersion(string filePath, BA2Version targetVersion)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("BA2 file not found: {FilePath}", filePath);
                return false;
            }

            // Use optimized single-read method
            var (currentVersion, _) = ReadHeaderInfo(filePath);
            if (currentVersion == BA2Version.Unknown)
            {
                _logger.LogError("File is not a valid BA2 archive: {FilePath}", filePath);
                return false;
            }

            if (currentVersion == targetVersion)
            {
                _logger.LogInformation("BA2 file already at target version {Version}: {FilePath}",
                    targetVersion, filePath);
                return true;
            }

            // Remove read-only attribute if present
            var fileInfo = new FileInfo(filePath);
            var wasReadOnly = fileInfo.IsReadOnly;
            if (wasReadOnly)
            {
                _logger.LogDebug("Removing read-only attribute from {FilePath}", filePath);
                fileInfo.IsReadOnly = false;
            }

            try
            {
                // Patch the version byte
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    stream.Seek(VersionOffset, SeekOrigin.Begin);
                    stream.WriteByte((byte)targetVersion);
                }

                _logger.LogInformation(
                    "Successfully patched BA2 from v{CurrentVersion} to v{TargetVersion}: {FilePath}",
                    (byte)currentVersion, (byte)targetVersion, filePath);

                return true;
            }
            finally
            {
                // Restore read-only attribute if it was set
                if (wasReadOnly)
                {
                    fileInfo.IsReadOnly = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching BA2 archive: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsValidBA2(string filePath)
    {
        // Use the optimized header reading method
        var (version, type) = ReadHeaderInfo(filePath);
        return version != BA2Version.Unknown && type != BA2Type.Unknown;
    }
}