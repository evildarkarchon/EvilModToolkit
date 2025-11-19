namespace EvilModToolkit.Models;

/// <summary>
/// Represents the result of an automatic fix attempt for a problem.
/// Corresponds to Python's AutoFixResult dataclass.
/// </summary>
public class AutoFixResult
{
    /// <summary>
    /// Indicates whether the auto-fix was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Detailed description of what happened during the fix attempt
    /// </summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new AutoFixResult
    /// </summary>
    /// <param name="success">Whether the fix was successful</param>
    /// <param name="details">Details about the fix attempt</param>
    public AutoFixResult(bool success, string details)
    {
        Success = success;
        Details = details;
    }

    /// <summary>
    /// Creates a successful AutoFixResult
    /// </summary>
    public static AutoFixResult Successful(string details) => new(true, details);

    /// <summary>
    /// Creates a failed AutoFixResult
    /// </summary>
    public static AutoFixResult Failed(string details) => new(false, details);
}
