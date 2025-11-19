using EvilModToolkit.Models;
using EvilModToolkit.Services.Configuration;
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
    /// Unit tests for the SettingsViewModel class.
    /// Tests cover constructor initialization, settings loading, property change notifications,
    /// save/reset commands, validation, and disposal.
    /// </summary>
    public class SettingsViewModelTests
    {
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SettingsViewModel> _logger;

        /// <summary>
        /// Initializes a new instance of the test class with mocked dependencies.
        /// </summary>
        public SettingsViewModelTests()
        {
            _settingsService = Substitute.For<ISettingsService>();
            _dialogService = Substitute.For<IDialogService>();
            _logger = Substitute.For<ILogger<SettingsViewModel>>();
        }

        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor initializes all properties correctly with valid dependencies.
        /// Commands should be created and settings should be loaded.
        /// </summary>
        [Fact]
        public async Task Constructor_InitializesProperties_WithValidDependencies()
        {
            // Arrange
            var testSettings = new AppSettings
            {
                GamePathOverride = @"C:\Games\Fallout 4",
                Theme = "Dark",
                LogLevel = "Debug"
            };
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(testSettings));

            // Act
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);

            // Wait for async initialization to complete
            await Task.Delay(100);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.SaveSettingsCommand.Should().NotBeNull();
            viewModel.ResetDefaultsCommand.Should().NotBeNull();

            // Verify settings were loaded
            await _settingsService.Received(1).LoadSettingsAsync();
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when settingsService is null.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenSettingsServiceIsNull()
        {
            // Act
            Action act = () => new SettingsViewModel(null!, _dialogService, _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("settingsService");
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when dialogService is null.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDialogServiceIsNull()
        {
            // Act
            Action act = () => new SettingsViewModel(_settingsService, null!, _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dialogService");
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when logger is null.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new SettingsViewModel(_settingsService, _dialogService, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        /// <summary>
        /// Verifies that settings are loaded from the service on construction.
        /// </summary>
        [Fact]
        public async Task Constructor_LoadsSettings_OnInitialization()
        {
            // Arrange
            var testSettings = new AppSettings
            {
                GamePathOverride = @"C:\TestPath",
                MO2PathOverride = @"C:\MO2",
                VortexPathOverride = @"C:\Vortex",
                ScanF4SERecursively = false,
                WindowWidth = 1600,
                WindowHeight = 900,
                ShowHiddenFiles = true,
                Theme = "Light",
                LogLevel = "Warning"
            };
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(testSettings));

            // Act
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);

            // Wait for async loading
            await Task.Delay(100);

            // Assert - verify all properties were loaded
            viewModel.GamePathOverride.Should().Be(@"C:\TestPath");
            viewModel.MO2PathOverride.Should().Be(@"C:\MO2");
            viewModel.VortexPathOverride.Should().Be(@"C:\Vortex");
            viewModel.ScanF4SERecursively.Should().BeFalse();
            viewModel.WindowWidth.Should().Be(1600);
            viewModel.WindowHeight.Should().Be(900);
            viewModel.ShowHiddenFiles.Should().BeTrue();
            viewModel.Theme.Should().Be("Light");
            viewModel.LogLevel.Should().Be("Warning");
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that GamePathOverride property can be set and get correctly.
        /// Should raise PropertyChanged event when value changes.
        /// </summary>
        [Fact]
        public async Task GamePathOverride_CanBeSetAndGet()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.GamePathOverride))
                    propertyChangedRaised = true;
            };

            // Act
            viewModel.GamePathOverride = @"C:\NewPath\Fallout4";

            // Assert
            viewModel.GamePathOverride.Should().Be(@"C:\NewPath\Fallout4");
            propertyChangedRaised.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that MO2PathOverride property can be set and get correctly.
        /// </summary>
        [Fact]
        public async Task MO2PathOverride_CanBeSetAndGet()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            viewModel.MO2PathOverride = @"C:\Modding\MO2";

            // Assert
            viewModel.MO2PathOverride.Should().Be(@"C:\Modding\MO2");
        }

        /// <summary>
        /// Verifies that ScanF4SERecursively property can be set and get correctly.
        /// </summary>
        [Fact]
        public async Task ScanF4SERecursively_CanBeSetAndGet()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            viewModel.ScanF4SERecursively = false;

            // Assert
            viewModel.ScanF4SERecursively.Should().BeFalse();

            // Act - toggle back
            viewModel.ScanF4SERecursively = true;

            // Assert
            viewModel.ScanF4SERecursively.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that Theme property can be set and get correctly.
        /// </summary>
        [Fact]
        public async Task Theme_CanBeSetAndGet()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            viewModel.Theme = "Dark";

            // Assert
            viewModel.Theme.Should().Be("Dark");
        }

        /// <summary>
        /// Verifies that WindowWidth and WindowHeight properties can be set and get correctly.
        /// </summary>
        [Fact]
        public async Task WindowDimensions_CanBeSetAndGet()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            viewModel.WindowWidth = 1920;
            viewModel.WindowHeight = 1080;

            // Assert
            viewModel.WindowWidth.Should().Be(1920);
            viewModel.WindowHeight.Should().Be(1080);
        }

        #endregion

        #region SaveSettingsCommand Tests

        /// <summary>
        /// Verifies that SaveSettingsCommand successfully saves valid settings.
        /// Should call the settings service and set success status message.
        /// </summary>
        [Fact]
        public async Task SaveSettingsCommand_SavesSettings_Successfully()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            _settingsService.SaveSettingsAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Set valid settings
            viewModel.Theme = "Dark";
            viewModel.LogLevel = "Information";
            viewModel.WindowWidth = 1200;
            viewModel.WindowHeight = 800;

            // Act
            await viewModel.SaveSettingsCommand.Execute().FirstAsync();

            // Assert
            await _settingsService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s =>
                s.Theme == "Dark" &&
                s.LogLevel == "Information" &&
                s.WindowWidth == 1200 &&
                s.WindowHeight == 800));
            viewModel.StatusMessage.Should().Contain("saved successfully");
        }

        /// <summary>
        /// Verifies that SaveSettingsCommand validates settings before saving.
        /// Should reject invalid settings and set error message.
        /// </summary>
        [Fact]
        public async Task SaveSettingsCommand_ValidatesSettings_BeforeSaving()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Set invalid window dimensions (too small)
            viewModel.WindowWidth = 100; // Below minimum of 400
            viewModel.WindowHeight = 100; // Below minimum of 400

            // Act
            await viewModel.SaveSettingsCommand.Execute().FirstAsync();

            // Assert - should not call save due to validation failure
            await _settingsService.DidNotReceive().SaveSettingsAsync(Arg.Any<AppSettings>());
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
            viewModel.ErrorMessage.Should().Contain("Invalid settings");
        }

        /// <summary>
        /// Verifies that SaveSettingsCommand sets IsBusy during execution.
        /// </summary>
        [Fact]
        public async Task SaveSettingsCommand_SetsIsBusy_DuringExecution()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var saveTaskSource = new TaskCompletionSource<bool>();
            _settingsService.SaveSettingsAsync(Arg.Any<AppSettings>())
                .Returns(async callInfo => { await saveTaskSource.Task; });

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act - Start save command
            var saveTask = viewModel.SaveSettingsCommand.Execute().FirstAsync();

            // Brief delay to allow command to start
            await Task.Delay(50);

            // Assert - should be busy during execution
            viewModel.IsBusy.Should().BeTrue();

            // Complete the save
            saveTaskSource.SetResult(true);
            await saveTask;

            // Assert - should not be busy after completion
            viewModel.IsBusy.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that SaveSettingsCommand handles exceptions gracefully.
        /// Should set error message and not crash.
        /// </summary>
        [Fact]
        public async Task SaveSettingsCommand_HandlesExceptions_Gracefully()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            _settingsService.SaveSettingsAsync(Arg.Any<AppSettings>())
                .Returns(callInfo => throw new InvalidOperationException("Test save exception"));

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            await viewModel.SaveSettingsCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
            viewModel.IsBusy.Should().BeFalse();
        }

        #endregion

        #region ResetDefaultsCommand Tests

        /// <summary>
        /// Verifies that ResetDefaultsCommand resets all settings to defaults.
        /// Should call GetDefaultSettings and update all properties.
        /// </summary>
        [Fact]
        public async Task ResetDefaultsCommand_ResetsAllSettings_ToDefaults()
        {
            // Arrange
            var loadedSettings = new AppSettings
            {
                GamePathOverride = @"C:\CustomPath",
                Theme = "Dark",
                LogLevel = "Debug",
                WindowWidth = 1920,
                WindowHeight = 1080
            };

            var defaultSettings = new AppSettings
            {
                GamePathOverride = null,
                Theme = "System",
                LogLevel = "Information",
                WindowWidth = 1200,
                WindowHeight = 800
            };

            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(loadedSettings));
            _settingsService.GetDefaultSettings().Returns(defaultSettings);

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Verify loaded settings are applied
            viewModel.GamePathOverride.Should().Be(@"C:\CustomPath");
            viewModel.Theme.Should().Be("Dark");

            // Act
            await viewModel.ResetDefaultsCommand.Execute().FirstAsync();

            // Assert
            _settingsService.Received(1).GetDefaultSettings();
            viewModel.GamePathOverride.Should().BeNull();
            viewModel.Theme.Should().Be("System");
            viewModel.LogLevel.Should().Be("Information");
            viewModel.WindowWidth.Should().Be(1200);
            viewModel.WindowHeight.Should().Be(800);
            viewModel.StatusMessage.Should().Contain("reset to defaults");
        }

        /// <summary>
        /// Verifies that ResetDefaultsCommand does not automatically save.
        /// User must explicitly call SaveSettings to persist the reset.
        /// </summary>
        [Fact]
        public async Task ResetDefaultsCommand_DoesNotAutomaticallySave()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            _settingsService.GetDefaultSettings().Returns(new AppSettings());

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            await viewModel.ResetDefaultsCommand.Execute().FirstAsync();

            // Assert - SaveSettingsAsync should not be called by reset
            await _settingsService.DidNotReceive().SaveSettingsAsync(Arg.Any<AppSettings>());
            viewModel.StatusMessage.Should().Contain("not saved yet");
        }

        /// <summary>
        /// Verifies that ResetDefaultsCommand handles exceptions gracefully.
        /// </summary>
        [Fact]
        public async Task ResetDefaultsCommand_HandlesExceptions_Gracefully()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            _settingsService.When(x => x.GetDefaultSettings())
                .Do(x => throw new InvalidOperationException("Test reset exception"));

            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            await viewModel.ResetDefaultsCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Load Settings Tests

        /// <summary>
        /// Verifies that LoadSettings handles service exceptions gracefully.
        /// Should log error and use default values when loading fails.
        /// </summary>
        [Fact]
        public async Task LoadSettings_HandlesExceptions_Gracefully()
        {
            // Arrange
            _settingsService.LoadSettingsAsync()
                .Returns<Task<AppSettings>>(x => throw new InvalidOperationException("Test load exception"));

            // Act
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(100);

            // Assert - should not crash, error should be set
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Disposal Tests

        /// <summary>
        /// Verifies that Dispose method disposes both commands.
        /// Multiple calls to Dispose should not throw exceptions.
        /// </summary>
        [Fact]
        public async Task Dispose_DisposesCommands_WithoutThrowing()
        {
            // Arrange
            _settingsService.LoadSettingsAsync().Returns(Task.FromResult(new AppSettings()));
            var viewModel = new SettingsViewModel(_settingsService, _dialogService, _logger);
            await Task.Delay(50);

            // Act
            viewModel.Dispose();

            // Assert - Dispose should be idempotent (can call multiple times without error)
            Action act = () => viewModel.Dispose();
            act.Should().NotThrow();
        }

        #endregion
    }
}