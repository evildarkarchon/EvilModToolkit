namespace EvilModToolkit.Models;

/// <summary>
/// Represents version information extracted from a file.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Gets the full version string (e.g., "1.10.163.0").
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file version string.
    /// </summary>
    public string FileVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product version string.
    /// </summary>
    public string ProductVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file description.
    /// </summary>
    public string? FileDescription { get; init; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string? ProductName { get; init; }

    /// <summary>
    /// Gets the company name.
    /// </summary>
    public string? CompanyName { get; init; }

    /// <summary>
    /// Gets the copyright information.
    /// </summary>
    public string? Copyright { get; init; }

    /// <summary>
    /// Gets the internal name of the file.
    /// </summary>
    public string? InternalName { get; init; }

    /// <summary>
    /// Gets the original filename.
    /// </summary>
    public string? OriginalFilename { get; init; }
}