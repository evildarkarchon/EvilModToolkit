namespace EvilModToolkit.Models;

/// <summary>
/// Represents the type of problem found during mod scanning.
/// Corresponds to Python's ProblemType StrEnum.
/// </summary>
public enum ProblemType
{
    /// <summary>
    /// Junk file that should be removed or archived
    /// </summary>
    JunkFile,

    /// <summary>
    /// File format is unexpected for its location
    /// </summary>
    UnexpectedFormat,

    /// <summary>
    /// DLL file is in wrong location
    /// </summary>
    MisplacedDll,

    /// <summary>
    /// Loose previs files found (should be archived)
    /// </summary>
    LoosePrevis,

    /// <summary>
    /// Loose AnimTextData folder found (should be archived)
    /// </summary>
    AnimTextDataFolder,

    /// <summary>
    /// BA2 archive is invalid or corrupted
    /// </summary>
    InvalidArchive,

    /// <summary>
    /// Plugin module (ESM/ESP/ESL) is invalid
    /// </summary>
    InvalidModule,

    /// <summary>
    /// BA2 archive has invalid naming
    /// </summary>
    InvalidArchiveName,

    /// <summary>
    /// F4SE script override detected
    /// </summary>
    F4SeScriptOverride,

    /// <summary>
    /// Required file not found
    /// </summary>
    FileNotFound,

    /// <summary>
    /// File version mismatch
    /// </summary>
    WrongVersion,

    /// <summary>
    /// Complex Sorter error (xEdit Addon Index issue)
    /// </summary>
    ComplexSorter
}
