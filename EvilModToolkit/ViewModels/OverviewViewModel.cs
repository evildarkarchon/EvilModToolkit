using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly ILogger<OverviewViewModel> _logger;

        private GameInfo? _gameInfo;
        private ModManagerInfo? _modManagerInfo;
        private SystemInfo? _systemInfo;
        private bool _isF4SeInstalled;
        private string? _f4SeVersion;
        private int _ba2CountGeneral;
        private int _ba2CountTexture;
        private int _ba2CountV1;
        private int _ba2CountV7V8;
        private int _ba2CountUnreadable;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewViewModel"/> class.
        /// </summary>
        public OverviewViewModel(
            IGameDetectionService gameDetectionService,
            IModManagerService modManagerService,
            ISystemInfoService systemInfoService,
            IBA2ArchiveService ba2ArchiveService,
            ILogger<OverviewViewModel> logger)
        {
            _gameDetectionService = gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
            _modManagerService = modManagerService ?? throw new ArgumentNullException(nameof(modManagerService));
            _systemInfoService = systemInfoService ?? throw new ArgumentNullException(nameof(systemInfoService));
            _ba2ArchiveService = ba2ArchiveService ?? throw new ArgumentNullException(nameof(ba2ArchiveService));
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
            private set
            {
                this.RaiseAndSetIfChanged(ref _gameInfo, value);
                this.RaisePropertyChanged(nameof(IsGameDetected));
            }
        }

        /// <summary>
        /// Gets or sets the mod manager information.
        /// </summary>
        public ModManagerInfo? ModManagerInfo
        {
            get => _modManagerInfo;
            private set
            {
                this.RaiseAndSetIfChanged(ref _modManagerInfo, value);
                this.RaisePropertyChanged(nameof(IsModManagerDetected));
            }
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
        /// Gets the count of General (GNRL) BA2 archives.
        /// </summary>
        public int BA2CountGeneral
        {
            get => _ba2CountGeneral;
            private set => this.RaiseAndSetIfChanged(ref _ba2CountGeneral, value);
        }

        /// <summary>
        /// Gets the count of Texture (DX10) BA2 archives.
        /// </summary>
        public int BA2CountTexture
        {
            get => _ba2CountTexture;
            private set => this.RaiseAndSetIfChanged(ref _ba2CountTexture, value);
        }

        /// <summary>
        /// Gets the total count of BA2 archives (General + Texture).
        /// </summary>
        public int BA2CountTotal => BA2CountGeneral + BA2CountTexture;

        /// <summary>
        /// Gets the count of v1 (OG) BA2 archives.
        /// </summary>
        public int BA2CountV1
        {
            get => _ba2CountV1;
            private set => this.RaiseAndSetIfChanged(ref _ba2CountV1, value);
        }

        /// <summary>
        /// Gets the count of v7/v8 (NG) BA2 archives.
        /// </summary>
        public int BA2CountV7V8
        {
            get => _ba2CountV7V8;
            private set => this.RaiseAndSetIfChanged(ref _ba2CountV7V8, value);
        }

        /// <summary>
        /// Gets the count of unreadable BA2 archives.
        /// </summary>
        public int BA2CountUnreadable
        {
            get => _ba2CountUnreadable;
            private set => this.RaiseAndSetIfChanged(ref _ba2CountUnreadable, value);
        }

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

                        // Scan BA2 archives
                        await ScanBA2ArchivesAsync();
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
                    // F4SE uses Minor.Build.Revision format (e.g., 0.7.2)
                    // ProductVersion might have commas, so clean it up
                    var productVersion = versionInfo.ProductVersion?.Replace(", ", ".").Trim();
                    F4SeVersion = !string.IsNullOrEmpty(productVersion) && productVersion != "0.0.0.0"
                        ? productVersion
                        : $"{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";

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

        /// <summary>
        /// Scans the Data directory for BA2 archives and counts them by type and version.
        /// </summary>
        private async Task ScanBA2ArchivesAsync()
        {
            // Reset counts
            BA2CountGeneral = 0;
            BA2CountTexture = 0;
            BA2CountV1 = 0;
            BA2CountV7V8 = 0;
            BA2CountUnreadable = 0;

            if (GameInfo == null || string.IsNullOrEmpty(GameInfo.DataPath))
            {
                _logger.LogDebug("Cannot scan BA2 archives: Data path is null");
                return;
            }

            if (!Directory.Exists(GameInfo.DataPath))
            {
                _logger.LogWarning("Data directory does not exist: {DataPath}", GameInfo.DataPath);
                return;
            }

            try
            {
                _logger.LogInformation("Scanning BA2 archives in: {DataPath}", GameInfo.DataPath);

                var ba2Files = Directory.GetFiles(GameInfo.DataPath, "*.ba2", SearchOption.TopDirectoryOnly);
                _logger.LogInformation("Found {Count} BA2 files", ba2Files.Length);

                foreach (var ba2File in ba2Files)
                {
                    try
                    {
                        var archiveInfo = _ba2ArchiveService.GetArchiveInfo(ba2File);
                        if (archiveInfo == null || !archiveInfo.IsValid)
                        {
                            BA2CountUnreadable++;
                            _logger.LogWarning("Unreadable BA2 archive: {FileName}", Path.GetFileName(ba2File));
                            continue;
                        }

                        // Count by type (GNRL vs DX10)
                        if (archiveInfo.Type == BA2Type.General)
                        {
                            BA2CountGeneral++;
                        }
                        else if (archiveInfo.Type == BA2Type.Texture)
                        {
                            BA2CountTexture++;
                        }
                        else
                        {
                            BA2CountUnreadable++;
                            _logger.LogWarning("Unknown BA2 type: {FileName}", Path.GetFileName(ba2File));
                            continue;
                        }

                        // Count by version (v1 vs v7/v8)
                        if (archiveInfo.Version == BA2Version.V1)
                        {
                            BA2CountV1++;
                        }
                        else if (archiveInfo.Version == BA2Version.V7 || archiveInfo.Version == BA2Version.V8)
                        {
                            BA2CountV7V8++;
                        }
                        else
                        {
                            _logger.LogDebug("Unknown version for {FileName}: {Version}",
                                Path.GetFileName(ba2File), archiveInfo.Version);
                        }
                    }
                    catch (Exception ex)
                    {
                        BA2CountUnreadable++;
                        _logger.LogError(ex, "Error reading BA2 archive: {FileName}", Path.GetFileName(ba2File));
                    }
                }

                // Raise property changed for the total count
                this.RaisePropertyChanged(nameof(BA2CountTotal));

                _logger.LogInformation("BA2 scan complete: GNRL={General}, DX10={Texture}, Total={Total}, v1={V1}, v7/8={V7V8}, Unreadable={Unreadable}",
                    BA2CountGeneral, BA2CountTexture, BA2CountTotal, BA2CountV1, BA2CountV7V8, BA2CountUnreadable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning BA2 archives");
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
