using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Game;

/// <summary>
/// Service for detecting and integrating with mod managers (MO2, Vortex).
/// </summary>
public interface IModManagerService
{
    /// <summary>
    /// Detects the mod manager used to launch the application.
    /// </summary>
    /// <returns>Information about the detected mod manager, or Type=None if not launched from a mod manager.</returns>
    Task<ModManagerInfo> DetectModManagerAsync();

    /// <summary>
    /// Finds Mod Organizer 2 installation on the system.
    /// </summary>
    /// <returns>Path to ModOrganizer.exe if found, null otherwise.</returns>
    string? FindMO2Installation();

    /// <summary>
    /// Finds Vortex installation on the system.
    /// </summary>
    /// <returns>Path to Vortex.exe if found, null otherwise.</returns>
    string? FindVortexInstallation();

    /// <summary>
    /// Parses MO2 configuration from ModOrganizer.ini.
    /// </summary>
    /// <param name="iniPath">Path to ModOrganizer.ini file.</param>
    /// <returns>Parsed MO2 settings.</returns>
    MO2Settings ParseMO2Config(string iniPath);
}
