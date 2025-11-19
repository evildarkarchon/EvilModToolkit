using System.Collections.Generic;

namespace EvilModToolkit.Models;

/// <summary>
/// Represents information about a Fallout 4 installation.
/// </summary>
public class GameInfo
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
}
