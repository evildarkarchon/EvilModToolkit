using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace EvilModToolkit.Services.Platform
{
    /// <summary>
    /// Platform-native dialog service implementation using Avalonia's StorageProvider API.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DialogService : IDialogService
    {
        private readonly ILogger<DialogService> _logger;

        public DialogService(ILogger<DialogService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Shows a folder picker dialog and returns the selected folder path.
        /// </summary>
        public async Task<string?> ShowFolderPickerAsync(string title, string? suggestedStartLocation = null)
        {
            try
            {
                // Get the main window from the application lifetime
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                {
                    _logger.LogError("Cannot show folder picker: ApplicationLifetime is not desktop style");
                    return null;
                }

                var mainWindow = desktop.MainWindow;
                if (mainWindow == null)
                {
                    _logger.LogError("Cannot show folder picker: MainWindow is null");
                    return null;
                }

                // Get the storage provider
                var storageProvider = mainWindow.StorageProvider;
                if (storageProvider == null)
                {
                    _logger.LogError("Cannot show folder picker: StorageProvider is null");
                    return null;
                }

                // Configure folder picker options
                var options = new FolderPickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false
                };

                // Set suggested start location if provided
                if (!string.IsNullOrEmpty(suggestedStartLocation) && System.IO.Directory.Exists(suggestedStartLocation))
                {
                    try
                    {
                        var folder = await storageProvider.TryGetFolderFromPathAsync(suggestedStartLocation);
                        if (folder != null)
                        {
                            options.SuggestedStartLocation = folder;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set suggested start location: {Path}", suggestedStartLocation);
                    }
                }

                // Show the folder picker
                var result = await storageProvider.OpenFolderPickerAsync(options);

                if (result != null && result.Count > 0)
                {
                    var selectedFolder = result[0];
                    var path = selectedFolder.TryGetLocalPath();

                    if (!string.IsNullOrEmpty(path))
                    {
                        _logger.LogInformation("Folder selected: {Path}", path);
                        return path;
                    }
                }

                _logger.LogDebug("Folder picker canceled or no selection made");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing folder picker dialog");
                return null;
            }
        }
    }
}
