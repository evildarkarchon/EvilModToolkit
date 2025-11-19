namespace EvilModToolkit.Models;

/// <summary>
/// Represents suggested solutions for problems found during mod scanning.
/// Corresponds to Python's SolutionType StrEnum.
/// </summary>
public enum SolutionType
{
    /// <summary>
    /// These files should either be archived or deleted.
    /// </summary>
    ArchiveOrDeleteFile,

    /// <summary>
    /// These folders should either be archived or deleted.
    /// </summary>
    ArchiveOrDeleteFolder,

    /// <summary>
    /// This file should be deleted.
    /// </summary>
    DeleteFile,

    /// <summary>
    /// This file may need to be converted and relevant files updated for the new name.
    /// Otherwise it can likely be deleted or ignored.
    /// </summary>
    ConvertDeleteOrIgnoreFile,

    /// <summary>
    /// It can either be deleted or ignored.
    /// </summary>
    DeleteOrIgnoreFile,

    /// <summary>
    /// It can either be deleted or ignored.
    /// </summary>
    DeleteOrIgnoreFolder,

    /// <summary>
    /// Archives must be named the same as a plugin with an added suffix or added to an INI.
    /// </summary>
    RenameArchive,

    /// <summary>
    /// Download the mod from the provided link.
    /// </summary>
    DownloadMod,

    /// <summary>
    /// Verify files with Steam or reinstall the game.
    /// If you downgraded the game you will need to do so again afterward.
    /// </summary>
    VerifyFiles,

    /// <summary>
    /// If this file type is expected here, please report it.
    /// </summary>
    UnknownFormat,

    /// <summary>
    /// If you are using xEdit v4.1.5g+, all references to 'Addon Index' in this file
    /// should be updated to 'Parent Combination Index'.
    /// </summary>
    ComplexSorterFix
}
