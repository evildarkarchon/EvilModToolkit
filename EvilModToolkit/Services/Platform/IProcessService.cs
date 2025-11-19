using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for process detection and parent process navigation.
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// Gets the parent process ID for the specified process.
    /// </summary>
    /// <param name="processId">The process ID to query.</param>
    /// <returns>The parent process ID, or null if not found.</returns>
    int? GetParentProcessId(int processId);

    /// <summary>
    /// Detects if the application was launched from a mod manager by walking the process tree.
    /// </summary>
    /// <returns>Mod manager information if detected, otherwise null.</returns>
    ModManagerInfo? FindModManager();
}