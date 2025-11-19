namespace EvilModToolkit.Models;

/// <summary>
/// Represents the result of a patch operation.
/// </summary>
public class PatchResult
{
    /// <summary>
    /// Gets a value indicating whether the patch was applied successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the patch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the path to the output file if the patch succeeded.
    /// </summary>
    public string? OutputFilePath { get; init; }

    /// <summary>
    /// Gets the exit code from the xdelta3 process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output from xdelta3.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Gets the standard error output from xdelta3.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;
}