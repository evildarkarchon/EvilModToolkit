using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for extracting version information from PE executables (EXE, DLL).
/// </summary>
public interface IFileVersionService
{
    /// <summary>
    /// Gets version information from the specified file.
    /// </summary>
    /// <param name="filePath">Path to the file to analyze.</param>
    /// <returns>Version information, or null if the file doesn't exist or has no version info.</returns>
    VersionInfo? GetFileVersion(string filePath);
}