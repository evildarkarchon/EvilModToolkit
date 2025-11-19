namespace EvilModToolkit.Models;

/// <summary>
/// Represents the progress state of a patch operation.
/// </summary>
public class PatchProgress
{
    /// <summary>
    /// Gets the current operation stage.
    /// </summary>
    public PatchStage Stage { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100), if available.
    /// Note: xdelta3 does not provide detailed progress, so this is typically 0 or 100.
    /// </summary>
    public int Percentage { get; init; }

    /// <summary>
    /// Gets a descriptive message about the current progress.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Represents the stages of a patch operation.
/// </summary>
public enum PatchStage
{
    /// <summary>
    /// Patch operation is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Patch operation is in progress.
    /// </summary>
    Patching,

    /// <summary>
    /// Patch operation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Patch operation failed.
    /// </summary>
    Failed
}
