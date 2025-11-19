namespace EvilModToolkit.Models;

/// <summary>
/// Represents the type of a BA2 archive file.
/// </summary>
public enum BA2Type
{
    /// <summary>
    /// Unknown or invalid type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// General archive (GNRL) - used for non-texture files.
    /// </summary>
    General = 1,

    /// <summary>
    /// Texture archive (DX10) - used for texture files.
    /// </summary>
    Texture = 2
}
