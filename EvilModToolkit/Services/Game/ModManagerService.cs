using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace EvilModToolkit.Services.Game;

/// <summary>
/// Service for detecting and integrating with mod managers.
/// </summary>
public class ModManagerService : IModManagerService
{
    private readonly ILogger<ModManagerService> _logger;
    private readonly IProcessService _processService;
    private readonly IFileVersionService _fileVersionService;

    public ModManagerService(
        ILogger<ModManagerService> logger,
        IProcessService processService,
        IFileVersionService fileVersionService)
    {
        _logger = logger;
        _processService = processService;
        _fileVersionService = fileVersionService;
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public Task<ModManagerInfo> DetectModManagerAsync()
    {
        try
        {
            // First, check if we were launched from a mod manager via process tree
            var processInfo = _processService.FindModManager();

            if (processInfo != null)
            {
                switch (processInfo.Type)
                {
                    case ModManagerType.ModOrganizer2:
                        return Task.FromResult(DetectMo2FromProcess(processInfo));
                    case ModManagerType.Vortex:
                        return Task.FromResult(DetectVortexFromProcess(processInfo));
                }
            }

            // Not launched from mod manager, but check if they're installed
            var mo2Path = FindMO2Installation();
            if (!string.IsNullOrEmpty(mo2Path))
            {
                _logger.LogInformation("MO2 found but not launched from it: {Path}", mo2Path);
            }

            var vortexPath = FindVortexInstallation();
            if (!string.IsNullOrEmpty(vortexPath))
            {
                _logger.LogInformation("Vortex found but not launched from it: {Path}", vortexPath);
            }

            // Return "None" - standalone usage
            return Task.FromResult(new ModManagerInfo
            {
                Type = ModManagerType.None,
                ExecutablePath = string.Empty,
                Version = "N/A"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting mod manager");
            return Task.FromResult(new ModManagerInfo
            {
                Type = ModManagerType.None,
                ExecutablePath = string.Empty,
                Version = "N/A"
            });
        }
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public virtual string? FindMO2Installation()
    {
        try
        {
            // Check common registry locations
            var registryPaths = new[]
            {
                @"SOFTWARE\Mod Organizer 2",
                @"SOFTWARE\WOW6432Node\Mod Organizer 2"
            };

            foreach (var regPath in registryPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    var installPath = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var exePath = Path.Combine(installPath, "ModOrganizer.exe");
                        if (File.Exists(exePath))
                        {
                            _logger.LogDebug("Found MO2 via registry: {Path}", exePath);
                            return exePath;
                        }
                    }
                }
            }

            // Check HKEY_CURRENT_USER
            using var hkcuKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Mod Organizer 2");
            if (hkcuKey != null)
            {
                var installPath = hkcuKey.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(installPath))
                {
                    var exePath = Path.Combine(installPath, "ModOrganizer.exe");
                    if (File.Exists(exePath))
                    {
                        _logger.LogDebug("Found MO2 via HKCU registry: {Path}", exePath);
                        return exePath;
                    }
                }
            }

            _logger.LogDebug("MO2 not found in registry");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for MO2 installation");
            return null;
        }
    }

    /// <inheritdoc />
    public virtual string? FindVortexInstallation()
    {
        try
        {
            // Vortex is typically installed in %APPDATA%\Vortex
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var vortexPath = Path.Combine(appData, "Vortex", "Vortex.exe");

            if (File.Exists(vortexPath))
            {
                _logger.LogDebug("Found Vortex: {Path}", vortexPath);
                return vortexPath;
            }

            // Check Program Files
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var vortexProgramFiles = Path.Combine(programFiles, "Black Tree Gaming Ltd", "Vortex", "Vortex.exe");

            if (File.Exists(vortexProgramFiles))
            {
                _logger.LogDebug("Found Vortex in Program Files: {Path}", vortexProgramFiles);
                return vortexProgramFiles;
            }

            _logger.LogDebug("Vortex not found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching for Vortex installation");
            return null;
        }
    }

    /// <inheritdoc />
    public MO2Settings ParseMO2Config(string iniPath)
    {
        if (!File.Exists(iniPath))
        {
            throw new FileNotFoundException($"MO2 configuration file not found: {iniPath}");
        }

        var settings = new MO2Settings();
        var defaultBaseDir = Path.GetDirectoryName(iniPath) ?? string.Empty;
        settings.BaseDirectory = defaultBaseDir;

        try
        {
            var lines = File.ReadAllLines(iniPath);
            string? currentSection = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed;
                    continue;
                }

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                {
                    continue;
                }

                // Parse key=value
                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex < 0)
                {
                    continue;
                }

                var key = trimmed.Substring(0, equalIndex).Trim();
                var value = trimmed.Substring(equalIndex + 1).Trim();

                // Remove @ByteArray() wrapper if present
                if (value.StartsWith("@ByteArray(") && value.EndsWith(")"))
                {
                    value = value.Substring(11, value.Length - 12);
                }

                // Parse based on section
                if (currentSection == "[General]")
                {
                    ParseGeneralSection(key, value, settings);
                }
                else if (currentSection == "[Settings]")
                {
                    ParseSettingsSection(key, value, settings);
                }
                else if (currentSection == "[customExecutables]")
                {
                    ParseCustomExecutables(key, value, settings);
                }
            }

            // Validate
            if (settings.GameName != "Fallout 4")
            {
                _logger.LogWarning("MO2 configured for {GameName}, not Fallout 4", settings.GameName);
            }

            _logger.LogInformation("Parsed MO2 config from {IniPath}", iniPath);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MO2 configuration: {IniPath}", iniPath);
            throw;
        }
    }

    private void ParseGeneralSection(string key, string value, MO2Settings settings)
    {
        switch (key)
        {
            case "gameName":
                settings.GameName = value;
                break;
            case "gamePath":
                settings.GamePath = value;
                break;
            case "selected_profile":
                settings.SelectedProfile = value;
                break;
        }
    }

    private void ParseSettingsSection(string key, string value, MO2Settings settings)
    {
        switch (key)
        {
            case "base_directory":
                settings.BaseDirectory = value;
                break;
            case "cache_directory":
                settings.CacheDirectory = ExpandPath(value, settings.BaseDirectory ?? string.Empty);
                break;
            case "download_directory":
                settings.DownloadDirectory = ExpandPath(value, settings.BaseDirectory ?? string.Empty);
                break;
            case "mod_directory":
                settings.ModDirectory = ExpandPath(value, settings.BaseDirectory ?? string.Empty);
                break;
            case "overwrite_directory":
                settings.OverwriteDirectory = ExpandPath(value, settings.BaseDirectory ?? string.Empty);
                break;
            case "profiles_directory":
                settings.ProfilesDirectory = ExpandPath(value, settings.BaseDirectory ?? string.Empty);
                break;
            case "profile_local_inis":
                settings.ProfileLocalInis = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                break;
            case "profile_local_saves":
                settings.ProfileLocalSaves = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                break;
            case "skip_file_suffixes":
                settings.SkipFileSuffixes = ParseCsvList(value);
                break;
            case "skip_directories":
                settings.SkipDirectories = new HashSet<string>(ParseCsvList(value), StringComparer.OrdinalIgnoreCase);
                break;
        }
    }

    private void ParseCustomExecutables(string key, string value, MO2Settings settings)
    {
        // Custom executables are stored as "size\\1\\binary=path"
        if (key.EndsWith("\\binary"))
        {
            var toolName = ExtractToolName(value);
            if (!string.IsNullOrEmpty(toolName))
            {
                if (!settings.CustomExecutables.ContainsKey(toolName))
                {
                    settings.CustomExecutables[toolName] = new List<string>();
                }

                settings.CustomExecutables[toolName].Add(value);
            }
        }
    }

    private string ExpandPath(string path, string baseDir)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Replace %BASE_DIR% with actual base directory
        if (path.Contains("%BASE_DIR%"))
        {
            return path.Replace("%BASE_DIR%", baseDir);
        }

        // If path is relative, make it absolute from base directory
        if (!Path.IsPathRooted(path))
        {
            return Path.Combine(baseDir, path);
        }

        return path;
    }

    private List<string> ParseCsvList(string csvValue)
    {
        if (string.IsNullOrEmpty(csvValue))
        {
            return new List<string>();
        }

        // Simple CSV parsing - split by comma, trim whitespace
        return csvValue.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    private string? ExtractToolName(string exePath)
    {
        if (string.IsNullOrEmpty(exePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(exePath).ToLowerInvariant();

        // Common modding tools
        if (fileName.Contains("xedit") || fileName.Contains("fo4edit"))
        {
            return "xEdit";
        }
        else if (fileName.Contains("bsarch"))
        {
            return "BSArch";
        }
        else if (fileName.Contains("wrye") && fileName.Contains("bash"))
        {
            return "WryeBash";
        }
        else if (fileName.Contains("loot"))
        {
            return "LOOT";
        }

        return null;
    }

    private ModManagerInfo DetectMo2FromProcess(ModManagerInfo processInfo)
    {
        try
        {
            var exePath = processInfo.ExecutablePath;
            var workingDir = processInfo.WorkingDirectory;

            // Get version info
            var versionInfo = _fileVersionService.GetFileVersion(exePath);
            var version = versionInfo?.FileVersion ?? "Unknown";

            // Find ModOrganizer.ini
            var iniPath = FindMo2IniFile(workingDir);

            MO2Settings? mo2Settings = null;
            string? gamePath = null;
            string? selectedProfile = null;
            bool isPortable = false;

            if (!string.IsNullOrEmpty(iniPath))
            {
                mo2Settings = ParseMO2Config(iniPath);
                gamePath = mo2Settings.GamePath;
                selectedProfile = mo2Settings.SelectedProfile;

                // Check if portable
                var portableTxt = Path.Combine(workingDir, "portable.txt");
                isPortable = File.Exists(portableTxt);
            }

            return new ModManagerInfo
            {
                Type = ModManagerType.ModOrganizer2,
                ExecutablePath = exePath,
                Version = version,
                ProcessId = processInfo.ProcessId,
                WorkingDirectory = workingDir,
                ConfigFilePath = iniPath,
                IsPortable = isPortable,
                MO2Settings = mo2Settings,
                GamePath = gamePath,
                SelectedProfile = selectedProfile
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting MO2 from process");
            return processInfo; // Return basic process info
        }
    }

    private ModManagerInfo DetectVortexFromProcess(ModManagerInfo processInfo)
    {
        try
        {
            var exePath = processInfo.ExecutablePath;
            var versionInfo = _fileVersionService.GetFileVersion(exePath);
            var version = versionInfo?.FileVersion ?? "Unknown";

            return new ModManagerInfo
            {
                Type = ModManagerType.Vortex,
                ExecutablePath = exePath,
                Version = version,
                ProcessId = processInfo.ProcessId,
                WorkingDirectory = processInfo.WorkingDirectory
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting Vortex from process");
            return processInfo;
        }
    }

    private string? FindMo2IniFile(string workingDir)
    {
        // Check for portable installation (ModOrganizer.ini in working directory)
        var portableIni = Path.Combine(workingDir, "ModOrganizer.ini");
        if (File.Exists(portableIni))
        {
            return portableIni;
        }

        // Check AppData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataIni = Path.Combine(appData, "ModOrganizer", "Fallout 4", "ModOrganizer.ini");
        if (File.Exists(appDataIni))
        {
            return appDataIni;
        }

        _logger.LogWarning("ModOrganizer.ini not found");
        return null;
    }
}
