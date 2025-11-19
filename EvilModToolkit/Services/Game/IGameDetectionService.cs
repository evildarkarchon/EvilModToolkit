using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Game;

/// <summary>
/// Service for detecting Fallout 4 installations and gathering game information.
/// </summary>
public interface IGameDetectionService
{
    /// <summary>
    /// Detects the Fallout 4 installation and returns game information.
    /// </summary>
    /// <returns>Game information if found, otherwise a GameInfo with IsInstalled = false.</returns>
    GameInfo DetectGame();

    /// <summary>
    /// Checks if a path is a valid Fallout 4 installation.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if the path contains a valid Fallout 4 installation.</returns>
    bool IsValidGamePath(string path);
}