using System;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Patching;

/// <summary>
/// Service for manipulating BA2 archive files (version patching).
/// </summary>
public interface IBA2ArchiveService
{
    /// <summary>
    /// Gets information about a BA2 archive file.
    /// </summary>
    /// <param name="filePath">Path to the BA2 file.</param>
    /// <returns>Archive information, or null if the file is invalid.</returns>
    BA2ArchiveInfo? GetArchiveInfo(string filePath);

    /// <summary>
    /// Patches a BA2 archive to the specified version.
    /// </summary>
    /// <param name="filePath">Path to the BA2 file to patch.</param>
    /// <param name="targetVersion">Target version to patch to.</param>
    /// <returns>True if patching succeeded, false otherwise.</returns>
    bool PatchArchiveVersion(string filePath, BA2Version targetVersion);

    /// <summary>
    /// Checks if a BA2 file is valid.
    /// </summary>
    /// <param name="filePath">Path to check.</param>
    /// <returns>True if the file is a valid BA2 archive.</returns>
    bool IsValidBA2(string filePath);

    /// <summary>
    /// Gets all BA2 files in a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory.</param>
    /// <param name="includeSubdirectories">Whether to search subdirectories.</param>
    /// <returns>Array of BA2 file paths.</returns>
    string[] GetBA2FilesInDirectory(string directoryPath, bool includeSubdirectories = false);

    /// <summary>
    /// Patches all BA2 archives in a directory to the specified version.
    /// </summary>
    /// <param name="directoryPath">Path to the directory containing BA2 files.</param>
    /// <param name="targetVersion">Target version to patch to.</param>
    /// <param name="includeSubdirectories">Whether to search subdirectories.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Summary of the batch patching operation.</returns>
    Task<BatchPatchSummary> BatchPatchDirectoryAsync(
        string directoryPath,
        BA2Version targetVersion,
        bool includeSubdirectories = false,
        IProgress<PatchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}