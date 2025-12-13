using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Game;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace EvilModToolkit.ViewModels;

/// <summary>
/// ViewModel for the Mod Scanner tab.
/// Handles scanning the game data folder for issues and displaying results.
/// </summary>
public class ScannerViewModel : ViewModelBase
{
    private readonly IModScannerService _modScannerService;
    private readonly IGameDetectionService _gameDetectionService;
    private readonly IModManagerService _modManagerService;
    private readonly ILogger<ScannerViewModel> _logger;

    private ObservableCollection<ScanResult> _results;
    private ScanResult? _selectedResult;
    private ScanOptions _scanOptions;
    private string _resultsInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScannerViewModel"/> class.
    /// </summary>
    public ScannerViewModel(
        IModScannerService modScannerService,
        IGameDetectionService gameDetectionService,
        IModManagerService modManagerService,
        ILogger<ScannerViewModel> logger)
    {
        _modScannerService = modScannerService;
        _gameDetectionService = gameDetectionService;
        _modManagerService = modManagerService;
        _logger = logger;

        _results = new ObservableCollection<ScanResult>();
        _scanOptions = new ScanOptions();
        _resultsInfo = "Ready to scan.";

        // Create commands
        ScanCommand = ReactiveCommand.CreateFromTask(ScanModsAsync);
        CancelScanCommand = ReactiveCommand.Create(CancelScan);
    }

    #region Properties

    /// <summary>
    /// Collection of scan results found.
    /// </summary>
    public ObservableCollection<ScanResult> Results
    {
        get => _results;
        set => this.RaiseAndSetIfChanged(ref _results, value);
    }

    /// <summary>
    /// The currently selected result in the UI.
    /// </summary>
    public ScanResult? SelectedResult
    {
        get => _selectedResult;
        set => this.RaiseAndSetIfChanged(ref _selectedResult, value);
    }

    /// <summary>
    /// Configuration options for the scan.
    /// </summary>
    public ScanOptions ScanOptions
    {
        get => _scanOptions;
        set => this.RaiseAndSetIfChanged(ref _scanOptions, value);
    }

    /// <summary>
    /// Status text displaying result count or state.
    /// </summary>
    public string ResultsInfo
    {
        get => _resultsInfo;
        set => this.RaiseAndSetIfChanged(ref _resultsInfo, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to start the mod scan.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ScanCommand { get; }

    /// <summary>
    /// Command to cancel an ongoing scan.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelScanCommand { get; }

    #endregion

    #region Methods

    private async Task ScanModsAsync(CancellationToken token)
    {
        IsBusy = true;
        ErrorMessage = null;
        ProgressPercentage = 0;
        SetStatus("Initializing scan...");
        Results.Clear();

        // Use ViewModelBase cancellation token source for manual cancellation
        CreateCancellationTokenSource();
        
        // Link the command token (from ReactiveCommand) with our manual cancellation token
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken);

        try
        {
            // Detect game and manager info
            var gameInfo = _gameDetectionService.DetectGame();
            
            var modManagerInfo = await _modManagerService.DetectModManagerAsync();

            if (string.IsNullOrEmpty(gameInfo.DataPath))
            {
                ErrorMessage = "Fallout 4 Data folder not found. Please configure the game path in Settings.";
                return;
            }

            var progress = new Progress<string>(status =>
            {
                SetStatus(status);
            });

            SetStatus("Scanning mod files...");
            
            var results = await _modScannerService.ScanAsync(
                gameInfo,
                modManagerInfo,
                ScanOptions,
                progress,
                linkedCts.Token);

            Results = new ObservableCollection<ScanResult>(results);
            ResultsInfo = $"{Results.Count} issues found.";
            SetStatus($"Scan complete. {Results.Count} issues found.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Scan cancelled.");
            ResultsInfo = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scan");
            ErrorMessage = $"Scan failed: {ex.Message}";
            ResultsInfo = "Scan failed.";
        }
        finally
        {
            IsBusy = false;
            ProgressPercentage = 0;
        }
    }

    private void CancelScan()
    {
        CancelOperation();
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
            ScanCommand?.Dispose();
            CancelScanCommand?.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion
}
