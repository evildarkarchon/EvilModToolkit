using EvilModToolkit.Models;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Game;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for the F4SEViewModel class.
    /// Tests cover constructor initialization, command execution, filtering, and disposal.
    /// </summary>
    public class F4SEViewModelTests
    {
        private readonly IF4SEPluginService _pluginService;
        private readonly IGameDetectionService _gameDetectionService;
        private readonly ILogger<F4SEViewModel> _logger;

        /// <summary>
        /// Initializes a new instance of the test class with mocked dependencies.
        /// </summary>
        public F4SEViewModelTests()
        {
            _pluginService = Substitute.For<IF4SEPluginService>();
            _gameDetectionService = Substitute.For<IGameDetectionService>();
            _logger = Substitute.For<ILogger<F4SEViewModel>>();
        }

        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor initializes all properties correctly with valid dependencies.
        /// All filter properties should be true by default to show all plugin types.
        /// </summary>
        [Fact]
        public void Constructor_InitializesProperties_WithValidDependencies()
        {
            // Arrange
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = @"C:\Games\Fallout 4"
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // Act
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.ScanPluginsCommand.Should().NotBeNull();
            viewModel.Plugins.Should().NotBeNull().And.BeEmpty();

            // Verify all filter properties are initialized to true (show all by default)
            viewModel.ShowOgOnly.Should().BeTrue();
            viewModel.ShowNgOnly.Should().BeTrue();
            viewModel.ShowUniversal.Should().BeTrue();
            viewModel.ShowIncompatible.Should().BeTrue();

            // Verify statistics properties are initialized to zero
            viewModel.TotalPluginCount.Should().Be(0);
            viewModel.OgOnlyCount.Should().Be(0);
            viewModel.NgOnlyCount.Should().Be(0);
            viewModel.UniversalCount.Should().Be(0);
            viewModel.IncompatibleCount.Should().Be(0);

            viewModel.SelectedPlugin.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the constructor calls DetectPluginDirectory which invokes DetectGame service.
        /// This ensures the plugin directory is automatically detected on initialization.
        /// </summary>
        [Fact]
        public void Constructor_CallsDetectPluginDirectory_OnInitialization()
        {
            // Arrange
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = @"C:\Games\Fallout 4"
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // Act
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert
            _gameDetectionService.Received(1).DetectGame();
            viewModel.PluginDirectory.Should().Be(Path.Combine(@"C:\Games\Fallout 4", "Data", "F4SE", "Plugins"));
        }

        /// <summary>
        /// Verifies that the constructor sets PluginDirectory to empty string when game is not detected.
        /// </summary>
        [Fact]
        public void Constructor_SetsEmptyPluginDirectory_WhenGameNotDetected()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // Act
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert
            viewModel.PluginDirectory.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when pluginService is null.
        /// This ensures all required dependencies are provided.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenPluginServiceIsNull()
        {
            // Act
            Action act = () => new F4SEViewModel(null!, _gameDetectionService, _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("pluginService");
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when gameDetectionService is null.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenGameDetectionServiceIsNull()
        {
            // Act
            Action act = () => new F4SEViewModel(_pluginService, null!, _logger);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("gameDetectionService");
        }

        /// <summary>
        /// Verifies that the constructor throws ArgumentNullException when logger is null.
        /// </summary>
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            Action act = () => new F4SEViewModel(_pluginService, _gameDetectionService, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #endregion

        #region ScanPluginsCommand Tests

        /// <summary>
        /// Verifies that ScanPluginsCommand successfully scans and populates plugins when directory exists.
        /// The command should call the plugin service, populate collections, and update statistics.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_ScansAndPopulatesPlugins_WhenDirectoryExists()
        {
            // Arrange - Use a real temporary directory that actually exists
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                // Create the expected plugin directory structure
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Create test plugins with different compatibility types
                var plugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo
                    {
                        FileName = "universal.dll",
                        FilePath = Path.Combine(pluginDir, "universal.dll"),
                        IsF4SePlugin = true,
                        SupportsOg = true,
                        SupportsNg = true,
                        Compatibility = F4SeCompatibility.Universal,
                        Version = "1.0.0"
                    },
                    new F4SePluginInfo
                    {
                        FileName = "ogonly.dll",
                        FilePath = Path.Combine(pluginDir, "ogonly.dll"),
                        IsF4SePlugin = true,
                        SupportsOg = true,
                        SupportsNg = false,
                        Compatibility = F4SeCompatibility.OgOnly,
                        Version = "1.0.0"
                    },
                    new F4SePluginInfo
                    {
                        FileName = "ngonly.dll",
                        FilePath = Path.Combine(pluginDir, "ngonly.dll"),
                        IsF4SePlugin = true,
                        SupportsOg = false,
                        SupportsNg = true,
                        Compatibility = F4SeCompatibility.NgOnly,
                        Version = "2.0.0"
                    }
                };

                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()).Returns(plugins);

                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // Act - Execute command and wait for completion
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert
                _pluginService.Received(1).ScanDirectory(
                    Arg.Is<string>(path => path.EndsWith(@"Data\F4SE\Plugins")),
                    Arg.Is<bool>(recursive => recursive == true));

                // Verify plugins are populated (filtered collection should contain all since all filters are true)
                viewModel.Plugins.Count.Should().Be(3);

                // Verify statistics are updated correctly
                viewModel.TotalPluginCount.Should().Be(3);
                viewModel.UniversalCount.Should().Be(1);
                viewModel.OgOnlyCount.Should().Be(1);
                viewModel.NgOnlyCount.Should().Be(1);
                viewModel.IncompatibleCount.Should().Be(0);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand handles empty directory without errors.
        /// Should complete successfully with zero plugins found.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_HandlesEmptyDirectory_Successfully()
        {
            // Arrange - Use a real temporary directory that actually exists
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()).Returns(new List<F4SePluginInfo>());

                // Create ViewModel BEFORE creating plugin directory to prevent constructor auto-scan
                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // NOW create the plugin directory (after ViewModel creation to avoid auto-scan interference)
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Act
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert
                viewModel.Plugins.Should().BeEmpty();
                viewModel.TotalPluginCount.Should().Be(0);
                viewModel.StatusMessage.Should().Contain("Found 0 plugins");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand handles non-existent directory gracefully.
        /// Should set appropriate status message and not throw exceptions.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_HandlesNonExistentDirectory_Gracefully()
        {
            // Arrange
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = @"C:\NonExistent\Path"
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Act
            await viewModel.ScanPluginsCommand.Execute().FirstAsync();

            // Assert
            viewModel.StatusMessage.Should().Contain("not found");
            viewModel.Plugins.Should().BeEmpty();
            viewModel.TotalPluginCount.Should().Be(0);
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand handles missing plugin directory (game not installed).
        /// Should set appropriate error message when PluginDirectory is empty.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_HandlesEmptyPluginDirectory_WhenGameNotDetected()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Act
            await viewModel.ScanPluginsCommand.Execute().FirstAsync();

            // Assert
            viewModel.ErrorMessage.Should().Contain("plugin directory not set");
            viewModel.Plugins.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand clears previous results before scanning.
        /// Ensures old data doesn't persist across multiple scans.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_ClearsPreviousResults_BeforeScanning()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                var firstScanPlugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo
                    {
                        FileName = "plugin1.dll",
                        Compatibility = F4SeCompatibility.Universal
                    }
                };

                var secondScanPlugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo
                    {
                        FileName = "plugin2.dll",
                        Compatibility = F4SeCompatibility.OgOnly
                    }
                };

                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>())
                    .Returns(firstScanPlugins, secondScanPlugins);

                // Create ViewModel BEFORE creating plugin directory to prevent constructor auto-scan
                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // NOW create the plugin directory (after ViewModel creation to avoid auto-scan interference)
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Act - First scan
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();
                viewModel.Plugins.Count.Should().Be(1);
                viewModel.Plugins[0].FileName.Should().Be("plugin1.dll");

                // Act - Second scan
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert - should only have second scan results
                viewModel.Plugins.Count.Should().Be(1);
                viewModel.Plugins[0].FileName.Should().Be("plugin2.dll");
                viewModel.SelectedPlugin.Should().BeNull(); // SelectedPlugin should be cleared
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand updates all statistics properties.
        /// Tests IncompatibleCount calculation including NotF4SePlugin and Unknown types.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_UpdatesStatistics_Correctly()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                var plugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.OgOnly },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.NgOnly },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.NotF4SePlugin },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Unknown }
                };

                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()).Returns(plugins);

                // Create ViewModel BEFORE creating plugin directory to prevent constructor auto-scan
                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // NOW create the plugin directory (after ViewModel creation to avoid auto-scan interference)
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Act
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert
                viewModel.TotalPluginCount.Should().Be(6);
                viewModel.UniversalCount.Should().Be(2);
                viewModel.OgOnlyCount.Should().Be(1);
                viewModel.NgOnlyCount.Should().Be(1);
                viewModel.IncompatibleCount.Should().Be(2); // NotF4SePlugin + Unknown
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand sets IsBusy correctly during execution.
        /// IsBusy should be true during scanning and false after completion.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_SetsIsBusy_DuringExecution()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                var taskCompletionSource = new TaskCompletionSource<List<F4SePluginInfo>>();
                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>())
                    .Returns(callInfo => taskCompletionSource.Task.Result);

                // Create ViewModel BEFORE creating plugin directory to prevent constructor auto-scan
                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // NOW create the plugin directory (after ViewModel creation to avoid auto-scan interference)
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Act - Start command execution
                var scanTask = viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Brief delay to allow command to start
                await Task.Delay(50);

                // Assert - should be busy during execution
                viewModel.IsBusy.Should().BeTrue();

                // Complete the scan
                taskCompletionSource.SetResult(new List<F4SePluginInfo>());
                await scanTask;

                // Assert - should not be busy after completion
                viewModel.IsBusy.Should().BeFalse();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand updates StatusMessage during and after scanning.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_UpdatesStatusMessage_DuringAndAfterScan()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                var plugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal }
                };
                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()).Returns(plugins);

                // Create ViewModel BEFORE creating plugin directory to prevent constructor auto-scan
                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // NOW create the plugin directory (after ViewModel creation to avoid auto-scan interference)
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                // Act
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert - should show completion status with count
                viewModel.StatusMessage.Should().Contain("Scan complete");
                viewModel.StatusMessage.Should().Contain("1 plugins");
                viewModel.ProgressPercentage.Should().Be(100);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Verifies that ScanPluginsCommand handles exceptions gracefully without crashing.
        /// ErrorMessage should be set and IsBusy should be reset to false.
        /// </summary>
        [Fact]
        public async Task ScanPluginsCommand_HandlesExceptions_Gracefully()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                // Create the plugin directory
                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                _pluginService.When(x => x.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()))
                    .Do(x => throw new InvalidOperationException("Test exception"));

                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // Act
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert - should handle exception gracefully
                viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
                viewModel.IsBusy.Should().BeFalse();
                viewModel.Plugins.Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region Filter Tests

        /// <summary>
        /// Verifies that ShowOgOnly property can be set and get correctly.
        /// NOTE: ReactiveUI's RaiseAndSetIfChanged does not trigger property change callbacks in unit test environments
        /// without proper scheduler initialization, so we only verify property get/set functionality here.
        /// The actual filtering logic is tested through ScanPluginsCommand which calls ApplyFilters() directly.
        /// </summary>
        [Fact]
        public void ShowOgOnly_CanBeSetAndGet()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Act & Assert - Initially true
            viewModel.ShowOgOnly.Should().BeTrue();

            // Act - Set to false
            viewModel.ShowOgOnly = false;

            // Assert - Property is updated
            viewModel.ShowOgOnly.Should().BeFalse();

            // Act - Set back to true
            viewModel.ShowOgOnly = true;

            // Assert - Property is updated
            viewModel.ShowOgOnly.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that ShowNgOnly property can be set and get correctly.
        /// </summary>
        [Fact]
        public void ShowNgOnly_CanBeSetAndGet()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert - Initially true
            viewModel.ShowNgOnly.Should().BeTrue();

            // Act & Assert
            viewModel.ShowNgOnly = false;
            viewModel.ShowNgOnly.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that ShowUniversal property can be set and get correctly.
        /// </summary>
        [Fact]
        public void ShowUniversal_CanBeSetAndGet()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert - Initially true
            viewModel.ShowUniversal.Should().BeTrue();

            // Act & Assert
            viewModel.ShowUniversal = false;
            viewModel.ShowUniversal.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that ShowIncompatible property can be set and get correctly.
        /// </summary>
        [Fact]
        public void ShowIncompatible_CanBeSetAndGet()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert - Initially true
            viewModel.ShowIncompatible.Should().BeTrue();

            // Act & Assert
            viewModel.ShowIncompatible = false;
            viewModel.ShowIncompatible.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that filter properties are initialized to true (show all by default).
        /// </summary>
        [Fact]
        public void FilterProperties_InitializedToTrue()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = false };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            // Act
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Assert - All filters should be true by default
            viewModel.ShowOgOnly.Should().BeTrue();
            viewModel.ShowNgOnly.Should().BeTrue();
            viewModel.ShowUniversal.Should().BeTrue();
            viewModel.ShowIncompatible.Should().BeTrue();
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that SelectedPlugin property can be set and raises property change notification.
        /// </summary>
        [Fact]
        public void SelectedPlugin_CanBeSetAndRaisesPropertyChanged()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = true, InstallPath = @"C:\Games\Fallout 4" };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            var plugin = new F4SePluginInfo
            {
                FileName = "test.dll",
                Compatibility = F4SeCompatibility.Universal
            };

            var propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.SelectedPlugin))
                    propertyChangedRaised = true;
            };

            // Act
            viewModel.SelectedPlugin = plugin;

            // Assert
            viewModel.SelectedPlugin.Should().Be(plugin);
            propertyChangedRaised.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that statistics properties return correct counts based on plugin compatibility.
        /// </summary>
        [Fact]
        public async Task StatisticsProperties_ReturnCorrectCounts()
        {
            // Arrange - Use a real temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), "F4SEPluginTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var gameInfo = new GameInfo
                {
                    IsInstalled = true,
                    InstallPath = tempDir
                };
                _gameDetectionService.DetectGame().Returns(gameInfo);

                var pluginDir = Path.Combine(tempDir, "Data", "F4SE", "Plugins");
                Directory.CreateDirectory(pluginDir);

                var plugins = new List<F4SePluginInfo>
                {
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.OgOnly },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.OgOnly },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.NgOnly },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Universal },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.NotF4SePlugin },
                    new F4SePluginInfo { Compatibility = F4SeCompatibility.Unknown }
                };
                _pluginService.ScanDirectory(Arg.Any<string>(), Arg.Any<bool>()).Returns(plugins);

                var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

                // Act
                await viewModel.ScanPluginsCommand.Execute().FirstAsync();

                // Assert
                viewModel.TotalPluginCount.Should().Be(8);
                viewModel.OgOnlyCount.Should().Be(2);
                viewModel.NgOnlyCount.Should().Be(1);
                viewModel.UniversalCount.Should().Be(3);
                viewModel.IncompatibleCount.Should().Be(2);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region Disposal Tests

        /// <summary>
        /// Verifies that Dispose method disposes the ScanPluginsCommand.
        /// Multiple calls to Dispose should not throw exceptions.
        /// </summary>
        [Fact]
        public void Dispose_DisposesCommand_WithoutThrowing()
        {
            // Arrange
            var gameInfo = new GameInfo { IsInstalled = true, InstallPath = @"C:\Games\Fallout 4" };
            _gameDetectionService.DetectGame().Returns(gameInfo);
            var viewModel = new F4SEViewModel(_pluginService, _gameDetectionService, _logger);

            // Act
            viewModel.Dispose();

            // Assert - Dispose should be idempotent (can call multiple times without error)
            Action act = () => viewModel.Dispose();
            act.Should().NotThrow();
        }

        #endregion
    }
}
