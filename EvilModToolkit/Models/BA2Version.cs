namespace EvilModToolkit.Models;

/// <summary>
/// Represents the version of a BA2 archive file.
/// </summary>
public enum BA2Version
{
    /// <summary>
    /// Unknown or invalid version.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Version 1 (Original Game / pre-NG).
    /// </summary>
    V1 = 0x01,

    /// <summary>
    /// Version 7 (Next Gen).
    /// </summary>
    V7 = 0x07,

    /// <summary>
    /// Version 8 (Next Gen).
    /// </summary>
    V8 = 0x08
}