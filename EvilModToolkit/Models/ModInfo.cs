using System.Collections.Generic;

namespace EvilModToolkit.Models;

/// <summary>
/// Represents metadata about a single mod in the game or mod manager.
/// </summary>
public class ModInfo
{
    /// <summary>
    /// Mod name or identifier
    /// </summary>
    public string ModName { get; init; } = string.Empty;

    /// <summary>
    /// Path to mod directory or installation location
    /// </summary>
    public string ModPath { get; init; } = string.Empty;

    /// <summary>
    /// List of all files contained in this mod
    /// </summary>
    public List<string> Files { get; init; } = new();

    /// <summary>
    /// List of plugin files (ESM, ESP, ESL) in this mod
    /// </summary>
    public List<string> Plugins { get; init; } = new();

    /// <summary>
    /// List of BA2 archive files in this mod
    /// </summary>
    public List<string> Archives { get; init; } = new();

    /// <summary>
    /// Mod version if available
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Whether this mod is currently enabled/active
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Plugin load order position (if applicable)
    /// </summary>
    public int LoadOrder { get; init; } = -1;

    /// <summary>
    /// Required master files for this mod's plugins
    /// </summary>
    public List<string> Masters { get; init; } = new();

    /// <summary>
    /// Source mod manager (MO2, Vortex, or null for unmanaged)
    /// </summary>
    public string? ModManager { get; init; }

    /// <summary>
    /// Whether this mod is managed by a mod manager
    /// </summary>
    public bool IsManaged => !string.IsNullOrEmpty(ModManager);

    /// <summary>
    /// Total number of files in this mod
    /// </summary>
    public int FileCount => Files.Count;

    /// <summary>
    /// Total number of plugins in this mod
    /// </summary>
    public int PluginCount => Plugins.Count;

    /// <summary>
    /// Total number of archives in this mod
    /// </summary>
    public int ArchiveCount => Archives.Count;

    /// <summary>
    /// Creates a new ModInfo instance
    /// </summary>
    public ModInfo(
        string modName,
        string modPath,
        bool isEnabled = true,
        string? modManager = null)
    {
        ModName = modName;
        ModPath = modPath;
        IsEnabled = isEnabled;
        ModManager = modManager;
    }

    /// <summary>
    /// Validates the mod info for consistency
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ModName))
            return false;

        if (string.IsNullOrWhiteSpace(ModPath))
            return false;

        if (LoadOrder < -1)
            return false;

        return true;
    }
}