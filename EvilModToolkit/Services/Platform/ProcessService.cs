using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for process detection and parent process navigation using WMI.
/// </summary>
[SupportedOSPlatform("windows")]
public class ProcessService : IProcessService
{
    private const int MaxProcessTreeDepth = 8;
    private readonly ILogger<ProcessService> _logger;
    private readonly IFileVersionService _fileVersionService;

    public ProcessService(ILogger<ProcessService> logger, IFileVersionService fileVersionService)
    {
        _logger = logger;
        _fileVersionService = fileVersionService;
    }

    /// <inheritdoc />
    public int? GetParentProcessId(int processId)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");
            using var results = searcher.Get();

            var process = results.Cast<ManagementObject>().FirstOrDefault();
            if (process?["ParentProcessId"] is uint parentId)
            {
                return (int)parentId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get parent process ID for process {ProcessId}", processId);
            return null;
        }
    }

    /// <inheritdoc />
    public ModManagerInfo? FindModManager()
    {
        try
        {
            var currentProcessId = Environment.ProcessId;
            _logger.LogDebug("Starting mod manager detection from process {ProcessId}", currentProcessId);

            // Walk up the process tree looking for known mod managers
            for (int depth = 0; depth < MaxProcessTreeDepth; depth++)
            {
                var parentId = GetParentProcessId(currentProcessId);
                if (parentId == null)
                {
                    _logger.LogDebug("Reached end of process tree at depth {Depth}", depth);
                    break;
                }

                _logger.LogDebug("Checking parent process {ParentId} at depth {Depth}", parentId, depth);

                var modManagerInfo = CheckProcess(parentId.Value);
                if (modManagerInfo != null)
                {
                    _logger.LogInformation("Found mod manager: {Type} at {Path}",
                        modManagerInfo.Type, modManagerInfo.ExecutablePath);
                    return modManagerInfo;
                }

                currentProcessId = parentId.Value;
            }

            _logger.LogDebug("No mod manager detected in process tree");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mod manager detection");
            return null;
        }
    }

    private ModManagerInfo? CheckProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName.ToLowerInvariant();
            var executablePath = GetProcessExecutablePath(processId);

            if (string.IsNullOrEmpty(executablePath))
            {
                return null;
            }

            // Check for Mod Organizer 2
            if (processName.Contains("modorganizer"))
            {
                var version = _fileVersionService.GetFileVersion(executablePath)?.Version ?? "Unknown";

                return new ModManagerInfo
                {
                    Type = ModManagerType.ModOrganizer2,
                    ExecutablePath = executablePath,
                    Version = version,
                    ProcessId = processId,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
                };
            }

            // Check for Vortex
            if (processName.Contains("vortex"))
            {
                var version = _fileVersionService.GetFileVersion(executablePath)?.Version ?? "Unknown";

                return new ModManagerInfo
                {
                    Type = ModManagerType.Vortex,
                    ExecutablePath = executablePath,
                    Version = version,
                    ProcessId = processId,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking process {ProcessId}", processId);
            return null;
        }
    }

    private string? GetProcessExecutablePath(int processId)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}");
            using var results = searcher.Get();

            var process = results.Cast<ManagementObject>().FirstOrDefault();
            return process?["ExecutablePath"]?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get executable path for process {ProcessId}", processId);
            return null;
        }
    }
}
