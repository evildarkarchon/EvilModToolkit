using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// ViewModel for the Overview tab that displays game detection, mod manager info, and system information.
    /// </summary>
    public class OverviewViewModel : ViewModelBase
    {
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IModManagerService _modManagerService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly ILogger<OverviewViewModel> _logger;

        private GameInfo? _gameInfo;
        private ModManagerInfo? _modManagerInfo;
        private SystemInfo? _systemInfo;
        private bool _isF4SeInstalled;
        private string? _f4SeVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewViewModel"/> class.
        /// </summary>
        public OverviewViewModel(
            IGameDetectionService gameDetectionService,
            IModManagerService modManagerService,
            ISystemInfoService systemInfoService,
            ILogger<OverviewViewModel> logger)
        {
            _gameDetectionService = gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
            _modManagerService = modManagerService ?? throw new ArgumentNullException(nameof(modManagerService));
            _systemInfoService = systemInfoService ?? throw new ArgumentNullException(nameof(systemInfoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Problems = new ObservableCollection<ScanResult>();

            // Create the refresh command
            RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);

            // Perform initial refresh
            _ = RefreshAsync();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the game information.
        /// </summary>
        public GameInfo? GameInfo
        {
            get => _gameInfo;
            private set => this.RaiseAndSetIfChanged(ref _gameInfo, value);
        }

        /// <summary>
        /// Gets or sets the mod manager information.
        /// </summary>
        public ModManagerInfo? ModManagerInfo
        {
            get => _modManagerInfo;
            private set => this.RaiseAndSetIfChanged(ref _modManagerInfo, value);
        }

        /// <summary>
        /// Gets or sets the system information.
        /// </summary>
        public SystemInfo? SystemInfo
        {
            get => _systemInfo;
            private set => this.RaiseAndSetIfChanged(ref _systemInfo, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether F4SE is installed.
        /// </summary>
        public bool IsF4SeInstalled
        {
            get => _isF4SeInstalled;
            private set => this.RaiseAndSetIfChanged(ref _isF4SeInstalled, value);
        }

        /// <summary>
        /// Gets or sets the F4SE version string.
        /// </summary>
        public string? F4SeVersion
        {
            get => _f4SeVersion;
            private set => this.RaiseAndSetIfChanged(ref _f4SeVersion, value);
        }

        /// <summary>
        /// Gets the collection of problems detected during scanning.
        /// </summary>
        public ObservableCollection<ScanResult> Problems { get; }

        /// <summary>
        /// Gets a value indicating whether the game was detected.
        /// </summary>
        public bool IsGameDetected => GameInfo != null && GameInfo.IsInstalled;

        /// <summary>
        /// Gets a value indicating whether a mod manager was detected.
        /// </summary>
        public bool IsModManagerDetected => ModManagerInfo != null && ModManagerInfo.Type != ModManagerType.None;

        /// <summary>
        /// Gets the command to refresh all information.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Refreshes all game, mod manager, and system information.
        /// </summary>
        private async Task RefreshAsync()
        {
            if (!await TryExecuteAsync(async () =>
            {
                IsBusy = true;
                SetStatus("Refreshing information...");
                Problems.Clear();

                try
                {
                    // Detect game installation
                    _logger.LogInformation("Detecting Fallout 4 installation...");
                    GameInfo = await Task.Run(() => _gameDetectionService.DetectGame());

                    if (GameInfo == null || !GameInfo.IsInstalled)
                    {
                        _logger.LogWarning("Fallout 4 installation not detected");
                        Problems.Add(new ScanResult(
                            type: ProblemType.FileNotFound,
                            path: string.Empty,
                            relativePath: string.Empty,
                            summary: "Fallout 4 installation not detected. Please ensure the game is installed.",
                            severity: SeverityLevel.Error
                        ));
                    }
                    else
                    {
                        _logger.LogInformation("Fallout 4 detected at: {InstallPath}", GameInfo.InstallPath);

                        // Check for F4SE
                        await DetectF4SeAsync();
                    }

                    // Detect mod manager
                    _logger.LogInformation("Detecting mod manager...");
                    ModManagerInfo = await _modManagerService.DetectModManagerAsync();

                    if (ModManagerInfo != null && ModManagerInfo.Type != ModManagerType.None)
                    {
                        _logger.LogInformation("Mod manager detected: {Type} at {ExecutablePath}",
                            ModManagerInfo.Type, ModManagerInfo.ExecutablePath);
                    }
                    else
                    {
                        _logger.LogInformation("No mod manager detected (running standalone)");
                    }

                    // Get system information
                    _logger.LogInformation("Gathering system information...");
                    SystemInfo = await _systemInfoService.GetSystemInfoAsync();

                    SetStatus("Refresh complete");
                    _logger.LogInformation("Overview refresh completed successfully");
                }
                finally
                {
                    IsBusy = false;
                }
            }, _logger))
            {
                _logger.LogError("Failed to refresh overview information");
            }
        }

        /// <summary>
        /// Detects F4SE installation and version.
        /// </summary>
        private async Task DetectF4SeAsync()
        {
            if (GameInfo == null || string.IsNullOrEmpty(GameInfo.InstallPath))
            {
                IsF4SeInstalled = false;
                F4SeVersion = null;
                return;
            }

            try
            {
                var f4sePath = System.IO.Path.Combine(GameInfo.InstallPath, "f4se_loader.exe");
                if (System.IO.File.Exists(f4sePath))
                {
                    IsF4SeInstalled = true;

                    // Try to get version info
                    var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(f4sePath);
                    F4SeVersion = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}";

                    _logger.LogInformation("F4SE detected: version {Version}", F4SeVersion);
                }
                else
                {
                    IsF4SeInstalled = false;
                    F4SeVersion = null;

                    Problems.Add(new ScanResult(
                        type: ProblemType.FileNotFound,
                        path: GameInfo.InstallPath,
                        relativePath: "f4se_loader.exe",
                        summary: "F4SE (Fallout 4 Script Extender) is not installed. Many mods require F4SE to function.",
                        severity: SeverityLevel.Warning
                    ));

                    _logger.LogWarning("F4SE not detected at: {F4SePath}", f4sePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting F4SE");
                IsF4SeInstalled = false;
                F4SeVersion = null;
            }

            await Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RefreshCommand?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
