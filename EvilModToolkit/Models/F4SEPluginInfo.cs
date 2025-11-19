namespace EvilModToolkit.Models;

/// <summary>
/// Represents information about an F4SE plugin DLL.
/// </summary>
public class F4SePluginInfo
{
    /// <summary>
    /// Gets the file name of the plugin.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full path to the plugin file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this is an F4SE plugin (exports F4SEPlugin_Load).
    /// </summary>
    public bool IsF4SePlugin { get; init; }

    /// <summary>
    /// Gets a value indicating whether this plugin supports Original Game (exports F4SEPlugin_Query).
    /// </summary>
    public bool SupportsOg { get; init; }

    /// <summary>
    /// Gets a value indicating whether this plugin supports Next Gen (exports F4SEPlugin_Version).
    /// </summary>
    public bool SupportsNg { get; init; }

    /// <summary>
    /// Gets the compatibility status of the plugin.
    /// </summary>
    public F4SeCompatibility Compatibility { get; init; }

    /// <summary>
    /// Gets the file version if available.
    /// </summary>
    public string? Version { get; init; }
}

/// <summary>
/// Represents the F4SE compatibility status of a plugin.
/// </summary>
public enum F4SeCompatibility
{
    /// <summary>
    /// Not an F4SE plugin.
    /// </summary>
    NotF4SePlugin,

    /// <summary>
    /// Supports both Original Game and Next Gen.
    /// </summary>
    Universal,

    /// <summary>
    /// Only supports Original Game (pre-NG).
    /// </summary>
    OgOnly,

    /// <summary>
    /// Only supports Next Gen.
    /// </summary>
    NgOnly,

    /// <summary>
    /// F4SE plugin but compatibility cannot be determined.
    /// </summary>
    Unknown
}
