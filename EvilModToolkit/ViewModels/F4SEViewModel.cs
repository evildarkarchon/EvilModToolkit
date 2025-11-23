using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Game;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// ViewModel for the F4SE Scanner tab that scans and displays F4SE plugin compatibility.
    /// </summary>
    public class F4SEViewModel : ViewModelBase
    {
        private readonly IF4SEPluginService _pluginService;
        private readonly IGameDetectionService _gameDetectionService;
        private readonly ILogger<F4SEViewModel> _logger;

        private F4SePluginInfo? _selectedPlugin;
        private bool _showOgOnly;
        private bool _showNgOnly;
        private bool _showUniversal;
        private bool _showIncompatible;
        private string _pluginDirectory = string.Empty;
        private bool _isNextGen;

        private readonly ObservableCollection<F4SePluginInfo> _allPlugins;
        private readonly ObservableCollection<F4SePluginInfo> _filteredPlugins;

        /// <summary>
        /// Initializes a new instance of the <see cref="F4SEViewModel"/> class.
        /// </summary>
        public F4SEViewModel(
            IF4SEPluginService pluginService,
            IGameDetectionService gameDetectionService,
            ILogger<F4SEViewModel> logger)
        {
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
            _gameDetectionService =
                gameDetectionService ?? throw new ArgumentNullException(nameof(gameDetectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _allPlugins = new ObservableCollection<F4SePluginInfo>();
            _filteredPlugins = new ObservableCollection<F4SePluginInfo>();

            // Initialize filter properties to show all by default
            _showOgOnly = true;
            _showNgOnly = true;
            _showUniversal = true;
            _showIncompatible = true;

            // Create commands
            ScanPluginsCommand = ReactiveCommand.CreateFromTask(ScanPluginsAsync);

            // Detect game and plugin directory
            DetectPluginDirectory();

            // Perform initial scan if plugin directory is valid
            if (!string.IsNullOrEmpty(PluginDirectory) && System.IO.Directory.Exists(PluginDirectory))
            {
                _ = ScanPluginsAsync();
            }
        }

        #region Properties

        /// <summary>
        /// Gets the filtered collection of F4SE plugins.
        /// </summary>
        public ObservableCollection<F4SePluginInfo> Plugins => _filteredPlugins;

        /// <summary>
        /// Gets or sets the currently selected plugin.
        /// </summary>
        public F4SePluginInfo? SelectedPlugin
        {
            get => _selectedPlugin;
            set => this.RaiseAndSetIfChanged(ref _selectedPlugin, value);
        }

        /// <summary>
        /// Gets a value indicating whether the installed game is Next Gen.
        /// </summary>
        public bool IsNextGen
        {
            get => _isNextGen;
            private set => this.RaiseAndSetIfChanged(ref _isNextGen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show OG-only plugins.
        /// </summary>
        public bool ShowOgOnly
        {
            get => _showOgOnly;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _showOgOnly, value))
                    ApplyFilters();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show NG-only plugins.
        /// </summary>
        public bool ShowNgOnly
        {
            get => _showNgOnly;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _showNgOnly, value))
                    ApplyFilters();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show universal plugins (both OG and NG).
        /// </summary>
        public bool ShowUniversal
        {
            get => _showUniversal;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _showUniversal, value))
                    ApplyFilters();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show incompatible/unknown plugins.
        /// </summary>
        public bool ShowIncompatible
        {
            get => _showIncompatible;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _showIncompatible, value))
                    ApplyFilters();
            }
        }

        /// <summary>
        /// Gets the plugin directory path.
        /// </summary>
        public string PluginDirectory
        {
            get => _pluginDirectory;
            private set => this.RaiseAndSetIfChanged(ref _pluginDirectory, value);
        }

        /// <summary>
        /// Gets the total count of all scanned plugins.
        /// </summary>
        public int TotalPluginCount => _allPlugins.Count;

        /// <summary>
        /// Gets the count of OG-only plugins.
        /// </summary>
        public int OgOnlyCount => _allPlugins.Count(p => p.Compatibility == F4SeCompatibility.OgOnly);

        /// <summary>
        /// Gets the count of NG-only plugins.
        /// </summary>
        public int NgOnlyCount => _allPlugins.Count(p => p.Compatibility == F4SeCompatibility.NgOnly);

        /// <summary>
        /// Gets the count of universal plugins.
        /// </summary>
        public int UniversalCount => _allPlugins.Count(p => p.Compatibility == F4SeCompatibility.Universal);

        /// <summary>
        /// Gets the count of incompatible/unknown plugins.
        /// </summary>
        public int IncompatibleCount => _allPlugins.Count(p =>
            p.Compatibility == F4SeCompatibility.NotF4SePlugin ||
            p.Compatibility == F4SeCompatibility.Unknown);

        /// <summary>
        /// Gets the command to scan for F4SE plugins.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ScanPluginsCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Detects the F4SE plugin directory based on the game installation.
        /// </summary>
        private void DetectPluginDirectory()
        {
            try
            {
                var gameInfo = _gameDetectionService.DetectGame();
                if (gameInfo != null && gameInfo.IsInstalled)
                {
                    IsNextGen = gameInfo.IsNextGen;
                    if (!string.IsNullOrEmpty(gameInfo.InstallPath))
                    {
                        PluginDirectory = Path.Combine(gameInfo.InstallPath, "Data", "F4SE", "Plugins");
                        _logger.LogInformation("F4SE plugin directory set to: {PluginDirectory}", PluginDirectory);
                    }
                }
                else
                {
                    _logger.LogWarning("Game not detected, F4SE plugin directory not set");
                    PluginDirectory = string.Empty;
                    IsNextGen = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting F4SE plugin directory");
                PluginDirectory = string.Empty;
                IsNextGen = false;
            }
        }

        /// <summary>
        /// Scans the F4SE plugin directory for plugins.
        /// </summary>
        private async Task ScanPluginsAsync()
        {
            if (!await TryExecuteAsync(async () =>
                {
                    IsBusy = true;
                    SetStatus("Scanning F4SE plugins...");
                    ProgressPercentage = 0;
                    CreateCancellationTokenSource();

                    try
                    {
                        _allPlugins.Clear();
                        _filteredPlugins.Clear();
                        SelectedPlugin = null;

                        if (string.IsNullOrEmpty(PluginDirectory))
                        {
                            SetError("F4SE plugin directory not set. Please ensure Fallout 4 is installed.");
                            _logger.LogWarning("Cannot scan: plugin directory not set");
                            return;
                        }

                        if (!Directory.Exists(PluginDirectory))
                        {
                            SetStatus($"F4SE plugin directory not found: {PluginDirectory}");
                            _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", PluginDirectory);
                            return;
                        }

                        _logger.LogInformation("Scanning directory: {PluginDirectory}", PluginDirectory);

                        // Scan for plugins (synchronous operation, run on thread pool)
                        var plugins = await Task.Run(() =>
                                _pluginService.ScanDirectory(PluginDirectory, recursive: true),
                            CancellationToken);

                        _logger.LogInformation("Found {Count} DLL files", plugins.Count);

                        // Add plugins to collection
                        foreach (var plugin in plugins.OrderBy(p => p.FileName))
                        {
                            _allPlugins.Add(plugin);
                        }

                        // Apply filters to populate the filtered collection
                        ApplyFilters();

                        // Update statistics
                        this.RaisePropertyChanged(nameof(TotalPluginCount));
                        this.RaisePropertyChanged(nameof(OgOnlyCount));
                        this.RaisePropertyChanged(nameof(NgOnlyCount));
                        this.RaisePropertyChanged(nameof(UniversalCount));
                        this.RaisePropertyChanged(nameof(IncompatibleCount));

                        SetStatus($"Scan complete. Found {TotalPluginCount} plugins.");
                        ProgressPercentage = 100;
                        _logger.LogInformation(
                            "Scan complete: {TotalCount} total, {OgOnly} OG-only, {NgOnly} NG-only, {Universal} universal, {Incompatible} incompatible",
                            TotalPluginCount, OgOnlyCount, NgOnlyCount, UniversalCount, IncompatibleCount);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }, _logger))
            {
                _logger.LogError("Failed to scan F4SE plugins");
            }
        }

        /// <summary>
        /// Applies the current filter settings to the plugin list.
        /// </summary>
        private void ApplyFilters()
        {
            _filteredPlugins.Clear();

            foreach (var plugin in _allPlugins)
            {
                var shouldShow = plugin.Compatibility switch
                {
                    F4SeCompatibility.OgOnly => ShowOgOnly,
                    F4SeCompatibility.NgOnly => ShowNgOnly,
                    F4SeCompatibility.Universal => ShowUniversal,
                    F4SeCompatibility.NotF4SePlugin => ShowIncompatible,
                    F4SeCompatibility.Unknown => ShowIncompatible,
                    _ => false
                };

                if (shouldShow)
                {
                    _filteredPlugins.Add(plugin);
                }
            }

            // Notify UI that the Plugins collection has changed
            this.RaisePropertyChanged(nameof(Plugins));

            _logger.LogDebug("Applied filters: {FilteredCount}/{TotalCount} plugins visible",
                _filteredPlugins.Count, _allPlugins.Count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ScanPluginsCommand?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}