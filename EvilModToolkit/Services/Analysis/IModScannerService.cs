using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Analysis;

/// <summary>
/// Service for scanning mod installations for issues and conflicts.
/// </summary>
public interface IModScannerService
{
    /// <summary>
    /// Scans the game data folder and mods for issues based on the provided options.
    /// </summary>
    /// <param name="gameInfo">The detected game information.</param>
    /// <param name="modManagerInfo">The detected mod manager information.</param>
    /// <param name="options">Options to configure the scan.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of detected problems/results.</returns>
    Task<List<ScanResult>> ScanAsync(
        GameInfo gameInfo,
        ModManagerInfo modManagerInfo,
        ScanOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
