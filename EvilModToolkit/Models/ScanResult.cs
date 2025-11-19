using System.Collections.Generic;

namespace EvilModToolkit.Models;

/// <summary>
/// Represents a problem or issue found during mod scanning.
/// Corresponds to Python's ProblemInfo class.
/// </summary>
public class ScanResult
{
    /// <summary>
    /// The type of problem detected
    /// </summary>
    public ProblemType Type { get; init; }

    /// <summary>
    /// Severity level of the problem (Error, Warning, Info)
    /// </summary>
    public SeverityLevel Severity { get; init; }

    /// <summary>
    /// Full absolute path to the problematic file or directory
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Path relative to the game or mod directory
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Name of the mod containing this problem, or null if not managed by a mod manager
    /// </summary>
    public string? ModName { get; init; }

    /// <summary>
    /// Human-readable summary of the problem
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Suggested solution type, or a custom solution string
    /// </summary>
    public object? Solution { get; init; }  // Can be SolutionType enum or string

    /// <summary>
    /// Optional list of related files with metadata (size/count, path)
    /// </summary>
    public List<(string Metadata, string FilePath)>? FileList { get; init; }

    /// <summary>
    /// Optional additional diagnostic data
    /// </summary>
    public List<string>? ExtraData { get; init; }

    /// <summary>
    /// Result of automatic fix attempt, if applicable
    /// </summary>
    public AutoFixResult? AutoFixResult { get; set; }

    /// <summary>
    /// Creates a new ScanResult
    /// </summary>
    public ScanResult(
        ProblemType type,
        string path,
        string relativePath,
        string summary,
        SeverityLevel severity = SeverityLevel.Warning,
        string? modName = null,
        object? solution = null,
        List<(string Metadata, string FilePath)>? fileList = null,
        List<string>? extraData = null)
    {
        Type = type;
        Path = path;
        RelativePath = relativePath;
        Summary = summary;
        Severity = severity;
        ModName = modName ?? (type != ProblemType.FileNotFound ? "<Unmanaged>" : string.Empty);
        Solution = solution;
        FileList = fileList;
        ExtraData = extraData;
    }

    /// <summary>
    /// Gets the solution as a displayable string
    /// </summary>
    public string GetSolutionText()
    {
        return Solution switch
        {
            SolutionType solutionType => GetSolutionDescription(solutionType),
            string str => str,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the human-readable description for a solution type
    /// </summary>
    private static string GetSolutionDescription(SolutionType solutionType)
    {
        return solutionType switch
        {
            SolutionType.ArchiveOrDeleteFile => "These files should either be archived or deleted.",
            SolutionType.ArchiveOrDeleteFolder => "These folders should either be archived or deleted.",
            SolutionType.DeleteFile => "This file should be deleted.",
            SolutionType.ConvertDeleteOrIgnoreFile => "This file may need to be converted and relevant files updated for the new name.\nOtherwise it can likely be deleted or ignored.",
            SolutionType.DeleteOrIgnoreFile => "It can either be deleted or ignored.",
            SolutionType.DeleteOrIgnoreFolder => "It can either be deleted or ignored.",
            SolutionType.RenameArchive => "Archives must be named the same as a plugin with an added suffix or added to an INI.",
            SolutionType.DownloadMod => "Download the mod here:",
            SolutionType.VerifyFiles => "Verify files with Steam or reinstall the game.\nIf you downgraded the game you will need to do so again afterward.",
            SolutionType.UnknownFormat => "If this file type is expected here, please report it.",
            SolutionType.ComplexSorterFix => "If you are using xEdit v4.1.5g+, all references to 'Addon Index' in this file should be updated to 'Parent Combination Index'.",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Validates the scan result for consistency
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            return false;

        if (string.IsNullOrWhiteSpace(Summary))
            return false;

        // FileNotFound problems should have empty ModName
        if (Type == ProblemType.FileNotFound && !string.IsNullOrEmpty(ModName))
            return false;

        return true;
    }
}
