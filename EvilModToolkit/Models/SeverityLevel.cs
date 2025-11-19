namespace EvilModToolkit.Models;

/// <summary>
/// Represents the severity level of a problem found during scanning.
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Informational message, no action required
    /// </summary>
    Info,

    /// <summary>
    /// Warning - potential issue that should be reviewed
    /// </summary>
    Warning,

    /// <summary>
    /// Error - critical issue that should be fixed
    /// </summary>
    Error
}
