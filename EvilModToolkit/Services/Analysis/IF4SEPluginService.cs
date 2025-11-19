using System.Collections.Generic;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Analysis;

/// <summary>
/// Service for analyzing F4SE plugin DLLs to determine compatibility.
/// </summary>
public interface IF4SEPluginService
{
    /// <summary>
    /// Analyzes an F4SE plugin DLL file.
    /// </summary>
    /// <param name="filePath">Path to the DLL file.</param>
    /// <returns>Plugin information, or null if the file is invalid or not a DLL.</returns>
    F4SEPluginInfo? AnalyzePlugin(string filePath);

    /// <summary>
    /// Scans a directory for F4SE plugins.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to scan (typically Data/F4SE/Plugins).</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>List of F4SE plugin information.</returns>
    List<F4SEPluginInfo> ScanDirectory(string directoryPath, bool recursive = false);
}
