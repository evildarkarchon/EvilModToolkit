namespace EvilModToolkit.Models;

/// <summary>
/// Represents metadata about an xdelta patch file for game downgrade/upgrade.
/// </summary>
public class PatchInfo
{
    /// <summary>
    /// Path to the .xdelta patch file
    /// </summary>
    public string PatchFilePath { get; init; } = string.Empty;

    /// <summary>
    /// Expected source file version (e.g., "1.10.163.0")
    /// </summary>
    public string? SourceVersion { get; init; }

    /// <summary>
    /// Resulting target version after patching (e.g., "1.10.138.0")
    /// </summary>
    public string? TargetVersion { get; init; }

    /// <summary>
    /// Patch file size in bytes
    /// </summary>
    public long PatchFileSizeBytes { get; init; }

    /// <summary>
    /// Human-readable description or notes about the patch
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the patch file exists and is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Patch file name without path
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(PatchFilePath);

    /// <summary>
    /// Patch file size in megabytes (for display)
    /// </summary>
    public double PatchFileSizeMb => PatchFileSizeBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Creates a new PatchInfo instance
    /// </summary>
    public PatchInfo(
        string patchFilePath,
        bool isValid = false,
        string? sourceVersion = null,
        string? targetVersion = null,
        long patchFileSizeBytes = 0,
        string? description = null)
    {
        PatchFilePath = patchFilePath;
        IsValid = isValid;
        SourceVersion = sourceVersion;
        TargetVersion = targetVersion;
        PatchFileSizeBytes = patchFileSizeBytes;
        Description = description;
    }

    /// <summary>
    /// Validates the patch info for consistency
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(PatchFilePath))
            return false;

        if (PatchFileSizeBytes < 0)
            return false;

        return true;
    }
}
