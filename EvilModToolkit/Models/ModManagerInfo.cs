namespace EvilModToolkit.Models;

/// <summary>
/// Represents information about a detected mod manager.
/// </summary>
public class ModManagerInfo
{
    /// <summary>
    /// Gets the type of mod manager detected.
    /// </summary>
    public ModManagerType Type { get; init; }

    /// <summary>
    /// Gets the path to the mod manager executable.
    /// </summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the version of the mod manager.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the process ID of the mod manager (if running).
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Gets the working directory of the mod manager.
    /// </summary>
    public string WorkingDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to the mod manager's configuration file (INI).
    /// </summary>
    public string? ConfigFilePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether MO2 is running in portable mode.
    /// </summary>
    public bool IsPortable { get; init; }

    /// <summary>
    /// Gets the MO2-specific settings (only populated for ModOrganizer2).
    /// </summary>
    public MO2Settings? MO2Settings { get; init; }

    /// <summary>
    /// Gets the path to the game as configured in the mod manager.
    /// </summary>
    public string? GamePath { get; init; }

    /// <summary>
    /// Gets the selected profile name (MO2 only).
    /// </summary>
    public string? SelectedProfile { get; init; }
}

/// <summary>
/// Represents the type of mod manager.
/// </summary>
public enum ModManagerType
{
    /// <summary>
    /// No mod manager detected.
    /// </summary>
    None,

    /// <summary>
    /// Mod Organizer 2.
    /// </summary>
    ModOrganizer2,

    /// <summary>
    /// Vortex mod manager.
    /// </summary>
    Vortex
}