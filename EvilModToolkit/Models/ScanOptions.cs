namespace EvilModToolkit.Models;

/// <summary>
/// Configuration options for the mod scanner.
/// </summary>
public class ScanOptions
{
    /// <summary>
    /// Scan for junk files not used by the game.
    /// </summary>
    public bool ScanJunkFiles { get; set; } = true;

    /// <summary>
    /// Scan for loose previs files that should be archived.
    /// </summary>
    public bool ScanLoosePrevis { get; set; } = true;

    /// <summary>
    /// Scan for problematic overrides (e.g. AnimTextData).
    /// </summary>
    public bool ScanProblemOverrides { get; set; } = true;

    /// <summary>
    /// Scan for files with wrong format/extension for their location.
    /// </summary>
    public bool ScanWrongFormat { get; set; } = true;

    /// <summary>
    /// Scan for specific errors (e.g. Complex Sorter INI).
    /// </summary>
    public bool ScanErrors { get; set; } = true;

    /// <summary>
    /// Scan for race subgraph records (sadd) in modules.
    /// </summary>
    public bool ScanRaceSubgraphs { get; set; } = true;

    /// <summary>
    /// Skip scanning the data folder (e.g. if only checking other things).
    /// </summary>
    public bool SkipDataScan { get; set; } = false;
}
