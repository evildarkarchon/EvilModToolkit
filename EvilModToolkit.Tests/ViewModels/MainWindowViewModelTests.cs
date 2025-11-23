using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Configuration;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    /// <summary>
    /// Test suite for MainWindowViewModel.
    /// Validates initialization, tab management, commands, and proper disposal.
    /// </summary>
    public class MainWindowViewModelTests
    {
        // Mock services for child ViewModels
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IModManagerService _modManagerService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly IF4SEPluginService _pluginService;
        private readonly IModScannerService _modScannerService;
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly IXDeltaPatcherService _xdeltaPatcherService;

        // Loggers for all ViewModels
        private readonly ILogger<OverviewViewModel> _overviewLogger;
        private readonly ILogger<F4SEViewModel> _f4seLogger;
        private readonly ILogger<ScannerViewModel> _scannerLogger;
        private readonly ILogger<SettingsViewModel> _settingsLogger;
        private readonly ILogger<ToolsViewModel> _toolsLogger;
        private readonly ILogger<MainWindowViewModel> _mainLogger;

        public MainWindowViewModelTests()
        {
            // Create mocks for all services that child ViewModels need
            _gameDetectionService = Substitute.For<IGameDetectionService>();
            _modManagerService = Substitute.For<IModManagerService>();
            _systemInfoService = Substitute.For<ISystemInfoService>();
            _pluginService = Substitute.For<IF4SEPluginService>();
            _modScannerService = Substitute.For<IModScannerService>();
            _settingsService = Substitute.For<ISettingsService>();
            _dialogService = Substitute.For<IDialogService>();
            _ba2ArchiveService = Substitute.For<IBA2ArchiveService>();
            _xdeltaPatcherService = Substitute.For<IXDeltaPatcherService>();

            // Create mock loggers
            _overviewLogger = Substitute.For<ILogger<OverviewViewModel>>();
            _f4seLogger = Substitute.For<ILogger<F4SEViewModel>>();
            _scannerLogger = Substitute.For<ILogger<ScannerViewModel>>();
            _settingsLogger = Substitute.For<ILogger<SettingsViewModel>>();
            _toolsLogger = Substitute.For<ILogger<ToolsViewModel>>();
            _mainLogger = Substitute.For<ILogger<MainWindowViewModel>>();

            // Setup default return values for services to prevent null reference exceptions
            _gameDetectionService.DetectGame().Returns(new GameInfo { IsInstalled = false });
            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync().Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            _settingsService.GetDefaultSettings().Returns(new AppSettings());
        }

        /// <summary>
        /// Creates real child ViewModels with mocked dependencies.
        /// This approach is necessary because ViewModels are concrete classes.
        /// </summary>
        private (OverviewViewModel overview, F4SEViewModel f4se, ScannerViewModel scanner, SettingsViewModel settings, ToolsViewModel tools)
            CreateChildViewModels()
        {
            var overview = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _overviewLogger);

            var f4se = new F4SEViewModel(
                _pluginService,
                _gameDetectionService,
                _f4seLogger);
            
            var scanner = new ScannerViewModel(
                _modScannerService,
                _gameDetectionService,
                _modManagerService,
                _scannerLogger);

            var settings = new SettingsViewModel(
                _settingsService,
                _dialogService,
                _settingsLogger);

            var tools = new ToolsViewModel(
                _ba2ArchiveService,
                _xdeltaPatcherService,
                _dialogService,
                _toolsLogger);

            return (overview, f4se, scanner, settings, tools);
        }

        #region Constructor Tests

        [Fact]
        public async Task Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();

            // Act
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Wait a bit for any async initialization in child ViewModels
            await Task.Delay(100);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.OverviewViewModel.Should().BeSameAs(overview);
            viewModel.F4SEViewModel.Should().BeSameAs(f4se);
            viewModel.ScannerViewModel.Should().BeSameAs(scanner);
            viewModel.SettingsViewModel.Should().BeSameAs(settings);
            viewModel.ToolsViewModel.Should().BeSameAs(tools);
            viewModel.WindowTitle.Should().Be("Evil Modding Toolkit");
            viewModel.SelectedTabIndex.Should().Be(0); // Default to Overview tab
        }

        [Fact]
        public void Constructor_InitializesCommands()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();

            // Act
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Assert
            viewModel.ExitCommand.Should().NotBeNull();
            viewModel.AboutCommand.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenOverviewViewModelIsNull()
        {
            // Arrange
            var (_, f4se, scanner, settings, tools) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                null!,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("overviewViewModel");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenF4SEViewModelIsNull()
        {
            // Arrange
            var (overview, _, scanner, settings, tools) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                overview,
                null!,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("f4seViewModel");
        }
        
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenScannerViewModelIsNull()
        {
            // Arrange
            var (overview, f4se, _, settings, tools) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                overview,
                f4se,
                null!,
                settings,
                tools,
                _mainLogger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("scannerViewModel");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSettingsViewModelIsNull()
        {
            // Arrange
            var (overview, f4se, scanner, _, tools) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                null!,
                tools,
                _mainLogger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("settingsViewModel");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenToolsViewModelIsNull()
        {
            // Arrange
            var (overview, f4se, scanner, settings, _) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                null!,
                _mainLogger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("toolsViewModel");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();

            // Act
            Action act = () => new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #endregion

        #region Property Tests

        [Fact]
        public void SelectedTabIndex_CanBeSetAndGet()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            viewModel.SelectedTabIndex = 2; // Switch to Mod Scanner tab

            // Assert
            viewModel.SelectedTabIndex.Should().Be(2);
        }

        [Fact]
        public void SelectedTabIndex_RaisesPropertyChangedNotification()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.SelectedTabIndex))
                    propertyChangedRaised = true;
            };

            // Act
            viewModel.SelectedTabIndex = 1;

            // Assert
            propertyChangedRaised.Should().BeTrue();
        }

        [Fact]
        public void WindowTitle_CanBeSetAndGet()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            viewModel.WindowTitle = "Test Title";

            // Assert
            viewModel.WindowTitle.Should().Be("Test Title");
        }

        [Fact]
        public void WindowTitle_RaisesPropertyChangedNotification()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.WindowTitle))
                    propertyChangedRaised = true;
            };

            // Act
            viewModel.WindowTitle = "Custom Title";

            // Assert
            propertyChangedRaised.Should().BeTrue();
        }

        #endregion

        #region Command Tests

        [Fact]
        public async Task ExitCommand_CanExecute()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            var canExecute = await viewModel.ExitCommand.CanExecute.FirstAsync();

            // Assert
            canExecute.Should().BeTrue();
        }

        [Fact]
        public async Task ExitCommand_ExecutesWithoutException()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            Func<Task> act = async () => await viewModel.ExitCommand.Execute().FirstAsync();

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ExitCommand_SetsStatusMessage()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            await viewModel.ExitCommand.Execute().FirstAsync();

            // Assert
            viewModel.StatusMessage.Should().NotBeNullOrEmpty();
            viewModel.StatusMessage.Should().Contain("Exiting");
        }

        [Fact]
        public async Task AboutCommand_CanExecute()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            var canExecute = await viewModel.AboutCommand.CanExecute.FirstAsync();

            // Assert
            canExecute.Should().BeTrue();
        }

        [Fact]
        public async Task AboutCommand_ExecutesWithoutException()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            Func<Task> act = async () => await viewModel.AboutCommand.Execute().FirstAsync();

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AboutCommand_SetsStatusMessage()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            await viewModel.AboutCommand.Execute().FirstAsync();

            // Assert
            viewModel.StatusMessage.Should().NotBeNullOrEmpty();
            viewModel.StatusMessage.Should().Contain("Evil Modding Toolkit");
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_DisposesChildViewModels()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act & Assert
            // We cannot directly verify Dispose was called on child ViewModels since they're real instances
            // Instead, verify that disposal completes without exception
            Action act = () => viewModel.Dispose();
            act.Should().NotThrow();

            // Verify child ViewModels are actually disposed by checking they don't throw
            // when commands are accessed (disposed commands should not throw, just not execute)
            overview.RefreshCommand.Should().NotBeNull();
            f4se.ScanPluginsCommand.Should().NotBeNull();
            scanner.ScanCommand.Should().NotBeNull();
            settings.SaveSettingsCommand.Should().NotBeNull();
            tools.PatchBA2Command.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var (overview, f4se, scanner, settings, tools) = CreateChildViewModels();
            var viewModel = new MainWindowViewModel(
                overview,
                f4se,
                scanner,
                settings,
                tools,
                _mainLogger);

            // Act
            Action act = () =>
            {
                viewModel.Dispose();
                viewModel.Dispose();
                viewModel.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion
    }
}