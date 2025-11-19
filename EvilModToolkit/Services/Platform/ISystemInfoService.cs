using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Platform;

/// <summary>
/// Service for collecting PC diagnostics (OS, RAM, CPU, GPU).
/// </summary>
public interface ISystemInfoService
{
    /// <summary>
    /// Collects system information asynchronously.
    /// </summary>
    /// <returns>System information, or null if collection fails.</returns>
    Task<SystemInfo?> GetSystemInfoAsync();
}