using EvilModToolkit.Models;
using EvilModToolkit.Services.Configuration;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings tab that allows users to configure application preferences.
    /// Provides two-way binding to AppSettings properties and commands to save and reset settings.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SettingsViewModel> _logger;

        // Backing fields for settings properties
        private string? _gamePathOverride;
        private string? _mo2PathOverride;
        private string? _vortexPathOverride;
        private string? _lastF4SEScanDirectory;
        private string? _lastBA2PatchDirectory;
        private bool _scanF4SERecursively;
        private double _windowWidth;
        private double _windowHeight;
        private bool _showHiddenFiles;
        private string _theme;
        private string _logLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// Loads settings from the settings service on construction.
        /// </summary>
        /// <param name="settingsService">The settings service for loading/saving settings.</param>
        /// <param name="logger">Logger for the ViewModel.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
        public SettingsViewModel(
            ISettingsService settingsService,
            IDialogService dialogService,
            ILogger<SettingsViewModel> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize default values before loading from service
            _theme = "System";
            _logLevel = "Information";
            _windowWidth = 1200;
            _windowHeight = 800;
            _scanF4SERecursively = true; // Default to recursive scanning

            // Create commands - SaveSettings is async, ResetDefaults is synchronous
            SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
            ResetDefaultsCommand = ReactiveCommand.Create(ResetDefaults);

            // Create browse commands for folder selection
            BrowseGamePathCommand = ReactiveCommand.CreateFromTask(BrowseGamePathAsync);
            BrowseMO2PathCommand = ReactiveCommand.CreateFromTask(BrowseMO2PathAsync);
            BrowseVortexPathCommand = ReactiveCommand.CreateFromTask(BrowseVortexPathAsync);
            BrowseF4SEScanDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseF4SEScanDirectoryAsync);
            BrowseBA2PatchDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseBA2PatchDirectoryAsync);

            // Load settings asynchronously on construction
            _ = LoadSettingsAsync();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the override path to the Fallout 4 installation.
        /// If null or empty, automatic detection is used.
        /// </summary>
        public string? GamePathOverride
        {
            get => _gamePathOverride;
            set => this.RaiseAndSetIfChanged(ref _gamePathOverride, value);
        }

        /// <summary>
        /// Gets or sets the override path to Mod Organizer 2.
        /// </summary>
        public string? MO2PathOverride
        {
            get => _mo2PathOverride;
            set => this.RaiseAndSetIfChanged(ref _mo2PathOverride, value);
        }

        /// <summary>
        /// Gets or sets the override path to Vortex.
        /// </summary>
        public string? VortexPathOverride
        {
            get => _vortexPathOverride;
            set => this.RaiseAndSetIfChanged(ref _vortexPathOverride, value);
        }

        /// <summary>
        /// Gets or sets the last scan directory for F4SE plugins.
        /// </summary>
        public string? LastF4SEScanDirectory
        {
            get => _lastF4SEScanDirectory;
            set => this.RaiseAndSetIfChanged(ref _lastF4SEScanDirectory, value);
        }

        /// <summary>
        /// Gets or sets the last BA2 patch directory.
        /// </summary>
        public string? LastBA2PatchDirectory
        {
            get => _lastBA2PatchDirectory;
            set => this.RaiseAndSetIfChanged(ref _lastBA2PatchDirectory, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to scan F4SE plugins recursively.
        /// Default is true for comprehensive scanning.
        /// </summary>
        public bool ScanF4SERecursively
        {
            get => _scanF4SERecursively;
            set => this.RaiseAndSetIfChanged(ref _scanF4SERecursively, value);
        }

        /// <summary>
        /// Gets or sets the window width.
        /// Must be between 400 and 8192 pixels.
        /// </summary>
        public double WindowWidth
        {
            get => _windowWidth;
            set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
        }

        /// <summary>
        /// Gets or sets the window height.
        /// Must be between 400 and 8192 pixels.
        /// </summary>
        public double WindowHeight
        {
            get => _windowHeight;
            set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show hidden files in scans.
        /// </summary>
        public bool ShowHiddenFiles
        {
            get => _showHiddenFiles;
            set => this.RaiseAndSetIfChanged(ref _showHiddenFiles, value);
        }

        /// <summary>
        /// Gets or sets the application theme (Light, Dark, System).
        /// </summary>
        public string Theme
        {
            get => _theme;
            set => this.RaiseAndSetIfChanged(ref _theme, value);
        }

        /// <summary>
        /// Gets or sets the log level (Trace, Debug, Information, Warning, Error, Critical, None).
        /// </summary>
        public string LogLevel
        {
            get => _logLevel;
            set => this.RaiseAndSetIfChanged(ref _logLevel, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to save current settings to disk.
        /// Validates settings before saving and displays appropriate status messages.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }

        /// <summary>
        /// Gets the command to reset all settings to default values.
        /// Does not automatically save - user must click Save to persist changes.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ResetDefaultsCommand { get; }

        /// <summary>
        /// Gets the command to browse for the game installation path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseGamePathCommand { get; }

        /// <summary>
        /// Gets the command to browse for the MO2 installation path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseMO2PathCommand { get; }

        /// <summary>
        /// Gets the command to browse for the Vortex installation path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseVortexPathCommand { get; }

        /// <summary>
        /// Gets the command to browse for the F4SE scan directory.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseF4SEScanDirectoryCommand { get; }

        /// <summary>
        /// Gets the command to browse for the BA2 patch directory.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseBA2PatchDirectoryCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads settings from the settings service asynchronously.
        /// Called automatically on construction.
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            if (!await TryExecuteAsync(async () =>
                {
                    _logger.LogInformation("Loading application settings...");
                    var settings = await _settingsService.LoadSettingsAsync();

                    // Update all properties from loaded settings
                    GamePathOverride = settings.GamePathOverride;
                    MO2PathOverride = settings.MO2PathOverride;
                    VortexPathOverride = settings.VortexPathOverride;
                    LastF4SEScanDirectory = settings.LastF4SEScanDirectory;
                    LastBA2PatchDirectory = settings.LastBA2PatchDirectory;
                    ScanF4SERecursively = settings.ScanF4SERecursively;
                    WindowWidth = settings.WindowWidth;
                    WindowHeight = settings.WindowHeight;
                    ShowHiddenFiles = settings.ShowHiddenFiles;
                    Theme = settings.Theme;
                    LogLevel = settings.LogLevel;

                    _logger.LogInformation("Settings loaded successfully from {SettingsPath}",
                        _settingsService.GetSettingsFilePath());
                }, _logger))
            {
                _logger.LogError("Failed to load settings, using defaults");
            }
        }

        /// <summary>
        /// Saves current settings to disk asynchronously.
        /// Validates settings before saving and provides user feedback.
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            try
            {
                if (!await TryExecuteAsync(async () =>
                    {
                        IsBusy = true;
                        SetStatus("Saving settings...");

                        // Create AppSettings object from current property values
                        var settings = new AppSettings
                        {
                            GamePathOverride = GamePathOverride,
                            MO2PathOverride = MO2PathOverride,
                            VortexPathOverride = VortexPathOverride,
                            LastF4SEScanDirectory = LastF4SEScanDirectory,
                            LastBA2PatchDirectory = LastBA2PatchDirectory,
                            ScanF4SERecursively = ScanF4SERecursively,
                            WindowWidth = WindowWidth,
                            WindowHeight = WindowHeight,
                            ShowHiddenFiles = ShowHiddenFiles,
                            Theme = Theme,
                            LogLevel = LogLevel
                        };

                        // Validate settings before saving
                        var validationErrors = settings.Validate();
                        if (validationErrors.Count > 0)
                        {
                            var errorMessage = $"Invalid settings: {string.Join(", ", validationErrors)}";
                            SetError(errorMessage);
                            _logger.LogWarning("Settings validation failed: {Errors}", errorMessage);
                            return;
                        }

                        // Save settings to disk
                        await _settingsService.SaveSettingsAsync(settings);

                        SetStatus("Settings saved successfully");
                        _logger.LogInformation("Settings saved to {SettingsPath}",
                            _settingsService.GetSettingsFilePath());
                    }, _logger))
                {
                    _logger.LogError("Failed to save settings");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Resets all settings to default values.
        /// Changes are not saved to disk until user explicitly clicks Save.
        /// </summary>
        private void ResetDefaults()
        {
            if (!TryExecute(() =>
                {
                    _logger.LogInformation("Resetting settings to defaults...");

                    // Get default settings from service
                    var defaults = _settingsService.GetDefaultSettings();

                    // Update all properties to default values
                    GamePathOverride = defaults.GamePathOverride;
                    MO2PathOverride = defaults.MO2PathOverride;
                    VortexPathOverride = defaults.VortexPathOverride;
                    LastF4SEScanDirectory = defaults.LastF4SEScanDirectory;
                    LastBA2PatchDirectory = defaults.LastBA2PatchDirectory;
                    ScanF4SERecursively = defaults.ScanF4SERecursively;
                    WindowWidth = defaults.WindowWidth;
                    WindowHeight = defaults.WindowHeight;
                    ShowHiddenFiles = defaults.ShowHiddenFiles;
                    Theme = defaults.Theme;
                    LogLevel = defaults.LogLevel;

                    SetStatus("Settings reset to defaults (not saved yet - click Save to persist)");
                    _logger.LogInformation("Settings reset to defaults");
                }, _logger))
            {
                _logger.LogError("Failed to reset settings to defaults");
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the game installation path.
        /// </summary>
        private async Task BrowseGamePathAsync()
        {
            var selectedPath = await _dialogService.ShowFolderPickerAsync(
                "Select Fallout 4 Installation Directory",
                GamePathOverride);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                GamePathOverride = selectedPath;
                _logger.LogInformation("Game path set to: {Path}", selectedPath);
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the MO2 installation path.
        /// </summary>
        private async Task BrowseMO2PathAsync()
        {
            var selectedPath = await _dialogService.ShowFolderPickerAsync(
                "Select Mod Organizer 2 Directory",
                MO2PathOverride);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                MO2PathOverride = selectedPath;
                _logger.LogInformation("MO2 path set to: {Path}", selectedPath);
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the Vortex installation path.
        /// </summary>
        private async Task BrowseVortexPathAsync()
        {
            var selectedPath = await _dialogService.ShowFolderPickerAsync(
                "Select Vortex Installation Directory",
                VortexPathOverride);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                VortexPathOverride = selectedPath;
                _logger.LogInformation("Vortex path set to: {Path}", selectedPath);
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the F4SE plugin scan directory.
        /// </summary>
        private async Task BrowseF4SEScanDirectoryAsync()
        {
            var selectedPath = await _dialogService.ShowFolderPickerAsync(
                "Select F4SE Plugin Directory",
                LastF4SEScanDirectory);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LastF4SEScanDirectory = selectedPath;
                _logger.LogInformation("F4SE scan directory set to: {Path}", selectedPath);
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the BA2 patch directory.
        /// </summary>
        private async Task BrowseBA2PatchDirectoryAsync()
        {
            var selectedPath = await _dialogService.ShowFolderPickerAsync(
                "Select BA2 Archive Directory",
                LastBA2PatchDirectory);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LastBA2PatchDirectory = selectedPath;
                _logger.LogInformation("BA2 patch directory set to: {Path}", selectedPath);
            }
        }

        /// <summary>
        /// Disposes resources used by the ViewModel.
        /// Ensures commands are properly disposed to prevent memory leaks.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose ReactiveCommand instances to clean up subscriptions
                SaveSettingsCommand?.Dispose();
                ResetDefaultsCommand?.Dispose();
                BrowseGamePathCommand?.Dispose();
                BrowseMO2PathCommand?.Dispose();
                BrowseVortexPathCommand?.Dispose();
                BrowseF4SEScanDirectoryCommand?.Dispose();
                BrowseBA2PatchDirectoryCommand?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}