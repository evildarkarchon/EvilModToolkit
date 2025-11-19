namespace EvilModToolkit.Models;

/// <summary>
/// Represents information about a BA2 archive file.
/// </summary>
public class BA2ArchiveInfo
{
    /// <summary>
    /// Gets the path to the BA2 file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this is a valid BA2 file.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the BA2 version.
    /// </summary>
    public BA2Version Version { get; init; }

    /// <summary>
    /// Gets the BA2 archive type (General or Texture).
    /// </summary>
    public BA2Type Type { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }
}