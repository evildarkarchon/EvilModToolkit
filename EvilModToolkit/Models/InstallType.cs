namespace EvilModToolkit.Models;

/// <summary>
/// Represents the type of Fallout 4 installation.
/// </summary>
public enum InstallType
{
    /// <summary>
    /// Installation type could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Steam installation.
    /// </summary>
    Steam,

    /// <summary>
    /// GOG Galaxy installation.
    /// </summary>
    GOG,

    /// <summary>
    /// Microsoft Store / Xbox Game Pass installation.
    /// </summary>
    MicrosoftStore
}