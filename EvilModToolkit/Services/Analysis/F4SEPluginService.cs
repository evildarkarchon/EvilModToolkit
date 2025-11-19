using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using PeNet;

namespace EvilModToolkit.Services.Analysis;

/// <summary>
/// Service for analyzing F4SE plugin DLLs using PeNet for PE export table parsing.
/// </summary>
public class F4SEPluginService : IF4SEPluginService
{
    private const string F4SEPluginLoadExport = "F4SEPlugin_Load";
    private const string F4SEPluginQueryExport = "F4SEPlugin_Query";
    private const string F4SEPluginVersionExport = "F4SEPlugin_Version";

    private readonly ILogger<F4SEPluginService> _logger;
    private readonly IFileVersionService _fileVersionService;

    public F4SEPluginService(ILogger<F4SEPluginService> logger, IFileVersionService fileVersionService)
    {
        _logger = logger;
        _fileVersionService = fileVersionService;
    }

    /// <inheritdoc />
    public F4SEPluginInfo? AnalyzePlugin(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Plugin file not found: {FilePath}", filePath);
                return null;
            }

            if (!filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("File is not a DLL: {FilePath}", filePath);
                return null;
            }

            // Use PeNet to parse the PE file
            var peFile = new PeFile(filePath);

            // Check export table for F4SE exports
            var exports = peFile.ExportedFunctions;
            if (exports == null || !exports.Any())
            {
                _logger.LogDebug("No exports found in DLL: {FilePath}", filePath);
                return CreateNonF4SEPluginInfo(filePath);
            }

            var exportNames = exports.Select(e => e.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();

            var isF4SEPlugin = exportNames.Contains(F4SEPluginLoadExport);
            var supportsOG = exportNames.Contains(F4SEPluginQueryExport);
            var supportsNG = exportNames.Contains(F4SEPluginVersionExport);

            var compatibility = DetermineCompatibility(isF4SEPlugin, supportsOG, supportsNG);

            var version = _fileVersionService.GetFileVersion(filePath)?.Version;

            var pluginInfo = new F4SEPluginInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                IsF4SEPlugin = isF4SEPlugin,
                SupportsOG = supportsOG,
                SupportsNG = supportsNG,
                Compatibility = compatibility,
                Version = version
            };

            if (isF4SEPlugin)
            {
                _logger.LogDebug("F4SE Plugin detected: {FileName}, Compatibility: {Compatibility}",
                    pluginInfo.FileName, compatibility);
            }

            return pluginInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing plugin: {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public List<F4SEPluginInfo> ScanDirectory(string directoryPath, bool recursive = false)
    {
        var plugins = new List<F4SEPluginInfo>();

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory not found: {DirectoryPath}", directoryPath);
                return plugins;
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dllFiles = Directory.GetFiles(directoryPath, "*.dll", searchOption);

            _logger.LogInformation("Scanning {Count} DLL files in {DirectoryPath}", dllFiles.Length, directoryPath);

            foreach (var dllFile in dllFiles)
            {
                var pluginInfo = AnalyzePlugin(dllFile);
                if (pluginInfo != null)
                {
                    plugins.Add(pluginInfo);
                }
            }

            var f4sePluginCount = plugins.Count(p => p.IsF4SEPlugin);
            _logger.LogInformation("Found {F4SECount} F4SE plugins out of {TotalCount} DLLs",
                f4sePluginCount, plugins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory: {DirectoryPath}", directoryPath);
        }

        return plugins;
    }

    private F4SEPluginInfo CreateNonF4SEPluginInfo(string filePath)
    {
        return new F4SEPluginInfo
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            IsF4SEPlugin = false,
            SupportsOG = false,
            SupportsNG = false,
            Compatibility = F4SECompatibility.NotF4SEPlugin,
            Version = _fileVersionService.GetFileVersion(filePath)?.Version
        };
    }

    private F4SECompatibility DetermineCompatibility(bool isF4SEPlugin, bool supportsOG, bool supportsNG)
    {
        if (!isF4SEPlugin)
            return F4SECompatibility.NotF4SEPlugin;

        if (supportsOG && supportsNG)
            return F4SECompatibility.Universal;

        if (supportsOG)
            return F4SECompatibility.OGOnly;

        if (supportsNG)
            return F4SECompatibility.NGOnly;

        return F4SECompatibility.Unknown;
    }
}
