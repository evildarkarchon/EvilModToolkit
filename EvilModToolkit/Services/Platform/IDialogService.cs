using System.Threading.Tasks;

namespace EvilModToolkit.Services.Platform
{
    /// <summary>
    /// Service for showing platform-native dialogs (folder picker, file picker, etc.)
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a folder picker dialog and returns the selected folder path.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="suggestedStartLocation">Optional suggested starting directory path.</param>
        /// <returns>The selected folder path, or null if canceled.</returns>
        Task<string?> ShowFolderPickerAsync(string title, string? suggestedStartLocation = null);

        /// <summary>
        /// Shows a file picker dialog and returns the selected file path.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="filterName">The name of the file filter (e.g., "Text Files").</param>
        /// <param name="filterPatterns">The glob patterns for the file filter (e.g., "*.txt").</param>
        /// <param name="suggestedStartLocation">Optional suggested starting directory path.</param>
        /// <returns>The selected file path, or null if canceled.</returns>
        Task<string?> ShowFilePickerAsync(string title, string filterName, string[] filterPatterns, string? suggestedStartLocation = null);
    }
}