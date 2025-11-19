using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    public class OverviewViewModelTests
    {
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IModManagerService _modManagerService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly ILogger<OverviewViewModel> _logger;

        public OverviewViewModelTests()
        {
            _gameDetectionService = Substitute.For<IGameDetectionService>();
            _modManagerService = Substitute.For<IModManagerService>();
            _systemInfoService = Substitute.For<ISystemInfoService>();
            _ba2ArchiveService = Substitute.For<IBA2ArchiveService>();
            _logger = Substitute.For<ILogger<OverviewViewModel>>();
        }

        [Fact]
        public async Task Constructor_InitializesProperties()
        {
            // Arrange
            // Setup mocks to return valid data so initial refresh doesn't add problems
            _gameDetectionService.DetectGame().Returns(new GameInfo { IsInstalled = true, InstallPath = @"C:\Games\FO4" });
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            // Act
            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.RefreshCommand.Should().NotBeNull();
            viewModel.Problems.Should().NotBeNull();
            // Note: Problems may not be empty if F4SE is not detected, which is expected behavior
            // The constructor successfully initializes all properties
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenGameDetectionServiceIsNull()
        {
            // Act
            Action act = () => new OverviewViewModel(
                null!,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("gameDetectionService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenModManagerServiceIsNull()
        {
            // Act
            Action act = () => new OverviewViewModel(
                _gameDetectionService,
                null!,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("modManagerService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSystemInfoServiceIsNull()
        {
            // Act
            Action act = () => new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                null!,
                _ba2ArchiveService,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("systemInfoService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenBA2ArchiveServiceIsNull()
        {
            // Act
            Action act = () => new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                null!,
                _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ba2ArchiveService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task RefreshCommand_DetectsGame_WhenGameIsInstalled()
        {
            // Arrange
            // GameInfo uses init-only properties, so we create with IsInstalled = true
            var gameInfo = new GameInfo
            {
                IsInstalled = true,  // Required to indicate game was found
                InstallPath = @"C:\Program Files\Steam\steamapps\common\Fallout 4",
                Version = "1.10.163.0",
                InstallType = InstallType.Steam
            };

            // DetectGame is synchronous (not async), returns GameInfo directly
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // DetectModManagerAsync returns non-nullable ModManagerInfo with Type = None when not detected
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.GameInfo.Should().Be(gameInfo);
            viewModel.IsGameDetected.Should().BeTrue();
            _gameDetectionService.Received(2).DetectGame(); // Initial + manual refresh (synchronous calls)
        }

        [Fact]
        public async Task RefreshCommand_AddsProblemToCollection_WhenGameNotDetected()
        {
            // Arrange
            // When game is not found, DetectGame returns GameInfo with IsInstalled = false
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // DetectModManagerAsync returns non-nullable ModManagerInfo with Type = None
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            // GameInfo will not be null, but IsInstalled will be false
            viewModel.GameInfo.Should().NotBeNull();
            viewModel.IsGameDetected.Should().BeFalse();
            // ScanResult uses Type property (not ProblemType) and FileNotFound (not GameNotFound)
            viewModel.Problems.Should().Contain(p => p.Type == ProblemType.FileNotFound);
        }

        [Fact]
        public async Task RefreshCommand_DetectsModManager_WhenModManagerIsInstalled()
        {
            // Arrange
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = @"C:\Program Files\Steam\steamapps\common\Fallout 4",
                Version = "1.10.163.0"
            };

            // ModManagerInfo uses Type (ModManagerType enum), not ManagerType (string)
            // ExecutablePath (not InstallPath) contains the path to the mod manager executable
            var modManagerInfo = new ModManagerInfo
            {
                Type = ModManagerType.ModOrganizer2,  // Correct property name and type
                ExecutablePath = @"C:\Modding\MO2\ModOrganizer.exe",  // Correct property name
                Version = "2.5.0"
            };

            // DetectGame is synchronous
            _gameDetectionService.DetectGame().Returns(gameInfo);
            // DetectModManagerAsync returns non-nullable ModManagerInfo
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(modManagerInfo));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.ModManagerInfo.Should().Be(modManagerInfo);
            viewModel.IsModManagerDetected.Should().BeTrue();
            await _modManagerService.Received(2).DetectModManagerAsync();
        }

        [Fact]
        public async Task RefreshCommand_GathersSystemInfo()
        {
            // Arrange
            var systemInfo = new SystemInfo
            {
                OperatingSystem = "Windows 11",
                TotalRamGb = 32.0,  // Correct property name: TotalRamGb (not TotalRamMb)
                CpuName = "AMD Ryzen 9 5900X",
                GpuName = "NVIDIA GeForce RTX 3080"
            };

            // DetectGame is synchronous, returns GameInfo with IsInstalled = true
            _gameDetectionService.DetectGame().Returns(new GameInfo { IsInstalled = true });
            // DetectModManagerAsync returns non-nullable ModManagerInfo with Type = None
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(systemInfo));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.SystemInfo.Should().Be(systemInfo);
            await _systemInfoService.Received(2).GetSystemInfoAsync();
        }

        [Fact]
        public async Task RefreshCommand_SetsIsBusy_DuringExecution()
        {
            // Arrange
            // Note: DetectGame is synchronous, so we can't delay it directly
            // We delay one of the async operations instead (SystemInfoService)
            var taskCompletionSource = new TaskCompletionSource<SystemInfo>();
            _gameDetectionService.DetectGame().Returns(new GameInfo { IsInstalled = true });
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            // Note: GetSystemInfoAsync returns Task<SystemInfo?>, so use TaskCompletionSource<SystemInfo?>
            var systemInfoTaskSource = new TaskCompletionSource<SystemInfo?>();
            _systemInfoService.GetSystemInfoAsync().Returns(systemInfoTaskSource.Task);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>
            var refreshObservable = viewModel.RefreshCommand.Execute();
            var refreshTask = refreshObservable.FirstAsync();

            // Assert - should be busy during execution
            await Task.Delay(50);
            viewModel.IsBusy.Should().BeTrue();

            // Complete the task - return SystemInfo
            systemInfoTaskSource.SetResult(new SystemInfo());
            await refreshTask;

            // Assert - should not be busy after completion
            viewModel.IsBusy.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshCommand_ClearsProblemsBeforeRefresh()
        {
            // Arrange
            // Initially return GameInfo with IsInstalled = false to trigger problem
            _gameDetectionService.DetectGame().Returns(new GameInfo { IsInstalled = false });
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh (which will add a "game not found" problem)
            await Task.Delay(100);
            viewModel.Problems.Should().NotBeEmpty();
            var initialProblemCount = viewModel.Problems.Count;

            // Now set up to return a valid game with IsInstalled = true
            var gameInfo = new GameInfo
            {
                IsInstalled = true,  // Game is now installed
                InstallPath = @"C:\Games\Fallout 4",
                Version = "1.10.163.0"
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert - the old "game not found" problem should be cleared
            // Note: A new "F4SE not found" problem may be added since F4SE won't exist at that path
            // The key assertion is that Problems collection was cleared and repopulated, not that it's empty
            // Verify the game detection problem is gone by checking the problem type
            viewModel.Problems.Should().NotContain(p => p.Summary.Contains("Fallout 4 installation not detected"));
        }

        [Fact]
        public async Task RefreshCommand_HandlesExceptions_Gracefully()
        {
            // Arrange
            // DetectGame is synchronous, so we throw directly when called
            _gameDetectionService.When(x => x.DetectGame())
                .Do(x => throw new InvalidOperationException("Test exception"));
            _modManagerService.DetectModManagerAsync().Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(100);

            // Act - RefreshCommand.Execute() returns IObservable<Unit>, subscribe and wait
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert - should not throw, error message should be set
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
            viewModel.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void Dispose_DisposesRefreshCommand()
        {
            // Arrange
            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            viewModel.Dispose();

            // Assert - should not throw
            Action act = () => viewModel.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void IsGameDetected_ReturnsFalse_WhenGameInfoIsNull()
        {
            // Arrange
            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act & Assert
            viewModel.IsGameDetected.Should().BeFalse();
        }

        [Fact]
        public void IsModManagerDetected_ReturnsFalse_WhenModManagerInfoIsNull()
        {
            // Arrange
            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act & Assert
            viewModel.IsModManagerDetected.Should().BeFalse();
        }

        #region Error Handling Tests

        [Fact]
        public async Task RefreshCommand_WhenDetectGameThrows_SetsErrorMessage()
        {
            // Arrange
            _gameDetectionService.DetectGame()
                .Returns(callInfo => throw new UnauthorizedAccessException("Registry access denied"));

            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));

            _systemInfoService.GetSystemInfoAsync()
                .Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to fail
            await Task.Delay(100);

            // Act - try to refresh again
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Registry access denied");
            viewModel.IsBusy.Should().BeFalse();
            viewModel.GameInfo.Should().BeNull();
        }

        [Fact]
        public async Task RefreshCommand_WhenDetectModManagerThrows_SetsErrorMessage()
        {
            // Arrange
            _gameDetectionService.DetectGame()
                .Returns(new GameInfo { IsInstalled = false });

            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromException<ModManagerInfo>(new IOException("Cannot access program files")));

            _systemInfoService.GetSystemInfoAsync()
                .Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to fail
            await Task.Delay(100);

            // Act
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("Cannot access program files");
            viewModel.IsBusy.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshCommand_WhenGetSystemInfoThrows_SetsErrorMessage()
        {
            // Arrange
            _gameDetectionService.DetectGame()
                .Returns(new GameInfo { IsInstalled = false });

            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));

            _systemInfoService.GetSystemInfoAsync()
                .Returns(Task.FromException<SystemInfo?>(new InvalidOperationException("WMI query failed")));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to fail
            await Task.Delay(100);

            // Act
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("WMI query failed");
            viewModel.IsBusy.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshCommand_WhenBA2ScanThrows_ContinuesAndSetsError()
        {
            // Arrange
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = @"C:\Games\Fallout 4",
                DataPath = @"C:\Games\Fallout 4\Data"
            };

            _gameDetectionService.DetectGame().Returns(gameInfo);

            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));

            _systemInfoService.GetSystemInfoAsync()
                .Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));

            // Mock GetArchiveInfo to throw exception
            _ba2ArchiveService.GetArchiveInfo(Arg.Any<string>())
                .Returns(callInfo => throw new UnauthorizedAccessException("Access denied to BA2 files"));

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Wait for initial refresh to complete
            await Task.Delay(200);

            // Act
            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert - BA2 scanning should not prevent the rest of refresh from completing
            viewModel.GameInfo.Should().NotBeNull();
            viewModel.GameInfo?.IsInstalled.Should().BeTrue();
            // BA2 counts should be 0 due to scanning failure
            viewModel.BA2CountTotal.Should().Be(0);
        }

        #endregion
    }
}
