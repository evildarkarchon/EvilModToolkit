using System;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for collecting PC diagnostics (OS, RAM, CPU, GPU) using WMI.
/// </summary>
[SupportedOSPlatform("windows")]
public class SystemInfoService : ISystemInfoService
{
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SystemInfo?> GetSystemInfoAsync()
    {
        try
        {
            // Run WMI queries on a background thread to avoid blocking
            return await Task.Run(() => CollectSystemInfo()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system information");
            return null;
        }
    }

    private SystemInfo? CollectSystemInfo()
    {
        try
        {
            var osInfo = GetOperatingSystemInfo();
            var ramInfo = GetRamInfo();
            var cpuInfo = GetCpuInfo();
            var gpuInfo = GetGpuInfo();

            return new SystemInfo
            {
                OperatingSystem = osInfo.Name,
                BuildNumber = osInfo.BuildNumber,
                Architecture = osInfo.Architecture,
                TotalRamGb = ramInfo,
                CpuName = cpuInfo,
                GpuName = gpuInfo.Name,
                GpuMemoryMb = gpuInfo.MemoryMb
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system info collection");
            return null;
        }
    }

    private (string Name, string BuildNumber, string Architecture) GetOperatingSystemInfo()
    {
        try
        {
            using var searcher =
                new ManagementObjectSearcher("SELECT Caption, BuildNumber, OSArchitecture FROM Win32_OperatingSystem");
            using var results = searcher.Get();

            var os = results.Cast<ManagementObject>().FirstOrDefault();
            if (os == null)
                return ("Unknown", "Unknown", "Unknown");

            var caption = os["Caption"]?.ToString() ?? "Unknown";
            var buildNumber = os["BuildNumber"]?.ToString() ?? "Unknown";
            var architecture = os["OSArchitecture"]?.ToString() ?? "Unknown";

            return (caption, buildNumber, architecture);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OS information");
            return ("Unknown", "Unknown", "Unknown");
        }
    }

    private double GetRamInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            using var results = searcher.Get();

            long totalBytes = 0;
            foreach (ManagementObject memory in results)
            {
                if (memory["Capacity"] != null)
                {
                    totalBytes += Convert.ToInt64(memory["Capacity"]);
                }
            }

            // Convert bytes to GB
            return totalBytes / (1024.0 * 1024.0 * 1024.0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get RAM information");
            return 0;
        }
    }

    private string GetCpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            using var results = searcher.Get();

            var cpu = results.Cast<ManagementObject>().FirstOrDefault();
            return cpu?["Name"]?.ToString() ?? "Unknown CPU";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU information");
            return "Unknown CPU";
        }
    }

    private (string Name, long MemoryMb) GetGpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
            using var results = searcher.Get();

            // Get the first GPU (primary)
            var gpu = results.Cast<ManagementObject>().FirstOrDefault();
            if (gpu == null)
                return ("Unknown GPU", 0);

            var name = gpu["Name"]?.ToString() ?? "Unknown GPU";
            var memoryBytes = gpu["AdapterRAM"] != null ? Convert.ToInt64(gpu["AdapterRAM"]) : 0;
            var memoryMb = memoryBytes / (1024 * 1024);

            return (name, memoryMb);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GPU information");
            return ("Unknown GPU", 0);
        }
    }
}