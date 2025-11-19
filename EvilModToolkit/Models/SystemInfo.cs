namespace EvilModToolkit.Models;

/// <summary>
/// Represents system hardware and software information.
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets the operating system name and version (e.g., "Windows 11 Pro").
    /// </summary>
    public string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets the OS build number (e.g., "22631").
    /// </summary>
    public string BuildNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total installed RAM in gigabytes (e.g., 16.0).
    /// </summary>
    public double TotalRamGb { get; init; }

    /// <summary>
    /// Gets the CPU name (e.g., "Intel(R) Core(TM) i7-10700K CPU @ 3.80GHz").
    /// </summary>
    public string CpuName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the GPU name (e.g., "NVIDIA GeForce RTX 3080").
    /// </summary>
    public string GpuName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the GPU memory in megabytes.
    /// </summary>
    public long GpuMemoryMb { get; init; }

    /// <summary>
    /// Gets the system architecture (e.g., "64-bit").
    /// </summary>
    public string Architecture { get; init; } = string.Empty;
}
