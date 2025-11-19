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
            var version = ReadVersion(filePath);
            var type = ReadType(filePath);

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

            if (!IsValidBA2(filePath))
            {
                _logger.LogError("File is not a valid BA2 archive: {FilePath}", filePath);
                return false;
            }

            var currentVersion = ReadVersion(filePath);
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
        try
        {
            if (!File.Exists(filePath))
                return false;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (stream.Length < VersionOffset + 1)
                return false;

            // Read magic bytes
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            var magic = BitConverter.ToUInt32(buffer, 0);

            return magic == ExpectedMagic;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking BA2 validity: {FilePath}", filePath);
            return false;
        }
    }

    private BA2Version ReadVersion(string filePath)
    {
        try
        {
            if (!IsValidBA2(filePath))
                return BA2Version.Unknown;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            stream.Seek(VersionOffset, SeekOrigin.Begin);
            var versionByte = (byte)stream.ReadByte();

            return versionByte switch
            {
                0x01 => BA2Version.V1,
                0x07 => BA2Version.V7,
                0x08 => BA2Version.V8,
                _ => BA2Version.Unknown
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading BA2 version: {FilePath}", filePath);
            return BA2Version.Unknown;
        }
    }

    private BA2Type ReadType(string filePath)
    {
        try
        {
            if (!IsValidBA2(filePath))
                return BA2Type.Unknown;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            stream.Seek(TypeOffset, SeekOrigin.Begin);

            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            var typeMagic = BitConverter.ToUInt32(buffer, 0);

            return typeMagic switch
            {
                GeneralMagic => BA2Type.General,
                TextureMagic => BA2Type.Texture,
                _ => BA2Type.Unknown
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading BA2 type: {FilePath}", filePath);
            return BA2Type.Unknown;
        }
    }
}