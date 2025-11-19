using System;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Patching;

/// <summary>
/// Service for applying xdelta patches to files.
/// </summary>
public interface IXDeltaPatcherService
{
    /// <summary>
    /// Applies a delta patch to a source file to produce an output file.
    /// </summary>
    /// <param name="sourceFilePath">Path to the source file (e.g., current game executable).</param>
    /// <param name="patchFilePath">Path to the xdelta patch file (.xdelta).</param>
    /// <param name="outputFilePath">Path where the patched output should be written.</param>
    /// <param name="progress">Optional progress reporter for operation updates.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the operation.</param>
    /// <returns>A <see cref="PatchResult"/> indicating success or failure.</returns>
    Task<PatchResult> ApplyPatchAsync(
        string sourceFilePath,
        string patchFilePath,
        string outputFilePath,
        IProgress<PatchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the xdelta3 executable.
    /// </summary>
    /// <returns>The path to xdelta3.exe, or null if not found.</returns>
    string? GetXDelta3Path();

    /// <summary>
    /// Checks if xdelta3.exe is available.
    /// </summary>
    /// <returns>True if xdelta3.exe exists and is accessible, false otherwise.</returns>
    bool IsXDelta3Available();

    /// <summary>
    /// Validates that a patch can be applied to a source file.
    /// Checks file existence, readability, and basic compatibility.
    /// </summary>
    /// <param name="sourceFilePath">Path to the source file.</param>
    /// <param name="patchFilePath">Path to the xdelta patch file.</param>
    /// <returns>A tuple indicating if validation passed and an error message if it failed.</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidatePatchAsync(string sourceFilePath, string patchFilePath);
}