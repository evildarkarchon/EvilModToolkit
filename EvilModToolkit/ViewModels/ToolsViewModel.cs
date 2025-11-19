using EvilModToolkit.Models;
using EvilModToolkit.Services.Patching;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace EvilModToolkit.ViewModels
{
    /// <summary>
    /// ViewModel for the Tools tab that provides BA2 archive patching and game file patching (xdelta) functionality.
    /// </summary>
    public class ToolsViewModel : ViewModelBase
    {
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly IXDeltaPatcherService _xdeltaPatcherService;
        private readonly ILogger<ToolsViewModel> _logger;

        // BA2 Archive Patcher properties
        private string _sourceBA2Path = string.Empty;
        private BA2Version _targetVersion = BA2Version.V1;

        // Game Patcher (xdelta) properties
        private string _sourceFilePath = string.Empty;
        private string _patchFilePath = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolsViewModel"/> class.
        /// </summary>
        /// <param name="ba2ArchiveService">Service for BA2 archive manipulation.</param>
        /// <param name="xdeltaPatcherService">Service for applying xdelta patches.</param>
        /// <param name="logger">Logger for diagnostic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required service is null.</exception>
        public ToolsViewModel(
            IBA2ArchiveService ba2ArchiveService,
            IXDeltaPatcherService xdeltaPatcherService,
            ILogger<ToolsViewModel> logger)
        {
            _ba2ArchiveService = ba2ArchiveService ?? throw new ArgumentNullException(nameof(ba2ArchiveService));
            _xdeltaPatcherService = xdeltaPatcherService ?? throw new ArgumentNullException(nameof(xdeltaPatcherService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create ReactiveCommands for BA2 patching and xdelta patching
            PatchBA2Command = ReactiveCommand.CreateFromTask(PatchBA2Async);
            ApplyPatchCommand = ReactiveCommand.CreateFromTask(ApplyPatchAsync);
        }

        #region BA2 Archive Patcher Properties

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
        /// Gets the command to patch a BA2 archive to the target version.
        /// </summary>
        public ReactiveCommand<Unit, Unit> PatchBA2Command { get; }

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

        #endregion

        #region BA2 Patching Methods

        /// <summary>
        /// Patches a BA2 archive file to the specified target version.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        private async Task PatchBA2Async()
        {
            IsBusy = true;
            try
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
                        throw new InvalidOperationException($"The file is not a valid BA2 archive: {SourceBA2Path}");
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
            finally
            {
                IsBusy = false;
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
                    var backupFileName = $"{Path.GetFileNameWithoutExtension(sourceFileInfo.Name)}_patchBackup{sourceFileInfo.Extension}";
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
                        _logger.LogInformation("Patch successful, creating backup at: {BackupPath}", backupFilePath);

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
                        _logger.LogInformation("xdelta patch completed successfully. Original backed up to: {BackupPath}", backupFilePath);
                    }
                    catch
                    {
                        // If something went wrong during backup/replacement, try to clean up temp file
                        if (File.Exists(tempOutputPath))
                        {
                            try
                            {
                                File.Delete(tempOutputPath);
                                _logger.LogInformation("Cleaned up temporary output file: {TempPath}", tempOutputPath);
                            }
                            catch (Exception cleanupEx)
                            {
                                _logger.LogWarning(cleanupEx, "Failed to clean up temporary file: {TempPath}", tempOutputPath);
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
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
