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
public class F4SePluginService : IF4SEPluginService
{
    private const string F4SePluginLoadExport = "F4SEPlugin_Load";
    private const string F4SePluginQueryExport = "F4SEPlugin_Query";
    private const string F4SePluginVersionExport = "F4SEPlugin_Version";

    private readonly ILogger<F4SePluginService> _logger;
    private readonly IFileVersionService _fileVersionService;

    public F4SePluginService(ILogger<F4SePluginService> logger, IFileVersionService fileVersionService)
    {
        _logger = logger;
        _fileVersionService = fileVersionService;
    }

    /// <inheritdoc />
    public F4SePluginInfo? AnalyzePlugin(string filePath)
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
                return CreateNonF4SePluginInfo(filePath);
            }

            var exportNames = exports.Select(e => e.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();

            var isF4SePlugin = exportNames.Contains(F4SePluginLoadExport);
            var supportsOg = exportNames.Contains(F4SePluginQueryExport);
            var supportsNg = exportNames.Contains(F4SePluginVersionExport);

            var compatibility = DetermineCompatibility(isF4SePlugin, supportsOg, supportsNg);

            var version = _fileVersionService.GetFileVersion(filePath)?.Version;

            var pluginInfo = new F4SePluginInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                IsF4SePlugin = isF4SePlugin,
                SupportsOg = supportsOg,
                SupportsNg = supportsNg,
                Compatibility = compatibility,
                Version = version
            };

            if (isF4SePlugin)
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
    public List<F4SePluginInfo> ScanDirectory(string directoryPath, bool recursive = false)
    {
        var plugins = new List<F4SePluginInfo>();

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

            var f4SePluginCount = plugins.Count(p => p.IsF4SePlugin);
            _logger.LogInformation("Found {F4SECount} F4SE plugins out of {TotalCount} DLLs",
                f4SePluginCount, plugins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory: {DirectoryPath}", directoryPath);
        }

        return plugins;
    }

    private F4SePluginInfo CreateNonF4SePluginInfo(string filePath)
    {
        return new F4SePluginInfo
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            IsF4SePlugin = false,
            SupportsOg = false,
            SupportsNg = false,
            Compatibility = F4SeCompatibility.NotF4SePlugin,
            Version = _fileVersionService.GetFileVersion(filePath)?.Version
        };
    }

    private F4SeCompatibility DetermineCompatibility(bool isF4SePlugin, bool supportsOg, bool supportsNg)
    {
        if (!isF4SePlugin)
            return F4SeCompatibility.NotF4SePlugin;

        if (supportsOg && supportsNg)
            return F4SeCompatibility.Universal;

        if (supportsOg)
            return F4SeCompatibility.OgOnly;

        if (supportsNg)
            return F4SeCompatibility.NgOnly;

        return F4SeCompatibility.Unknown;
    }
}