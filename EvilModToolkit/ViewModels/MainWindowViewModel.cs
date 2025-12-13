using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// Main window ViewModel that serves as the composition root for the application.
    /// Manages tab navigation, window state, and global application commands.
    /// Composes all child tab ViewModels (Overview, F4SE, Settings, Tools).
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger<MainWindowViewModel> _logger;

        // Child ViewModels for each tab - injected via DI and owned by this ViewModel
        private readonly OverviewViewModel _overviewViewModel;
        private readonly F4SEViewModel _f4seViewModel;
        private readonly ScannerViewModel _scannerViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ToolsViewModel _toolsViewModel;

        // Window state properties
        private int _selectedTabIndex;
        private string _windowTitle;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// This is the composition root - all child ViewModels are injected and managed here.
        /// </summary>
        /// <param name="overviewViewModel">ViewModel for the Overview tab (Tab 0).</param>
        /// <param name="f4seViewModel">ViewModel for the F4SE Scanner tab (Tab 1).</param>
        /// <param name="scannerViewModel">ViewModel for the Mod Scanner tab (Tab 2).</param>
        /// <param name="settingsViewModel">ViewModel for the Settings tab (Tab 4).</param>
        /// <param name="toolsViewModel">ViewModel for the Tools/Patcher tab (Tab 3).</param>
        /// <param name="logger">Logger for diagnostic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
        public MainWindowViewModel(
            OverviewViewModel overviewViewModel,
            F4SEViewModel f4seViewModel,
            ScannerViewModel scannerViewModel,
            SettingsViewModel settingsViewModel,
            ToolsViewModel toolsViewModel,
            ILogger<MainWindowViewModel> logger)
        {
            // Validate all dependencies - fail fast if any are missing
            _overviewViewModel = overviewViewModel ?? throw new ArgumentNullException(nameof(overviewViewModel));
            _f4seViewModel = f4seViewModel ?? throw new ArgumentNullException(nameof(f4seViewModel));
            _scannerViewModel = scannerViewModel ?? throw new ArgumentNullException(nameof(scannerViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            _toolsViewModel = toolsViewModel ?? throw new ArgumentNullException(nameof(toolsViewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize window properties
            _windowTitle = "Evil Modding Toolkit";
            _selectedTabIndex = 0; // Default to Overview tab

            // Create global commands
            // ExitCommand - Simple command to close the application (View will handle actual closure)
            ExitCommand = ReactiveCommand.Create(OnExit);

            // AboutCommand - Shows about dialog (placeholder for now, will be enhanced later)
            AboutCommand = ReactiveCommand.Create(OnAbout);

            _logger.LogInformation("MainWindowViewModel initialized with {WindowTitle}", _windowTitle);
        }

        #region Properties - Tab ViewModels

        /// <summary>
        /// Gets the ViewModel for the Overview tab (Tab 0).
        /// Displays game detection, mod manager info, and system information.
        /// </summary>
        public OverviewViewModel OverviewViewModel => _overviewViewModel;

        /// <summary>
        /// Gets the ViewModel for the F4SE Scanner tab (Tab 1).
        /// Scans and displays F4SE plugin compatibility.
        /// </summary>
        public F4SEViewModel F4SEViewModel => _f4seViewModel;

        /// <summary>
        /// Gets the ViewModel for the Mod Scanner tab (Tab 2).
        /// Scans mod files for issues.
        /// </summary>
        public ScannerViewModel ScannerViewModel => _scannerViewModel;

        /// <summary>
        /// Gets the ViewModel for the Tools/Patcher tab (Tab 3).
        /// Provides BA2 archive patching and xdelta game file patching.
        /// </summary>
        public ToolsViewModel ToolsViewModel => _toolsViewModel;

        /// <summary>
        /// Gets the ViewModel for the Settings tab (Tab 4).
        /// Allows configuration of application preferences.
        /// </summary>
        public SettingsViewModel SettingsViewModel => _settingsViewModel;

        #endregion

        #region Properties - Window State

        /// <summary>
        /// Gets or sets the currently selected tab index.
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                // Log tab changes for diagnostics
                if (_selectedTabIndex != value)
                {
                    var tabName = GetTabName(value);
                    _logger.LogDebug("Navigating to tab: {TabIndex} ({TabName})", value, tabName);
                }

                this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
            }
        }

        /// <summary>
        /// Gets or sets the window title displayed in the title bar.
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to exit the application.
        /// Execution signals the View to close the main window gracefully.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }

        /// <summary>
        /// Gets the command to show the About dialog.
        /// Displays application information, version, and credits.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AboutCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the friendly name for a tab index.
        /// Used for logging and diagnostics.
        /// </summary>
        /// <param name="tabIndex">The zero-based tab index.</param>
        /// <returns>The friendly tab name.</returns>
        private static string GetTabName(int tabIndex) => tabIndex switch
        {
            0 => "Overview",
            1 => "F4SE Scanner",
            2 => "Mod Scanner",
            3 => "Tools/Patcher",
            4 => "Settings",
            _ => "Unknown"
        };

        /// <summary>
        /// Handles the Exit command execution.
        /// Logs the exit request and sets status message.
        /// The View is responsible for actually closing the window.
        /// </summary>
        private void OnExit()
        {
            _logger.LogInformation("Exit command executed - requesting application shutdown");
            SetStatus("Exiting application...");
        }

        /// <summary>
        /// Handles the About command execution.
        /// Displays application information and credits.
        /// Currently logs the event - will be enhanced with actual dialog in the View.
        /// </summary>
        private void OnAbout()
        {
            _logger.LogInformation("About command executed - showing application info");
            SetStatus("Evil Modding Toolkit - A C# port of the Collective Modding Toolkit");

            // TODO: In the View, this should trigger an About dialog showing:
            // - Application name and version
            // - Original toolkit credits (wxMichael)
            // - C# port information
            // - License information
            // For now, just log and set status message
        }

        /// <summary>
        /// Disposes resources used by the ViewModel.
        /// Properly disposes all child ViewModels and commands to prevent memory leaks.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.LogDebug("Disposing MainWindowViewModel and all child ViewModels");

                // Dispose all child ViewModels - important because they are Transient
                // and owned by this ViewModel (guaranteed non-null by constructor validation)
                _overviewViewModel.Dispose();
                _f4seViewModel.Dispose();
                _scannerViewModel.Dispose();
                _settingsViewModel.Dispose();
                _toolsViewModel.Dispose();

                // Dispose ReactiveCommands to clean up subscriptions
                ExitCommand.Dispose();
                AboutCommand.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
