using System.Collections.Generic;

namespace EvilModToolkit.Models;

/// <summary>
/// Represents Mod Organizer 2 settings parsed from ModOrganizer.ini.
/// </summary>
public class MO2Settings
{
    /// <summary>
    /// Gets or sets the game name (should be "Fallout 4").
    /// </summary>
    public string GameName { get; set; } = "Fallout 4";

    /// <summary>
    /// Gets or sets the path to the game installation.
    /// </summary>
    public string? GamePath { get; set; }

    /// <summary>
    /// Gets or sets the selected profile name.
    /// </summary>
    public string? SelectedProfile { get; set; }

    /// <summary>
    /// Gets or sets the MO2 base directory.
    /// </summary>
    public string? BaseDirectory { get; set; }

    /// <summary>
    /// Gets or sets the cache directory path.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the downloads directory path.
    /// </summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>
    /// Gets or sets the mods directory path.
    /// </summary>
    public string? ModDirectory { get; set; }

    /// <summary>
    /// Gets or sets the overwrite directory path.
    /// </summary>
    public string? OverwriteDirectory { get; set; }

    /// <summary>
    /// Gets or sets the profiles directory path.
    /// </summary>
    public string? ProfilesDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether profiles use local INI files.
    /// </summary>
    public bool ProfileLocalInis { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether profiles use local save files.
    /// </summary>
    public bool ProfileLocalSaves { get; set; }

    /// <summary>
    /// Gets or sets file suffixes to skip (e.g., ".mohidden").
    /// </summary>
    public List<string> SkipFileSuffixes { get; set; } = new() { ".mohidden" };

    /// <summary>
    /// Gets or sets directory names to skip.
    /// </summary>
    public HashSet<string> SkipDirectories { get; set; } = new();

    /// <summary>
    /// Gets or sets paths to custom executables configured in MO2.
    /// Key is the tool name, value is the list of executable paths.
    /// </summary>
    public Dictionary<string, List<string>> CustomExecutables { get; set; } = new();
}
