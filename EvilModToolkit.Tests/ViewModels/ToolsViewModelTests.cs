using EvilModToolkit.Models;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for the ToolsViewModel class.
    /// </summary>
    public class ToolsViewModelTests
    {
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly IXDeltaPatcherService _xdeltaPatcherService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ToolsViewModel> _logger;

        public ToolsViewModelTests()
        {
            _ba2ArchiveService = Substitute.For<IBA2ArchiveService>();
            _xdeltaPatcherService = Substitute.For<IXDeltaPatcherService>();
            _dialogService = Substitute.For<IDialogService>();
            _logger = Substitute.For<ILogger<ToolsViewModel>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Act
            var viewModel = new ToolsViewModel(
                _ba2ArchiveService,
                _xdeltaPatcherService,
                _dialogService,
                _logger);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.SourceBA2Path.Should().BeEmpty();
            viewModel.TargetVersion.Should().Be(BA2Version.V1);
            viewModel.SourceFilePath.Should().BeEmpty();
            viewModel.PatchFilePath.Should().BeEmpty();
            viewModel.PatchBA2Command.Should().NotBeNull();
            viewModel.ApplyPatchCommand.Should().NotBeNull();
            viewModel.BrowseSourceBA2Command.Should().NotBeNull();
            viewModel.BrowseSourceFileCommand.Should().NotBeNull();
            viewModel.BrowsePatchFileCommand.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenBA2ArchiveServiceIsNull()
        {
            // Act
            Action act = () => new ToolsViewModel(
                null!,
                _xdeltaPatcherService,
                _dialogService,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ba2ArchiveService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenXDeltaPatcherServiceIsNull()
        {
            // Act
            Action act = () => new ToolsViewModel(
                _ba2ArchiveService,
                null!,
                _dialogService,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("xdeltaPatcherService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDialogServiceIsNull()
        {
            // Act
            Action act = () => new ToolsViewModel(
                _ba2ArchiveService,
                _xdeltaPatcherService,
                null!,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dialogService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new ToolsViewModel(
                _ba2ArchiveService,
                _xdeltaPatcherService,
                _dialogService,
                null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #endregion

        #region Property Tests

        [Fact]
        public void SourceBA2Path_CanBeSetAndRetrieved()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var testPath = @"C:\Games\Fallout4\Data\TestArchive.ba2";

            // Act
            viewModel.SourceBA2Path = testPath;

            // Assert
            viewModel.SourceBA2Path.Should().Be(testPath);
        }

        [Fact]
        public void TargetVersion_CanBeSetAndRetrieved()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);

            // Act
            viewModel.TargetVersion = BA2Version.V8;

            // Assert
            viewModel.TargetVersion.Should().Be(BA2Version.V8);
        }

        [Fact]
        public void SourceFilePath_CanBeSetAndRetrieved()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var testPath = @"C:\Games\Fallout4\Fallout4.exe";

            // Act
            viewModel.SourceFilePath = testPath;

            // Assert
            viewModel.SourceFilePath.Should().Be(testPath);
        }

        [Fact]
        public void PatchFilePath_CanBeSetAndRetrieved()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var testPath = @"C:\Patches\game_patch.xdelta";

            // Act
            viewModel.PatchFilePath = testPath;

            // Assert
            viewModel.PatchFilePath.Should().Be(testPath);
        }

        #endregion

        #region PatchBA2Command Tests

        [Fact]
        public async Task PatchBA2Command_ThrowsInvalidOperationException_WhenSourceBA2PathIsEmpty()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            viewModel.SourceBA2Path = string.Empty;

            // Act
            await viewModel.PatchBA2Command.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Source BA2 path is required");
        }

        [Fact]
        public async Task PatchBA2Command_SetsError_WhenSourceBA2FileNotFound()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            viewModel.SourceBA2Path = @"C:\NonExistent\Archive.ba2";
            viewModel.TargetVersion = BA2Version.V8;

            // Act
            await viewModel.PatchBA2Command.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task PatchBA2Command_SetsError_WhenFileIsNotValidBA2()
        {
            // Arrange
            // Create a temporary file that is not a BA2 archive
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "This is not a BA2 file");

            try
            {
                _ba2ArchiveService.IsValidBA2(tempFile).Returns(false);

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceBA2Path = tempFile;
                viewModel.TargetVersion = BA2Version.V8;

                // Act
                await viewModel.PatchBA2Command.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().Contain("not a valid BA2 archive");
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PatchBA2Command_PatchesSuccessfully_WhenInputIsValid()
        {
            // Arrange
            // Create a temporary file to simulate a BA2 archive
            var tempFile = Path.GetTempFileName();
            try
            {
                _ba2ArchiveService.IsValidBA2(tempFile).Returns(true);
                _ba2ArchiveService.GetArchiveInfo(tempFile).Returns(new BA2ArchiveInfo
                {
                    FilePath = tempFile,
                    Version = BA2Version.V1,
                    IsValid = true
                });
                _ba2ArchiveService.PatchArchiveVersion(tempFile, BA2Version.V8).Returns(true);

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceBA2Path = tempFile;
                viewModel.TargetVersion = BA2Version.V8;

                // Act
                await viewModel.PatchBA2Command.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().BeNullOrEmpty();
                viewModel.StatusMessage.Should().Contain("Successfully patched");
                _ba2ArchiveService.Received(1).PatchArchiveVersion(tempFile, BA2Version.V8);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PatchBA2Command_SetsStatus_WhenAlreadyAtTargetVersion()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                _ba2ArchiveService.IsValidBA2(tempFile).Returns(true);
                _ba2ArchiveService.GetArchiveInfo(tempFile).Returns(new BA2ArchiveInfo
                {
                    FilePath = tempFile,
                    Version = BA2Version.V8, // Already at target version
                    IsValid = true
                });

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceBA2Path = tempFile;
                viewModel.TargetVersion = BA2Version.V8;

                // Act
                await viewModel.PatchBA2Command.Execute().FirstAsync();

                // Assert
                viewModel.StatusMessage.Should().Contain("already at version");
                // Should not call PatchArchiveVersion when already at target version
                _ba2ArchiveService.DidNotReceive().PatchArchiveVersion(Arg.Any<string>(), Arg.Any<BA2Version>());
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PatchBA2Command_SetsError_WhenPatchingFails()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                _ba2ArchiveService.IsValidBA2(tempFile).Returns(true);
                _ba2ArchiveService.GetArchiveInfo(tempFile).Returns(new BA2ArchiveInfo
                {
                    FilePath = tempFile,
                    Version = BA2Version.V1,
                    IsValid = true
                });
                _ba2ArchiveService.PatchArchiveVersion(tempFile, BA2Version.V8).Returns(false);

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceBA2Path = tempFile;
                viewModel.TargetVersion = BA2Version.V8;

                // Act
                await viewModel.PatchBA2Command.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().Contain("patching failed");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task PatchBA2Command_SetsIsBusy_DuringExecution()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            try
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();

                _ba2ArchiveService.IsValidBA2(tempFile).Returns(true);
                _ba2ArchiveService.GetArchiveInfo(tempFile).Returns(new BA2ArchiveInfo
                {
                    FilePath = tempFile,
                    Version = BA2Version.V1,
                    IsValid = true
                });
                // PatchArchiveVersion is synchronous, but we can simulate delay by making the test wait
                _ba2ArchiveService.PatchArchiveVersion(tempFile, BA2Version.V8).Returns(x =>
                {
                    Thread.Sleep(100); // Simulate work
                    return true;
                });

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceBA2Path = tempFile;
                viewModel.TargetVersion = BA2Version.V8;

                // Act
                var commandTask = viewModel.PatchBA2Command.Execute().FirstAsync();

                // Assert - should be busy during execution
                await Task.Delay(20);
                viewModel.IsBusy.Should().BeTrue();

                // Wait for completion
                await commandTask;

                // Should not be busy after completion
                viewModel.IsBusy.Should().BeFalse();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        #endregion

        #region ApplyPatchCommand Tests

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenSourceFilePathIsEmpty()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            viewModel.SourceFilePath = string.Empty;
            viewModel.PatchFilePath = @"C:\patch.xdelta";

            // Act
            await viewModel.ApplyPatchCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Source file path is required");
        }

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenPatchFilePathIsEmpty()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            viewModel.SourceFilePath = @"C:\source.exe";
            viewModel.PatchFilePath = string.Empty;

            // Act
            await viewModel.ApplyPatchCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Patch file path is required");
        }

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenSourceFileNotFound()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            viewModel.SourceFilePath = @"C:\NonExistent\source.exe";
            viewModel.PatchFilePath = @"C:\patch.xdelta";

            // Act
            await viewModel.ApplyPatchCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Source file not found");
        }

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenPatchFileNotFound()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            try
            {
                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = @"C:\NonExistent\patch.xdelta";

                // Act
                await viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().Contain("Patch file not found");
            }
            finally
            {
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
            }
        }

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenValidationFails()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            var tempPatchFile = Path.GetTempFileName();
            try
            {
                // Setup validation to fail
                _xdeltaPatcherService.ValidatePatchAsync(tempSourceFile, tempPatchFile)
                    .Returns(Task.FromResult((false, (string?)"xdelta3.exe not found")));

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = tempPatchFile;

                // Act
                await viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().Contain("Patch validation failed");
            }
            finally
            {
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
                if (File.Exists(tempPatchFile))
                    File.Delete(tempPatchFile);
            }
        }

        [Fact]
        public async Task ApplyPatchCommand_AppliesPatchSuccessfully_WhenInputIsValid()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            var tempPatchFile = Path.GetTempFileName();
            var tempDir = Path.GetDirectoryName(tempSourceFile)!;
            var tempOutputFile = Path.Combine(tempDir,
                $"{Path.GetFileNameWithoutExtension(tempSourceFile)}_temp{Path.GetExtension(tempSourceFile)}");
            var backupFile = Path.Combine(tempDir,
                $"{Path.GetFileNameWithoutExtension(tempSourceFile)}_patchBackup{Path.GetExtension(tempSourceFile)}");

            try
            {
                // Setup validation to pass
                _xdeltaPatcherService.ValidatePatchAsync(tempSourceFile, tempPatchFile)
                    .Returns(Task.FromResult((true, (string?)null)));

                // Setup patch to succeed
                _xdeltaPatcherService.ApplyPatchAsync(
                        tempSourceFile,
                        tempPatchFile,
                        Arg.Any<string>(), // Will be temp output path
                        Arg.Any<IProgress<PatchProgress>>(),
                        Arg.Any<CancellationToken>())
                    .Returns(callInfo =>
                    {
                        // Create the temp output file to simulate successful patch
                        var outputPath = callInfo.ArgAt<string>(2);
                        File.WriteAllText(outputPath, "Patched content");
                        return Task.FromResult(new PatchResult
                        {
                            Success = true,
                            OutputFilePath = outputPath,
                            ExitCode = 0
                        });
                    });

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = tempPatchFile;

                // Act
                await viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().BeNullOrEmpty();
                viewModel.StatusMessage.Should().Contain("successfully");
                viewModel.ProgressPercentage.Should().Be(100);

                // Verify validation was called
                await _xdeltaPatcherService.Received(1).ValidatePatchAsync(tempSourceFile, tempPatchFile);

                // Verify patch was applied
                await _xdeltaPatcherService.Received(1).ApplyPatchAsync(
                    tempSourceFile,
                    tempPatchFile,
                    Arg.Any<string>(),
                    Arg.Any<IProgress<PatchProgress>>(),
                    Arg.Any<CancellationToken>());
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
                if (File.Exists(tempPatchFile))
                    File.Delete(tempPatchFile);
                if (File.Exists(tempOutputFile))
                    File.Delete(tempOutputFile);
                if (File.Exists(backupFile))
                    File.Delete(backupFile);
            }
        }

        [Fact]
        public async Task ApplyPatchCommand_ReportsProgress_DuringExecution()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            var tempPatchFile = Path.GetTempFileName();
            var tempDir = Path.GetDirectoryName(tempSourceFile)!;
            var tempOutputFile = Path.Combine(tempDir,
                $"{Path.GetFileNameWithoutExtension(tempSourceFile)}_temp{Path.GetExtension(tempSourceFile)}");
            var backupFile = Path.Combine(tempDir,
                $"{Path.GetFileNameWithoutExtension(tempSourceFile)}_patchBackup{Path.GetExtension(tempSourceFile)}");

            try
            {
                // Setup validation to pass
                _xdeltaPatcherService.ValidatePatchAsync(tempSourceFile, tempPatchFile)
                    .Returns(Task.FromResult((true, (string?)null)));

                // Capture the progress reporter passed to ApplyPatchAsync
                _xdeltaPatcherService.ApplyPatchAsync(
                        tempSourceFile,
                        tempPatchFile,
                        Arg.Any<string>(),
                        Arg.Do<IProgress<PatchProgress>>(p => { }),
                        Arg.Any<CancellationToken>())
                    .Returns(async callInfo =>
                    {
                        // Simulate progress updates
                        var progress = callInfo.ArgAt<IProgress<PatchProgress>>(3);
                        var outputPath = callInfo.ArgAt<string>(2);

                        progress?.Report(new PatchProgress
                        {
                            Stage = PatchStage.Starting,
                            Percentage = 0,
                            Message = "Starting patch"
                        });

                        await Task.Delay(50);

                        progress?.Report(new PatchProgress
                        {
                            Stage = PatchStage.Patching,
                            Percentage = 50,
                            Message = "Patching in progress"
                        });

                        await Task.Delay(50);

                        progress?.Report(new PatchProgress
                        {
                            Stage = PatchStage.Completed,
                            Percentage = 100,
                            Message = "Patch complete"
                        });

                        // Create the temp output file
                        File.WriteAllText(outputPath, "Patched content");
                        return new PatchResult { Success = true, OutputFilePath = outputPath };
                    });

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = tempPatchFile;

                // Act
                await viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Assert
                // Progress should have been reported and final state should be 100%
                viewModel.ProgressPercentage.Should().Be(100);
            }
            finally
            {
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
                if (File.Exists(tempPatchFile))
                    File.Delete(tempPatchFile);
                if (File.Exists(tempOutputFile))
                    File.Delete(tempOutputFile);
                if (File.Exists(backupFile))
                    File.Delete(backupFile);
            }
        }

        [Fact]
        public async Task ApplyPatchCommand_SetsError_WhenPatchingFails()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            var tempPatchFile = Path.GetTempFileName();
            try
            {
                // Setup validation to pass
                _xdeltaPatcherService.ValidatePatchAsync(tempSourceFile, tempPatchFile)
                    .Returns(Task.FromResult((true, (string?)null)));

                _xdeltaPatcherService.ApplyPatchAsync(
                        tempSourceFile,
                        tempPatchFile,
                        Arg.Any<string>(),
                        Arg.Any<IProgress<PatchProgress>>(),
                        Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new PatchResult
                    {
                        Success = false,
                        ErrorMessage = "Patch application failed",
                        ExitCode = 1,
                        StandardError = "Error details"
                    }));

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = tempPatchFile;

                // Act
                await viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Assert
                viewModel.ErrorMessage.Should().Contain("Patch operation failed");
            }
            finally
            {
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
                if (File.Exists(tempPatchFile))
                    File.Delete(tempPatchFile);
            }
        }

        [Fact]
        public async Task ApplyPatchCommand_HandlesCancellation_Gracefully()
        {
            // Arrange
            var tempSourceFile = Path.GetTempFileName();
            var tempPatchFile = Path.GetTempFileName();
            try
            {
                // Setup validation to pass
                _xdeltaPatcherService.ValidatePatchAsync(tempSourceFile, tempPatchFile)
                    .Returns(Task.FromResult((true, (string?)null)));

                _xdeltaPatcherService.ApplyPatchAsync(
                        tempSourceFile,
                        tempPatchFile,
                        Arg.Any<string>(),
                        Arg.Any<IProgress<PatchProgress>>(),
                        Arg.Any<CancellationToken>())
                    .Returns(async callInfo =>
                    {
                        var cancellationToken = callInfo.ArgAt<CancellationToken>(4);
                        await Task.Delay(100, cancellationToken);
                        return new PatchResult { Success = true };
                    });

                var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
                viewModel.SourceFilePath = tempSourceFile;
                viewModel.PatchFilePath = tempPatchFile;

                // Act - Start the command but don't await yet
                var commandTask = viewModel.ApplyPatchCommand.Execute().FirstAsync();

                // Give it time to start
                await Task.Delay(20);

                // Cancel the operation via Dispose (which calls CancelOperation)
                viewModel.Dispose();

                // Wait for command to complete
                await commandTask;

                // Assert - Cancellation should be handled gracefully
                viewModel.StatusMessage.Should().Contain("cancelled");
            }
            finally
            {
                if (File.Exists(tempSourceFile))
                    File.Delete(tempSourceFile);
                if (File.Exists(tempPatchFile))
                    File.Delete(tempPatchFile);
            }
        }

        #endregion

        #region Browse Commands Tests

        [Fact]
        public async Task BrowseSourceBA2Command_UpdatesSourceBA2Path_WhenFileSelected()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var selectedFile = @"C:\Archives\Test.ba2";

            _dialogService.ShowFilePickerAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<string[]>())
                .Returns(Task.FromResult((string?)selectedFile));

            // Act
            await viewModel.BrowseSourceBA2Command.Execute().FirstAsync();

            // Assert
            viewModel.SourceBA2Path.Should().Be(selectedFile);
        }

        [Fact]
        public async Task BrowseSourceBA2Command_DoesNotUpdatePath_WhenCancelled()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var initialPath = @"C:\Initial\Path.ba2";
            viewModel.SourceBA2Path = initialPath;

            _dialogService.ShowFilePickerAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<string[]>())
                .Returns(Task.FromResult<string?>(null));

            // Act
            await viewModel.BrowseSourceBA2Command.Execute().FirstAsync();

            // Assert
            viewModel.SourceBA2Path.Should().Be(initialPath);
        }

        [Fact]
        public async Task BrowseSourceFileCommand_UpdatesSourceFilePath_WhenFileSelected()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var selectedFile = @"C:\Games\Fallout4.exe";

            _dialogService.ShowFilePickerAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<string[]>())
                .Returns(Task.FromResult((string?)selectedFile));

            // Act
            await viewModel.BrowseSourceFileCommand.Execute().FirstAsync();

            // Assert
            viewModel.SourceFilePath.Should().Be(selectedFile);
        }

        [Fact]
        public async Task BrowsePatchFileCommand_UpdatesPatchFilePath_WhenFileSelected()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);
            var selectedFile = @"C:\Patches\patch.xdelta";

            _dialogService.ShowFilePickerAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<string[]>())
                .Returns(Task.FromResult((string?)selectedFile));

            // Act
            await viewModel.BrowsePatchFileCommand.Execute().FirstAsync();

            // Assert
            viewModel.PatchFilePath.Should().Be(selectedFile);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_DisposesCommands()
        {
            // Arrange
            var viewModel = new ToolsViewModel(_ba2ArchiveService, _xdeltaPatcherService, _dialogService, _logger);

            // Act
            viewModel.Dispose();

            // Assert - should not throw on multiple dispose calls
            Action act = () => viewModel.Dispose();
            act.Should().NotThrow();
        }

        #endregion
    }
}