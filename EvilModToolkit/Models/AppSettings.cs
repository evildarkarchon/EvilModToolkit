using System;
using System.Collections.Generic;
using System.IO;

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

    /// <summary>
    /// Valid theme values
    /// </summary>
    private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Light", "Dark", "System"
    };

    /// <summary>
    /// Valid log level values
    /// </summary>
    private static readonly HashSet<string> ValidLogLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"
    };

    /// <summary>
    /// Minimum reasonable window dimension
    /// </summary>
    private const double MinWindowDimension = 400;

    /// <summary>
    /// Maximum reasonable window dimension
    /// </summary>
    private const double MaxWindowDimension = 8192;

    /// <summary>
    /// Validates the settings for consistency and correctness.
    /// </summary>
    /// <returns>A list of validation error messages, or empty list if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate window dimensions
        if (WindowWidth < MinWindowDimension || WindowWidth > MaxWindowDimension)
            errors.Add($"Window width must be between {MinWindowDimension} and {MaxWindowDimension}");

        if (WindowHeight < MinWindowDimension || WindowHeight > MaxWindowDimension)
            errors.Add($"Window height must be between {MinWindowDimension} and {MaxWindowDimension}");

        // Validate theme
        if (!ValidThemes.Contains(Theme))
            errors.Add($"Theme must be one of: {string.Join(", ", ValidThemes)}");

        // Validate log level
        if (!ValidLogLevels.Contains(LogLevel))
            errors.Add($"LogLevel must be one of: {string.Join(", ", ValidLogLevels)}");

        // Validate path overrides if set
        if (!string.IsNullOrWhiteSpace(GamePathOverride) && !Directory.Exists(GamePathOverride))
            errors.Add($"Game path override does not exist: {GamePathOverride}");

        if (!string.IsNullOrWhiteSpace(MO2PathOverride) && !File.Exists(MO2PathOverride) && !Directory.Exists(MO2PathOverride))
            errors.Add($"MO2 path override does not exist: {MO2PathOverride}");

        if (!string.IsNullOrWhiteSpace(VortexPathOverride) && !File.Exists(VortexPathOverride) && !Directory.Exists(VortexPathOverride))
            errors.Add($"Vortex path override does not exist: {VortexPathOverride}");

        return errors;
    }

    /// <summary>
    /// Checks if the settings are valid.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid() => Validate().Count == 0;

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        GamePathOverride = null;
        MO2PathOverride = null;
        VortexPathOverride = null;
        LastF4SEScanDirectory = null;
        LastBA2PatchDirectory = null;
        ScanF4SERecursively = false;
        WindowWidth = 1200;
        WindowHeight = 800;
        ShowHiddenFiles = false;
        Theme = "System";
        LogLevel = "Information";
    }
}
