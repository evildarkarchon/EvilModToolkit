using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Analysis;

/// <summary>
/// Service for scanning mod installations for issues.
/// </summary>
public class ModScannerService : IModScannerService
{
    private readonly ILogger<ModScannerService> _logger;

    // Constants from scan_settings.py
    private static readonly HashSet<string> JunkFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "desktop.ini", "thumbs.db", ".ds_store"
    };

    private static readonly HashSet<string> JunkFileSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".pdf", ".doc", ".docx", ".rtf", ".jpg", ".png", ".bmp", ".gif", ".zip", ".rar", ".7z", ".tar", ".gz"
    };

    private static readonly Dictionary<string, HashSet<string>> DataWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        { "f4se", new HashSet<string> { "f4se" } },
        { "music", new HashSet<string> { "music" } },
        { "sound", new HashSet<string> { "sound" } },
        { "interface", new HashSet<string> { "interface" } },
        { "meshes", new HashSet<string> { "meshes" } },
        { "textures", new HashSet<string> { "textures" } },
        { "materials", new HashSet<string> { "materials" } },
        { "programs", new HashSet<string> { "programs" } },
        { "video", new HashSet<string> { "video" } },
        { "vis", new HashSet<string> { "vis" } },
        { "scripts", new HashSet<string> { "scripts" } },
        { "shadersfx", new HashSet<string> { "shadersfx" } },
        { "mcm", new HashSet<string> { "mcm" } },
        { "lod", new HashSet<string> { "lod" } }
    };

    private static readonly Dictionary<string, string[]> ProperFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        { "music", new[] { "xwm", "wav", "mp3" } },
        { "sound", new[] { "xwm", "wav", "mp3" } },
        { "interface", new[] { "swf" } },
        { "meshes", new[] { "nif", "tri" } },
        { "textures", new[] { "dds" } },
        { "materials", new[] { "bgsm", "bgem" } },
        { "programs", new[] { "swf" } },
        { "video", new[] { "bk2" } },
        { "vis", new[] { "txt" } },
        { "scripts", new[] { "pex" } },
        { "shadersfx", new[] { "fx" } },
        { "mcm", new[] { "json" } }
    };

    public ModScannerService(ILogger<ModScannerService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ScanResult>> ScanAsync(
        GameInfo gameInfo,
        ModManagerInfo modManagerInfo,
        ScanOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ScanResult>();
        
        if (string.IsNullOrEmpty(gameInfo.DataPath) || !Directory.Exists(gameInfo.DataPath))
        {
            _logger.LogWarning("Game Data path not found or invalid: {Path}", gameInfo.DataPath);
            return results;
        }

        if (options.SkipDataScan)
        {
            return results;
        }

        await Task.Run(() =>
        {
            try
            {
                progress?.Report("Scanning Data folder...");
                ScanDirectory(gameInfo.DataPath, gameInfo.DataPath, results, options, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scan cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mod scan");
                results.Add(new ScanResult(
                    ProblemType.UnexpectedFormat, // Using generic type for scan error
                    "Scan Error",
                    "",
                    $"An error occurred during the scan: {ex.Message}",
                    SeverityLevel.Error
                ));
            }
        }, cancellationToken).ConfigureAwait(false);

        return results;
    }

    private void ScanDirectory(
        string currentPath,
        string rootDataPath,
        List<ScanResult> results,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Get relative path from Data folder
        var relativePath = Path.GetRelativePath(rootDataPath, currentPath);
        var isRoot = currentPath.Equals(rootDataPath, StringComparison.OrdinalIgnoreCase);
        var dirName = Path.GetFileName(currentPath);
        
        // Check folders
        if (!isRoot)
        {
            // Check if this top-level folder is whitelisted
            if (Path.GetDirectoryName(relativePath) == "." || string.IsNullOrEmpty(Path.GetDirectoryName(relativePath)))
            {
                 if (!DataWhitelist.ContainsKey(dirName))
                 {
                     // Not in whitelist, but check if it's a known junk folder
                     if (options.ScanJunkFiles && dirName.Equals("fomod", StringComparison.OrdinalIgnoreCase))
                     {
                         results.Add(new ScanResult(
                             ProblemType.JunkFile,
                             currentPath,
                             relativePath,
                             "This is a junk folder not used by the game or mod managers.",
                             SeverityLevel.Info,
                             solution: SolutionType.DeleteOrIgnoreFolder
                         ));
                         return; // Don't scan inside junk folders
                     }
                     
                     // Could add check for other non-whitelisted folders here
                 }
            }

            // Loose Previs check
            if (options.ScanLoosePrevis)
            {
                if (dirName.Equals("vis", StringComparison.OrdinalIgnoreCase) && Path.GetDirectoryName(relativePath) == ".")
                {
                     results.Add(new ScanResult(
                         ProblemType.LoosePrevis,
                         currentPath,
                         relativePath,
                         "Loose previs files should be archived so they only win conflicts according to their plugin's load order.",
                         SeverityLevel.Warning,
                         solution: SolutionType.ArchiveOrDeleteFolder
                     ));
                }
                if (dirName.Equals("precombined", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ScanResult(
                        ProblemType.LoosePrevis,
                        currentPath,
                        relativePath,
                        "Loose previs files should be archived so they only win conflicts according to their plugin's load order.",
                        SeverityLevel.Warning,
                        solution: SolutionType.ArchiveOrDeleteFolder
                    ));
                }
            }

            // Problem overrides
            if (options.ScanProblemOverrides)
            {
                if (dirName.Equals("AnimTextData", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new ScanResult(
                        ProblemType.AnimTextDataFolder,
                        currentPath,
                        relativePath,
                        "The existence of unpacked AnimTextData may cause the game to crash.",
                        SeverityLevel.Error,
                        solution: SolutionType.ArchiveOrDeleteFolder
                    ));
                }
            }
        }

        // Files
        try
        {
            foreach (var file in Directory.GetFiles(currentPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file);
                var fileRelativePath = Path.GetRelativePath(rootDataPath, file);

                // Junk Files
                if (options.ScanJunkFiles)
                {
                    if (JunkFiles.Contains(fileName) || JunkFileSuffixes.Contains(extension))
                    {
                        // Skip if it's in a specific allowed context (simplified for now)
                        // Python code checked strict junk files.
                        results.Add(new ScanResult(
                            ProblemType.JunkFile,
                            file,
                            fileRelativePath,
                            "This is a junk file not used by the game or mod managers.",
                            SeverityLevel.Info,
                            solution: SolutionType.DeleteOrIgnoreFile
                        ));
                        continue;
                    }
                }

                // Check format
                if (options.ScanWrongFormat && !string.IsNullOrEmpty(extension))
                {
                    var extNoDot = extension.TrimStart('.');
                    var topFolder = fileRelativePath.Split(Path.DirectorySeparatorChar)[0];
                    
                    if (ProperFormats.TryGetValue(topFolder, out var validFormats))
                    {
                        if (!validFormats.Contains(extNoDot, StringComparer.OrdinalIgnoreCase))
                        {
                            // Allowed exceptions
                            if (topFolder.Equals("f4se", StringComparison.OrdinalIgnoreCase) && extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
                            {
                                // This is fine (plugins)
                            }
                            else
                            {
                                results.Add(new ScanResult(
                                    ProblemType.UnexpectedFormat,
                                    file,
                                    fileRelativePath,
                                    $"Format '{extension}' not in whitelist for '{topFolder}'. Expected: {string.Join(", ", validFormats)}",
                                    SeverityLevel.Warning,
                                    solution: SolutionType.UnknownFormat
                                ));
                            }
                        }
                    }
                }
                
                // Complex Sorter INI Check
                if (options.ScanErrors && extension.Equals(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    // Simplified check: if path contains "Complex Sorter"
                    if (fileRelativePath.Contains("Complex Sorter", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check content
                        CheckComplexSorterIni(file, fileRelativePath, results);
                    }
                }
            }

            // Recursively scan subdirectories
            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                ScanDirectory(dir, rootDataPath, results, options, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore access denied
        }
        catch (DirectoryNotFoundException)
        {
            // Directory might have been moved/deleted
        }
    }

    private async Task CheckComplexSorterIniAsync(string filePath, string relativePath, List<ScanResult> results)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);
            bool errorFound = false;
            foreach (var line in lines)
            {
                if (!line.TrimStart().StartsWith(";") &&
                    (line.Contains("FindNode OBTS(FindNode \"Addon Index\"") ||
                     line.Contains("FindNode OBTS(FindNode 'Addon Index'")))
                {
                    errorFound = true;
                    break;
                }
            }

            if (errorFound)
            {
                results.Add(new ScanResult(
                    ProblemType.ComplexSorter,
                    filePath,
                    relativePath,
                    "INI uses an outdated field name. xEdit 4.1.5g changed 'Addon Index' to 'Parent Combination Index'.",
                    SeverityLevel.Error,
                    solution: SolutionType.ComplexSorterFix
                ));
            }
        }
        catch
        {
            // Ignore read errors
        }
    }

    /// <summary>
    /// Synchronous wrapper for CheckComplexSorterIniAsync for use in synchronous scan context.
    /// Uses blocking wait since this is called from within Task.Run().
    /// </summary>
    private void CheckComplexSorterIni(string filePath, string relativePath, List<ScanResult> results)
    {
        // Since this is already running on a thread pool thread via Task.Run,
        // we can safely use synchronous file I/O here
        try
        {
            var lines = File.ReadAllLines(filePath);
            bool errorFound = false;
            foreach (var line in lines)
            {
                if (!line.TrimStart().StartsWith(";") &&
                    (line.Contains("FindNode OBTS(FindNode \"Addon Index\"") ||
                     line.Contains("FindNode OBTS(FindNode 'Addon Index'")))
                {
                    errorFound = true;
                    break;
                }
            }

            if (errorFound)
            {
                results.Add(new ScanResult(
                    ProblemType.ComplexSorter,
                    filePath,
                    relativePath,
                    "INI uses an outdated field name. xEdit 4.1.5g changed 'Addon Index' to 'Parent Combination Index'.",
                    SeverityLevel.Error,
                    solution: SolutionType.ComplexSorterFix
                ));
            }
        }
        catch
        {
            // Ignore read errors
        }
    }
}
