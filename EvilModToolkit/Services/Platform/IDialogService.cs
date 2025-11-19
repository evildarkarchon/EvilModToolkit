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
    }
}