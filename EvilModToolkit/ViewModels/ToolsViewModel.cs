using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// ViewModel for the Tools tab that provides BA2 archive patching and game file patching (xdelta) functionality.
    /// </summary>
    public class ToolsViewModel : ViewModelBase
    {
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly IXDeltaPatcherService _xdeltaPatcherService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ToolsViewModel> _logger;

        // BA2 Archive Patcher properties
        private string _sourceBA2Path = string.Empty;
        private BA2Version _targetVersion = BA2Version.V1;
        private bool _isDirectoryMode = true;
        private string _sourceDirectory = string.Empty;
        private bool _includeSubdirectories = false;
        private BatchPatchSummary? _lastBatchResult;
        private ObservableCollection<BatchPatchResult> _batchResults = new();

        // Game Patcher (xdelta) properties
        private string _sourceFilePath = string.Empty;
        private string _patchFilePath = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolsViewModel"/> class.
        /// </summary>
        /// <param name="ba2ArchiveService">Service for BA2 archive manipulation.</param>
        /// <param name="xdeltaPatcherService">Service for applying xdelta patches.</param>
        /// <param name="dialogService">Service for file dialogs.</param>
        /// <param name="logger">Logger for diagnostic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required service is null.</exception>
        public ToolsViewModel(
            IBA2ArchiveService ba2ArchiveService,
            IXDeltaPatcherService xdeltaPatcherService,
            IDialogService dialogService,
            ILogger<ToolsViewModel> logger)
        {
            _ba2ArchiveService = ba2ArchiveService ?? throw new ArgumentNullException(nameof(ba2ArchiveService));
            _xdeltaPatcherService =
                xdeltaPatcherService ?? throw new ArgumentNullException(nameof(xdeltaPatcherService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create ReactiveCommands for BA2 patching and xdelta patching
            PatchBA2Command = ReactiveCommand.CreateFromTask(PatchBA2Async);
            ApplyPatchCommand = ReactiveCommand.CreateFromTask(ApplyPatchAsync);

            // Create browsing commands
            BrowseSourceBA2Command = ReactiveCommand.CreateFromTask(BrowseSourceBA2Async);
            BrowseSourceDirectoryCommand = ReactiveCommand.CreateFromTask(BrowseSourceDirectoryAsync);
            BrowseSourceFileCommand = ReactiveCommand.CreateFromTask(BrowseSourceFileAsync);
            BrowsePatchFileCommand = ReactiveCommand.CreateFromTask(BrowsePatchFileAsync);
        }

        #region BA2 Archive Patcher Properties

        /// <summary>
        /// Gets or sets whether to operate in directory mode (batch) or single file mode.
        /// </summary>
        public bool IsDirectoryMode
        {
            get => _isDirectoryMode;
            set => this.RaiseAndSetIfChanged(ref _isDirectoryMode, value);
        }

        /// <summary>
        /// Gets or sets the path to the source directory containing BA2 files.
        /// </summary>
        public string SourceDirectory
        {
            get => _sourceDirectory;
            set => this.RaiseAndSetIfChanged(ref _sourceDirectory, value);
        }

        /// <summary>
        /// Gets or sets whether to include subdirectories in batch operations.
        /// </summary>
        public bool IncludeSubdirectories
        {
            get => _includeSubdirectories;
            set => this.RaiseAndSetIfChanged(ref _includeSubdirectories, value);
        }

        /// <summary>
        /// Gets or sets the path to the source BA2 archive file to patch.
        /// </summary>
        public string SourceBA2Path
        {
            get => _sourceBA2Path;
            set => this.RaiseAndSetIfChanged(ref _sourceBA2Path, value);
        }

        /// <summary>
        /// Gets or sets the target BA2 version to patch to (V1, V7, or V8).
        /// </summary>
        public BA2Version TargetVersion
        {
            get => _targetVersion;
            set => this.RaiseAndSetIfChanged(ref _targetVersion, value);
        }

        /// <summary>
        /// Gets the last batch operation result summary.
        /// </summary>
        public BatchPatchSummary? LastBatchResult
        {
            get => _lastBatchResult;
            set => this.RaiseAndSetIfChanged(ref _lastBatchResult, value);
        }

        /// <summary>
        /// Gets the collection of individual file results from the last batch operation.
        /// </summary>
        public ObservableCollection<BatchPatchResult> BatchResults => _batchResults;

        /// <summary>
        /// Gets the command to patch a BA2 archive to the target version.
        /// </summary>
        public ReactiveCommand<Unit, Unit> PatchBA2Command { get; }

        /// <summary>
        /// Gets the command to browse for the source BA2 archive.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseSourceBA2Command { get; }

        /// <summary>
        /// Gets the command to browse for the source directory.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseSourceDirectoryCommand { get; }

        #endregion

        #region Game Patcher (xdelta) Properties

        /// <summary>
        /// Gets or sets the path to the source file (e.g., game executable to patch).
        /// </summary>
        public string SourceFilePath
        {
            get => _sourceFilePath;
            set => this.RaiseAndSetIfChanged(ref _sourceFilePath, value);
        }

        /// <summary>
        /// Gets or sets the path to the xdelta patch file (.xdelta).
        /// </summary>
        public string PatchFilePath
        {
            get => _patchFilePath;
            set => this.RaiseAndSetIfChanged(ref _patchFilePath, value);
        }

        /// <summary>
        /// Gets the command to apply an xdelta patch to a file.
        /// The patch will be applied in-place to the source file, with automatic backup creation.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ApplyPatchCommand { get; }

        /// <summary>
        /// Gets the command to browse for the source file to patch.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseSourceFileCommand { get; }

        /// <summary>
        /// Gets the command to browse for the xdelta patch file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowsePatchFileCommand { get; }

        #endregion

        #region Browsing Methods

        private async Task BrowseSourceBA2Async()
        {
            var file = await _dialogService.ShowFilePickerAsync(
                "Select BA2 Archive",
                "BA2 Archives",
                new[] { "*.ba2" });

            if (!string.IsNullOrEmpty(file))
            {
                SourceBA2Path = file;
            }
        }

        private async Task BrowseSourceDirectoryAsync()
        {
            var folder = await _dialogService.ShowFolderPickerAsync(
                "Select Directory with BA2 Archives",
                SourceDirectory);

            if (!string.IsNullOrEmpty(folder))
            {
                SourceDirectory = folder;
            }
        }

        private async Task BrowseSourceFileAsync()
        {
            var file = await _dialogService.ShowFilePickerAsync(
                "Select Source File",
                "Game Files",
                new[] { "*.exe", "*.dll" });

            if (!string.IsNullOrEmpty(file))
            {
                SourceFilePath = file;
            }
        }

        private async Task BrowsePatchFileAsync()
        {
            var file = await _dialogService.ShowFilePickerAsync(
                "Select Delta Patch File",
                "XDelta Patches",
                new[] { "*.xdelta" });

            if (!string.IsNullOrEmpty(file))
            {
                PatchFilePath = file;
            }
        }

        #endregion

        #region BA2 Patching Methods

        /// <summary>
        /// Patches BA2 archive(s) to the specified target version.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        private async Task PatchBA2Async()
        {
            IsBusy = true;
            ProgressPercentage = 0;
            BatchResults.Clear();
            LastBatchResult = null;

            try
            {
                if (IsDirectoryMode)
                {
                    await PatchDirectoryAsync();
                }
                else
                {
                    await PatchSingleFileAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PatchDirectoryAsync()
        {
            // Validate directory
            if (string.IsNullOrWhiteSpace(SourceDirectory) || !Directory.Exists(SourceDirectory))
            {
                SetError("Please select a valid directory.");
                return;
            }

            CreateCancellationTokenSource();

            if (!await TryExecuteAsync(async () =>
            {
                var progress = new Progress<PatchProgress>(p =>
                {
                    ProgressPercentage = p.Percentage;
                    SetStatus(p.Message);
                });

                var result = await _ba2ArchiveService.BatchPatchDirectoryAsync(
                    SourceDirectory,
                    TargetVersion,
                    IncludeSubdirectories,
                    progress,
                    CancellationToken);

                LastBatchResult = result;
                foreach (var r in result.Results)
                {
                    BatchResults.Add(r);
                }

                SetStatus($"Completed: {result.SuccessCount} patched, {result.SkippedCount} skipped, {result.FailedCount} failed");
            }, _logger))
            {
                _logger.LogError("Batch BA2 patch operation failed");
            }
        }

        private async Task PatchSingleFileAsync()
        {
            // Execute with error handling provided by ViewModelBase
            if (!await TryExecuteAsync(async () =>
                {
                    SetStatus("Patching BA2 archive...");
                    _logger.LogInformation("Starting BA2 patch operation: {SourcePath} -> {TargetVersion}",
                        SourceBA2Path, TargetVersion);

                    // Validate input path
                    if (string.IsNullOrWhiteSpace(SourceBA2Path))
                    {
                        throw new InvalidOperationException("Source BA2 path is required.");
                    }

                    if (!File.Exists(SourceBA2Path))
                    {
                        throw new FileNotFoundException($"Source BA2 file not found: {SourceBA2Path}");
                    }

                    // Validate that the file is a valid BA2 archive
                    if (!_ba2ArchiveService.IsValidBA2(SourceBA2Path))
                    {
                        throw new InvalidOperationException(
                            $"The file is not a valid BA2 archive: {SourceBA2Path}");
                    }

                    // Get current archive info to display before patching
                    var archiveInfo = _ba2ArchiveService.GetArchiveInfo(SourceBA2Path);
                    if (archiveInfo != null)
                    {
                        _logger.LogInformation("Current BA2 version: {CurrentVersion}, Target: {TargetVersion}",
                            archiveInfo.Version, TargetVersion);

                        // Check if already at target version
                        if (archiveInfo.Version == TargetVersion)
                        {
                            SetStatus($"Archive is already at version {TargetVersion}.");
                            _logger.LogInformation("BA2 archive already at target version");
                            return;
                        }
                    }

                    // Perform the patch operation (synchronous operation wrapped in Task.Run for UI responsiveness)
                    bool success = await Task.Run(() =>
                        _ba2ArchiveService.PatchArchiveVersion(SourceBA2Path, TargetVersion));

                    if (success)
                    {
                        SetStatus($"Successfully patched BA2 archive to version {TargetVersion}.");
                        _logger.LogInformation("BA2 patch completed successfully");
                    }
                    else
                    {
                        throw new InvalidOperationException("BA2 patching failed. See logs for details.");
                    }
                }, _logger))
            {
                _logger.LogError("BA2 patch operation failed");
            }
        }

        #endregion

        #region xdelta Patching Methods

        /// <summary>
        /// Applies an xdelta patch to a source file in-place with automatic backup.
        /// Creates a backup of the source file before applying the patch.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        private async Task ApplyPatchAsync()
        {
            IsBusy = true;
            ProgressPercentage = 0;
            CreateCancellationTokenSource(); // Create cancellation token for this operation
            try
            {
                // Execute with error handling and cancellation support provided by ViewModelBase
                if (!await TryExecuteAsync(async () =>
                    {
                        SetStatus("Applying xdelta patch...");
                        _logger.LogInformation("Starting xdelta patch operation: {SourceFile} + {PatchFile}",
                            SourceFilePath, PatchFilePath);

                        // Validate inputs
                        if (string.IsNullOrWhiteSpace(SourceFilePath))
                        {
                            throw new InvalidOperationException("Source file path is required.");
                        }

                        if (string.IsNullOrWhiteSpace(PatchFilePath))
                        {
                            throw new InvalidOperationException("Patch file path is required.");
                        }

                        // Validate file existence
                        if (!File.Exists(SourceFilePath))
                        {
                            throw new FileNotFoundException($"Source file not found: {SourceFilePath}");
                        }

                        if (!File.Exists(PatchFilePath))
                        {
                            throw new FileNotFoundException($"Patch file not found: {PatchFilePath}");
                        }

                        // Validate the patch before attempting to apply
                        SetStatus("Validating patch compatibility...");
                        _logger.LogInformation("Validating patch: {PatchFile} for source: {SourceFile}",
                            PatchFilePath, SourceFilePath);

                        var (isValid, validationError) = await _xdeltaPatcherService.ValidatePatchAsync(
                            SourceFilePath,
                            PatchFilePath);

                        if (!isValid)
                        {
                            throw new InvalidOperationException(
                                $"Patch validation failed: {validationError}");
                        }

                        _logger.LogInformation("Patch validation successful");

                        // Generate backup file path (e.g., Fallout4_patchBackup.exe)
                        var sourceFileInfo = new FileInfo(SourceFilePath);
                        var backupFileName =
                            $"{Path.GetFileNameWithoutExtension(sourceFileInfo.Name)}_patchBackup{sourceFileInfo.Extension}";
                        var backupFilePath = Path.Combine(sourceFileInfo.DirectoryName ?? string.Empty, backupFileName);

                        // Generate temporary output path for the patched file
                        var tempOutputPath = Path.Combine(
                            sourceFileInfo.DirectoryName ?? string.Empty,
                            $"{Path.GetFileNameWithoutExtension(sourceFileInfo.Name)}_temp{sourceFileInfo.Extension}");

                        try
                        {
                            // Create progress reporter that updates ViewModel progress state
                            var progress = new Progress<PatchProgress>(progressUpdate =>
                            {
                                // Update progress percentage
                                ProgressPercentage = progressUpdate.Percentage;

                                // Update status message with current stage
                                SetStatus($"{progressUpdate.Message} ({progressUpdate.Percentage}%)");

                                _logger.LogDebug("Patch progress: {Stage} - {Percentage}% - {Message}",
                                    progressUpdate.Stage, progressUpdate.Percentage, progressUpdate.Message);
                            });

                            // Apply the patch to a temporary output file
                            var result = await _xdeltaPatcherService.ApplyPatchAsync(
                                SourceFilePath,
                                PatchFilePath,
                                tempOutputPath,
                                progress,
                                CancellationToken);

                            // Check the result
                            if (!result.Success)
                            {
                                // Patch failed - include error details in exception
                                throw new InvalidOperationException(
                                    $"Patch operation failed: {result.ErrorMessage}\n" +
                                    $"Exit Code: {result.ExitCode}\n" +
                                    $"Standard Error: {result.StandardError}");
                            }

                            // Patch successful - now perform the backup and replacement
                            SetStatus("Creating backup and replacing source file...");
                            _logger.LogInformation("Patch successful, creating backup at: {BackupPath}",
                                backupFilePath);

                            // If a backup already exists, delete it
                            if (File.Exists(backupFilePath))
                            {
                                _logger.LogInformation("Removing existing backup file: {BackupPath}", backupFilePath);
                                File.Delete(backupFilePath);
                            }

                            // Move the original file to backup
                            File.Move(SourceFilePath, backupFilePath);
                            _logger.LogInformation("Source file backed up to: {BackupPath}", backupFilePath);

                            // Move the patched file to the original location
                            File.Move(tempOutputPath, SourceFilePath);
                            _logger.LogInformation("Patched file moved to: {SourcePath}", SourceFilePath);

                            ProgressPercentage = 100;
                            SetStatus($"Patch applied successfully. Backup saved as: {backupFileName}");
                            _logger.LogInformation(
                                "xdelta patch completed successfully. Original backed up to: {BackupPath}",
                                backupFilePath);
                        }
                        catch
                        {
                            // If something went wrong during backup/replacement, try to clean up temp file
                            if (File.Exists(tempOutputPath))
                            {
                                try
                                {
                                    File.Delete(tempOutputPath);
                                    _logger.LogInformation("Cleaned up temporary output file: {TempPath}",
                                        tempOutputPath);
                                }
                                catch (Exception cleanupEx)
                                {
                                    _logger.LogWarning(cleanupEx, "Failed to clean up temporary file: {TempPath}",
                                        tempOutputPath);
                                }
                            }

                            throw;
                        }
                    }, _logger))
                {
                    _logger.LogError("xdelta patch operation failed");
                }
            }
            finally
            {
                IsBusy = false;
                // Don't reset ProgressPercentage to 0 here - keep the final value (100 on success)
                // This allows UI to show completion status
            }
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Disposes resources used by the ViewModel, including ReactiveCommands.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose ReactiveCommands to prevent memory leaks
                PatchBA2Command?.Dispose();
                ApplyPatchCommand?.Dispose();
                BrowseSourceBA2Command?.Dispose();
                BrowseSourceFileCommand?.Dispose();
                BrowsePatchFileCommand?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}