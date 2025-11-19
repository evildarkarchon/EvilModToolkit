namespace EvilModToolkit.Models;

/// <summary>
/// Represents application settings and user preferences.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the override path to the Fallout 4 installation.
    /// If null or empty, automatic detection is used.
    /// </summary>
    public string? GamePathOverride { get; set; }

    /// <summary>
    /// Gets or sets the override path to Mod Organizer 2.
    /// </summary>
    public string? MO2PathOverride { get; set; }

    /// <summary>
    /// Gets or sets the override path to Vortex.
    /// </summary>
    public string? VortexPathOverride { get; set; }

    /// <summary>
    /// Gets or sets the last scan directory for F4SE plugins.
    /// </summary>
    public string? LastF4SEScanDirectory { get; set; }

    /// <summary>
    /// Gets or sets the last BA2 patch directory.
    /// </summary>
    public string? LastBA2PatchDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to scan F4SE plugins recursively.
    /// </summary>
    public bool ScanF4SERecursively { get; set; } = false;

    /// <summary>
    /// Gets or sets the window width.
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Gets or sets the window height.
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// Gets or sets a value indicating whether to show hidden files in scans.
    /// </summary>
    public bool ShowHiddenFiles { get; set; } = false;

    /// <summary>
    /// Gets or sets the application theme (Light, Dark, System).
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Gets or sets the log level (Debug, Information, Warning, Error).
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}
