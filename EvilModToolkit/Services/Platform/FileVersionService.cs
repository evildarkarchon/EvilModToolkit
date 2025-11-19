using System;
using System.Diagnostics;
using System.IO;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for extracting version information from PE executables (EXE, DLL).
/// </summary>
public class FileVersionService : IFileVersionService
{
    private readonly ILogger<FileVersionService> _logger;

    public FileVersionService(ILogger<FileVersionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public VersionInfo? GetFileVersion(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

            // If no version info is present, return null
            if (string.IsNullOrEmpty(versionInfo.FileVersion) &&
                string.IsNullOrEmpty(versionInfo.ProductVersion))
            {
                _logger.LogDebug("No version information found in file: {FilePath}", filePath);
                return null;
            }

            return new VersionInfo
            {
                Version = versionInfo.FileVersion ?? string.Empty,
                FileVersion = versionInfo.FileVersion ?? string.Empty,
                ProductVersion = versionInfo.ProductVersion ?? string.Empty,
                FileDescription = versionInfo.FileDescription,
                ProductName = versionInfo.ProductName,
                CompanyName = versionInfo.CompanyName,
                Copyright = versionInfo.LegalCopyright,
                InternalName = versionInfo.InternalName,
                OriginalFilename = versionInfo.OriginalFilename
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading version info from file: {FilePath}", filePath);
            return null;
        }
    }
}