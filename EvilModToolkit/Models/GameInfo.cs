using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EvilModToolkit.Models;

/// <summary>
/// Represents information about a Fallout 4 installation.
/// </summary>
public partial class GameInfo
{
    /// <summary>
    /// Gets a value indicating whether the game was found.
    /// </summary>
    public bool IsInstalled { get; init; }

    /// <summary>
    /// Gets the installation directory path.
    /// </summary>
    public string InstallPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of installation (Steam, GOG, MS Store).
    /// </summary>
    public InstallType InstallType { get; init; }

    /// <summary>
    /// Gets the game version string (e.g., "1.10.163.0").
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this is the Next Gen update (v1.10.980+) or Original Game.
    /// </summary>
    public bool IsNextGen { get; init; }

    /// <summary>
    /// Gets the path to the game executable.
    /// </summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to the Data directory.
    /// </summary>
    public string DataPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of installed DLCs.
    /// </summary>
    public List<string> InstalledDLCs { get; init; } = new();

    /// <summary>
    /// Gets the game language (e.g., "en", "de", "fr").
    /// </summary>
    public string Language { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to Fallout4.ini.
    /// </summary>
    public string IniPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to Fallout4Prefs.ini.
    /// </summary>
    public string PrefsIniPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to Fallout4Custom.ini.
    /// </summary>
    public string CustomIniPath { get; init; } = string.Empty;

    /// <summary>
    /// Version number pattern (e.g., "1.10.163.0")
    /// </summary>
    [GeneratedRegex(@"^\d+\.\d+\.\d+\.\d+$")]
    private static partial Regex VersionPattern();

    /// <summary>
    /// Validates the game info for consistency.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // If game is installed, validate required paths
        if (IsInstalled)
        {
            if (string.IsNullOrWhiteSpace(InstallPath))
                errors.Add("InstallPath is required when game is marked as installed");
            else if (!Directory.Exists(InstallPath))
                errors.Add($"InstallPath does not exist: {InstallPath}");

            if (string.IsNullOrWhiteSpace(ExecutablePath))
                errors.Add("ExecutablePath is required when game is marked as installed");
            else if (!File.Exists(ExecutablePath))
                errors.Add($"ExecutablePath does not exist: {ExecutablePath}");

            if (string.IsNullOrWhiteSpace(DataPath))
                errors.Add("DataPath is required when game is marked as installed");
            else if (!Directory.Exists(DataPath))
                errors.Add($"DataPath does not exist: {DataPath}");
        }

        // Validate version format if set
        if (!string.IsNullOrWhiteSpace(Version) && !VersionPattern().IsMatch(Version))
            errors.Add($"Version must be in format 'x.x.x.x': {Version}");

        // Validate install type
        if (IsInstalled && InstallType == InstallType.Unknown)
            errors.Add("InstallType should not be Unknown when game is installed");

        return errors;
    }

    /// <summary>
    /// Checks if the game info is valid.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid() => Validate().Count == 0;
}
