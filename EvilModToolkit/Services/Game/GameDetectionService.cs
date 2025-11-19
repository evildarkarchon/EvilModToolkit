using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace EvilModToolkit.Services.Game;

/// <summary>
/// Service for detecting Fallout 4 installations via registry and file system.
/// </summary>
public class GameDetectionService : IGameDetectionService
{
    private const string GameExecutable = "Fallout4.exe";
    private const string NextGenVersionThreshold = "1.10.980";

    private readonly ILogger<GameDetectionService> _logger;
    private readonly IFileVersionService _fileVersionService;

    // Known DLC ESM files
    private static readonly string[] KnownDLCs = new[]
    {
        "DLCRobot.esm",
        "DLCworkshop01.esm",
        "DLCCoast.esm",
        "DLCworkshop02.esm",
        "DLCworkshop03.esm",
        "DLCNukaWorld.esm"
    };

    public GameDetectionService(ILogger<GameDetectionService> logger, IFileVersionService fileVersionService)
    {
        _logger = logger;
        _fileVersionService = fileVersionService;
    }

    /// <inheritdoc />
    public GameInfo DetectGame()
    {
        try
        {
            // Try detection methods in order of likelihood
            var installPath = TryDetectSteam() ?? TryDetectGOG() ?? TryDetectMicrosoftStore();

            if (string.IsNullOrEmpty(installPath) || !IsValidGamePath(installPath))
            {
                _logger.LogWarning("Fallout 4 installation not found");
                return new GameInfo { IsInstalled = false };
            }

            return BuildGameInfo(installPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during game detection");
            return new GameInfo { IsInstalled = false };
        }
    }

    /// <inheritdoc />
    public bool IsValidGamePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        var exePath = Path.Combine(path, GameExecutable);
        return File.Exists(exePath);
    }

    private string? TryDetectSteam()
    {
        try
        {
            // Try HKEY_LOCAL_MACHINE first
            var path = ReadRegistryValue(
                Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\Bethesda Softworks\Fallout4",
                "installed path");

            if (!string.IsNullOrEmpty(path) && IsValidGamePath(path))
            {
                _logger.LogInformation("Found Steam installation (HKLM): {Path}", path);
                return path;
            }

            // Try HKEY_CURRENT_USER
            path = ReadRegistryValue(
                Registry.CurrentUser,
                @"SOFTWARE\Bethesda Softworks\Fallout4",
                "installed path");

            if (!string.IsNullOrEmpty(path) && IsValidGamePath(path))
            {
                _logger.LogInformation("Found Steam installation (HKCU): {Path}", path);
                return path;
            }

            // Try parsing Steam library folders (if needed)
            // This would require VDF parsing - implement if needed

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Steam installation");
            return null;
        }
    }

    private string? TryDetectGOG()
    {
        try
        {
            var path = ReadRegistryValue(
                Registry.LocalMachine,
                @"SOFTWARE\WOW6432Node\GOG.com\Games\1998527297",
                "path");

            if (!string.IsNullOrEmpty(path) && IsValidGamePath(path))
            {
                _logger.LogInformation("Found GOG installation: {Path}", path);
                return path;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect GOG installation");
            return null;
        }
    }

    private string? TryDetectMicrosoftStore()
    {
        try
        {
            // Microsoft Store games are typically in WindowsApps folder
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var possiblePaths = new[]
            {
                Path.Combine(programFiles, @"WindowsApps\BethesdaSoftworks.Fallout4"),
                Path.Combine(programFiles, @"ModifiableWindowsApps\Fallout4")
            };

            foreach (var basePath in possiblePaths)
            {
                if (Directory.Exists(basePath))
                {
                    // MS Store might have version-suffixed folders
                    var dirs = Directory.GetDirectories(Path.GetDirectoryName(basePath) ?? string.Empty,
                        Path.GetFileName(basePath) + "*");

                    foreach (var dir in dirs)
                    {
                        if (IsValidGamePath(dir))
                        {
                            _logger.LogInformation("Found Microsoft Store installation: {Path}", dir);
                            return dir;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect Microsoft Store installation");
            return null;
        }
    }

    private GameInfo BuildGameInfo(string installPath)
    {
        var exePath = Path.Combine(installPath, GameExecutable);
        var dataPath = Path.Combine(installPath, "Data");
        var myGamesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games", "Fallout4");

        var versionInfo = _fileVersionService.GetFileVersion(exePath);
        var version = versionInfo?.Version ?? "Unknown";
        var isNextGen = IsNextGenVersion(version);

        return new GameInfo
        {
            IsInstalled = true,
            InstallPath = installPath,
            InstallType = DetermineInstallType(installPath),
            Version = version,
            IsNextGen = isNextGen,
            ExecutablePath = exePath,
            DataPath = dataPath,
            InstalledDLCs = DetectInstalledDLCs(dataPath),
            Language = DetectLanguage(myGamesPath),
            IniPath = Path.Combine(myGamesPath, "Fallout4.ini"),
            PrefsIniPath = Path.Combine(myGamesPath, "Fallout4Prefs.ini"),
            CustomIniPath = Path.Combine(myGamesPath, "Fallout4Custom.ini")
        };
    }

    private bool IsNextGenVersion(string version)
    {
        try
        {
            var currentVersion = new Version(version);
            var threshold = new Version(NextGenVersionThreshold);
            return currentVersion >= threshold;
        }
        catch
        {
            return false;
        }
    }

    private InstallType DetermineInstallType(string installPath)
    {
        if (installPath.Contains("steamapps", StringComparison.OrdinalIgnoreCase))
            return InstallType.Steam;

        if (installPath.Contains("GOG", StringComparison.OrdinalIgnoreCase))
            return InstallType.GOG;

        if (installPath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase) ||
            installPath.Contains("ModifiableWindowsApps", StringComparison.OrdinalIgnoreCase))
            return InstallType.MicrosoftStore;

        return InstallType.Unknown;
    }

    private List<string> DetectInstalledDLCs(string dataPath)
    {
        var installedDLCs = new List<string>();

        if (!Directory.Exists(dataPath))
            return installedDLCs;

        foreach (var dlc in KnownDLCs)
        {
            var dlcPath = Path.Combine(dataPath, dlc);
            if (File.Exists(dlcPath))
            {
                installedDLCs.Add(dlc);
            }
        }

        return installedDLCs;
    }

    private string DetectLanguage(string myGamesPath)
    {
        try
        {
            var iniPath = Path.Combine(myGamesPath, "Fallout4.ini");
            if (!File.Exists(iniPath))
                return "en"; // Default to English

            var lines = File.ReadAllLines(iniPath);
            foreach (var line in lines)
            {
                if (line.StartsWith("sLanguage=", StringComparison.OrdinalIgnoreCase))
                {
                    var language = line.Substring("sLanguage=".Length).Trim();
                    return language.ToLowerInvariant();
                }
            }

            return "en";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect language from INI");
            return "en";
        }
    }

    private string? ReadRegistryValue(RegistryKey root, string keyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(keyPath);
            return key?.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
