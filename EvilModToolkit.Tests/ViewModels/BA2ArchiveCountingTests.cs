using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EvilModToolkit.Tests.ViewModels
{
    /// <summary>
    /// Comprehensive tests for BA2 archive counting functionality in OverviewViewModel.
    /// Tests cover BA2 type detection (GNRL vs DX10), version detection (v1 vs v7/v8),
    /// error handling, and count aggregation logic.
    /// </summary>
    public class BA2ArchiveCountingTests : IDisposable
    {
        private readonly IGameDetectionService _gameDetectionService;
        private readonly IModManagerService _modManagerService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly IBA2ArchiveService _ba2ArchiveService;
        private readonly ILogger<OverviewViewModel> _logger;
        private readonly string _testDataPath;

        public BA2ArchiveCountingTests()
        {
            _gameDetectionService = Substitute.For<IGameDetectionService>();
            _modManagerService = Substitute.For<IModManagerService>();
            _systemInfoService = Substitute.For<ISystemInfoService>();
            _ba2ArchiveService = Substitute.For<IBA2ArchiveService>();
            _logger = Substitute.For<ILogger<OverviewViewModel>>();

            // Create a unique test directory for each test run
            _testDataPath = Path.Combine(Path.GetTempPath(), $"BA2CountingTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDataPath);

            // Setup default mocks to avoid null reference issues in other tests
            _modManagerService.DetectModManagerAsync()
                .Returns(Task.FromResult(new ModManagerInfo { Type = ModManagerType.None }));
            _systemInfoService.GetSystemInfoAsync()
                .Returns(Task.FromResult<SystemInfo?>(new SystemInfo()));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, recursive: true);
            }
        }

        #region No BA2 Files Tests

        /// <summary>
        /// Tests that BA2 counts are initialized to zero when no BA2 files exist.
        /// This validates the default state and ensures no false positives.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithNoFiles_SetsCountsToZero()
        {
            // Arrange - GameInfo points to test directory with no BA2 files
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act - Wait for refresh to complete
            await Task.Delay(200);

            // Assert - All counts should be zero
            viewModel.BA2CountGeneral.Should().Be(0, "no GNRL archives exist");
            viewModel.BA2CountTexture.Should().Be(0, "no DX10 archives exist");
            viewModel.BA2CountTotal.Should().Be(0, "total should be sum of General and Texture");
            viewModel.BA2CountV1.Should().Be(0, "no v1 archives exist");
            viewModel.BA2CountV7V8.Should().Be(0, "no v7/v8 archives exist");
            viewModel.BA2CountUnreadable.Should().Be(0, "no unreadable archives exist");
        }

        /// <summary>
        /// Tests that BA2 scanning handles non-existent Data directory gracefully.
        /// This validates error handling when game detection returns invalid paths.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithNonExistentDataDirectory_SetsCountsToZero()
        {
            // Arrange - GameInfo points to non-existent directory
            var nonExistentPath = Path.Combine(_testDataPath, "NonExistent");
            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = nonExistentPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act - Wait for refresh to complete
            await Task.Delay(200);

            // Assert - All counts should be zero, no exceptions thrown
            viewModel.BA2CountGeneral.Should().Be(0);
            viewModel.BA2CountTexture.Should().Be(0);
            viewModel.BA2CountTotal.Should().Be(0);
            viewModel.BA2CountV1.Should().Be(0);
            viewModel.BA2CountV7V8.Should().Be(0);
            viewModel.BA2CountUnreadable.Should().Be(0);
        }

        /// <summary>
        /// Tests that BA2 scanning handles null GameInfo gracefully.
        /// This validates defensive programming for edge cases.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithNullGameInfo_SetsCountsToZero()
        {
            // Arrange - DetectGame returns null
            _gameDetectionService.DetectGame().Returns((GameInfo?)null);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act - Wait for refresh to complete
            await Task.Delay(200);

            // Assert - All counts should be zero, no exceptions thrown
            viewModel.BA2CountGeneral.Should().Be(0);
            viewModel.BA2CountTexture.Should().Be(0);
            viewModel.BA2CountTotal.Should().Be(0);
        }

        #endregion

        #region Type Counting Tests (GNRL vs DX10)

        /// <summary>
        /// Tests counting of General (GNRL) BA2 archives.
        /// Validates that only GNRL archives are counted in BA2CountGeneral.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithOnlyGeneralArchives_CountsCorrectly()
        {
            // Arrange - Create mock BA2 files
            var ba2Files = CreateMockBA2Files(3, BA2Type.General, BA2Version.V1);
            SetupBA2ArchiveServiceMocks(ba2Files);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act - Wait for refresh
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(3, "three GNRL archives exist");
            viewModel.BA2CountTexture.Should().Be(0, "no DX10 archives exist");
            viewModel.BA2CountTotal.Should().Be(3, "total is sum of GNRL and DX10");
        }

        /// <summary>
        /// Tests counting of Texture (DX10) BA2 archives.
        /// Validates that only DX10 archives are counted in BA2CountTexture.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithOnlyTextureArchives_CountsCorrectly()
        {
            // Arrange - Create mock BA2 files
            var ba2Files = CreateMockBA2Files(5, BA2Type.Texture, BA2Version.V1);
            SetupBA2ArchiveServiceMocks(ba2Files);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(0, "no GNRL archives exist");
            viewModel.BA2CountTexture.Should().Be(5, "five DX10 archives exist");
            viewModel.BA2CountTotal.Should().Be(5, "total is sum of GNRL and DX10");
        }

        /// <summary>
        /// Tests counting with mixed GNRL and DX10 archives.
        /// Validates that archives are correctly categorized by type.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithMixedTypes_CountsEachTypeCorrectly()
        {
            // Arrange - Create 10 GNRL and 7 DX10 archives
            var gnrlFiles = CreateMockBA2Files(10, BA2Type.General, BA2Version.V1);
            var dx10Files = CreateMockBA2Files(7, BA2Type.Texture, BA2Version.V1);
            var allFiles = gnrlFiles.Concat(dx10Files).ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(10, "ten GNRL archives exist");
            viewModel.BA2CountTexture.Should().Be(7, "seven DX10 archives exist");
            viewModel.BA2CountTotal.Should().Be(17, "total is 10 + 7 = 17");
        }

        #endregion

        #region Version Counting Tests (v1 vs v7/v8)

        /// <summary>
        /// Tests counting of v1 (Original Game) BA2 archives.
        /// Validates version detection for legacy archives.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithOnlyV1Archives_CountsCorrectly()
        {
            // Arrange - Create v1 archives
            var ba2Files = CreateMockBA2Files(4, BA2Type.General, BA2Version.V1);
            SetupBA2ArchiveServiceMocks(ba2Files);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountV1.Should().Be(4, "four v1 archives exist");
            viewModel.BA2CountV7V8.Should().Be(0, "no v7/v8 archives exist");
        }

        /// <summary>
        /// Tests counting of v7 and v8 (Next Gen) BA2 archives.
        /// Validates that both v7 and v8 are counted together in BA2CountV7V8.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithMixedV7AndV8Archives_CountsCorrectly()
        {
            // Arrange - Create mix of v7 and v8 archives
            var v7Files = CreateMockBA2Files(3, BA2Type.General, BA2Version.V7);
            var v8Files = CreateMockBA2Files(2, BA2Type.General, BA2Version.V8);
            var allFiles = v7Files.Concat(v8Files).ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountV1.Should().Be(0, "no v1 archives exist");
            viewModel.BA2CountV7V8.Should().Be(5, "three v7 and two v8 archives exist");
        }

        /// <summary>
        /// Tests counting with mixed v1 and v7/v8 archives.
        /// Validates proper segregation of OG vs NG archive versions.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithMixedVersions_CountsEachVersionCorrectly()
        {
            // Arrange - Create mix of all versions
            var v1Files = CreateMockBA2Files(6, BA2Type.General, BA2Version.V1);
            var v7Files = CreateMockBA2Files(3, BA2Type.General, BA2Version.V7);
            var v8Files = CreateMockBA2Files(4, BA2Type.General, BA2Version.V8);
            var allFiles = v1Files.Concat(v7Files).Concat(v8Files).ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountV1.Should().Be(6, "six v1 archives exist");
            viewModel.BA2CountV7V8.Should().Be(7, "three v7 and four v8 archives exist (3 + 4 = 7)");
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests counting of unreadable/invalid BA2 archives.
        /// Validates error handling for corrupted or invalid files.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithUnreadableArchives_CountsUnreadable()
        {
            // Arrange - Create some valid and some invalid archives
            var validFile = Path.Combine(_testDataPath, "Valid.ba2");
            var invalidFile = Path.Combine(_testDataPath, "Invalid.ba2");
            File.WriteAllText(validFile, "dummy");
            File.WriteAllText(invalidFile, "dummy");

            // Mock: valid archive returns proper info, invalid returns null
            _ba2ArchiveService.GetArchiveInfo(validFile).Returns(new BA2ArchiveInfo
            {
                FilePath = validFile,
                IsValid = true,
                Type = BA2Type.General,
                Version = BA2Version.V1
            });
            _ba2ArchiveService.GetArchiveInfo(invalidFile).Returns((BA2ArchiveInfo?)null);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(1, "one valid GNRL archive");
            viewModel.BA2CountUnreadable.Should().Be(1, "one unreadable archive");
        }

        /// <summary>
        /// Tests counting when BA2 archive has invalid IsValid flag.
        /// Validates handling of archives that fail validation.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithInvalidArchiveInfo_CountsAsUnreadable()
        {
            // Arrange - Archive returns info but IsValid = false
            var ba2File = Path.Combine(_testDataPath, "Invalid.ba2");
            File.WriteAllText(ba2File, "dummy");

            _ba2ArchiveService.GetArchiveInfo(ba2File).Returns(new BA2ArchiveInfo
            {
                FilePath = ba2File,
                IsValid = false,  // Invalid archive
                Type = BA2Type.Unknown,
                Version = BA2Version.Unknown
            });

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountUnreadable.Should().Be(1, "archive with IsValid=false should count as unreadable");
        }

        /// <summary>
        /// Tests counting when BA2 archive has unknown type.
        /// Validates handling of archives with unrecognized type magic.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithUnknownType_CountsAsUnreadable()
        {
            // Arrange - Archive with unknown type
            var ba2File = Path.Combine(_testDataPath, "UnknownType.ba2");
            File.WriteAllText(ba2File, "dummy");

            _ba2ArchiveService.GetArchiveInfo(ba2File).Returns(new BA2ArchiveInfo
            {
                FilePath = ba2File,
                IsValid = true,
                Type = BA2Type.Unknown,  // Unknown type
                Version = BA2Version.V1
            });

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountUnreadable.Should().Be(1, "archive with Unknown type should count as unreadable");
        }

        /// <summary>
        /// Tests that GetArchiveInfo exceptions are handled gracefully.
        /// Validates robust error handling during scanning.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WhenGetArchiveInfoThrows_CountsAsUnreadable()
        {
            // Arrange - GetArchiveInfo throws exception
            var ba2File = Path.Combine(_testDataPath, "Throws.ba2");
            File.WriteAllText(ba2File, "dummy");

            _ba2ArchiveService.GetArchiveInfo(ba2File)
                .Returns(x => throw new IOException("Test exception"));

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert - Should handle exception and count as unreadable
            viewModel.BA2CountUnreadable.Should().Be(1, "exception during GetArchiveInfo should count as unreadable");
        }

        #endregion

        #region Total Count Tests

        /// <summary>
        /// Tests that BA2CountTotal is calculated correctly as sum of General and Texture.
        /// This is a critical calculation for detecting archive limit issues.
        /// </summary>
        [Fact]
        public async Task BA2CountTotal_CalculatesCorrectly()
        {
            // Arrange - Create mix of types
            var gnrlFiles = CreateMockBA2Files(150, BA2Type.General, BA2Version.V1);
            var dx10Files = CreateMockBA2Files(200, BA2Type.Texture, BA2Version.V1);
            var allFiles = gnrlFiles.Concat(dx10Files).ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(150);
            viewModel.BA2CountTexture.Should().Be(200);
            viewModel.BA2CountTotal.Should().Be(350, "total should be 150 + 200 = 350");
        }

        /// <summary>
        /// Tests that BA2CountTotal updates when individual counts change.
        /// Validates reactive property notification for the computed property.
        /// </summary>
        [Fact]
        public async Task BA2CountTotal_UpdatesWhenIndividualCountsChange()
        {
            // Arrange - Start with some archives
            var initialFiles = CreateMockBA2Files(10, BA2Type.General, BA2Version.V1);
            SetupBA2ArchiveServiceMocks(initialFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            await Task.Delay(200);
            var initialTotal = viewModel.BA2CountTotal;

            // Act - Refresh with more archives
            var newFiles = CreateMockBA2Files(20, BA2Type.Texture, BA2Version.V1);
            var allFiles = initialFiles.Concat(newFiles).ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            await viewModel.RefreshCommand.Execute().FirstAsync();

            // Assert - Total should update
            viewModel.BA2CountTotal.Should().Be(30, "total should update to 10 + 20 = 30");
            viewModel.BA2CountTotal.Should().NotBe(initialTotal, "total should have changed");
        }

        #endregion

        #region Realistic Scenario Tests

        /// <summary>
        /// Tests a realistic scenario with many archives of different types and versions.
        /// Simulates a heavily modded Fallout 4 installation.
        /// </summary>
        [Fact]
        public async Task ScanBA2Archives_WithRealisticMixedArchives_CountsCorrectly()
        {
            // Arrange - Realistic mix: some vanilla (v1), some NG updates (v7/v8), mix of types
            var v1Gnrl = CreateMockBA2Files(50, BA2Type.General, BA2Version.V1);
            var v1Dx10 = CreateMockBA2Files(30, BA2Type.Texture, BA2Version.V1);
            var v7Gnrl = CreateMockBA2Files(20, BA2Type.General, BA2Version.V7);
            var v7Dx10 = CreateMockBA2Files(15, BA2Type.Texture, BA2Version.V7);
            var v8Gnrl = CreateMockBA2Files(10, BA2Type.General, BA2Version.V8);
            var v8Dx10 = CreateMockBA2Files(5, BA2Type.Texture, BA2Version.V8);

            var allFiles = v1Gnrl.Concat(v1Dx10)
                .Concat(v7Gnrl).Concat(v7Dx10)
                .Concat(v8Gnrl).Concat(v8Dx10)
                .ToList();
            SetupBA2ArchiveServiceMocks(allFiles);

            var gameInfo = new GameInfo
            {
                IsInstalled = true,
                InstallPath = _testDataPath,
                DataPath = _testDataPath
            };
            _gameDetectionService.DetectGame().Returns(gameInfo);

            var viewModel = new OverviewViewModel(
                _gameDetectionService,
                _modManagerService,
                _systemInfoService,
                _ba2ArchiveService,
                _logger);

            // Act
            await Task.Delay(200);

            // Assert
            viewModel.BA2CountGeneral.Should().Be(80, "50 + 20 + 10 = 80 GNRL archives");
            viewModel.BA2CountTexture.Should().Be(50, "30 + 15 + 5 = 50 DX10 archives");
            viewModel.BA2CountTotal.Should().Be(130, "80 + 50 = 130 total archives");
            viewModel.BA2CountV1.Should().Be(80, "50 + 30 = 80 v1 archives");
            viewModel.BA2CountV7V8.Should().Be(50, "20 + 15 + 10 + 5 = 50 v7/v8 archives");
            viewModel.BA2CountUnreadable.Should().Be(0, "all archives are valid");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a list of mock BA2 file paths with specified type and version.
        /// Files are created in the test directory but mocked in the BA2ArchiveService.
        /// </summary>
        /// <param name="count">Number of files to create.</param>
        /// <param name="type">BA2 archive type (GNRL or DX10).</param>
        /// <param name="version">BA2 archive version (V1, V7, or V8).</param>
        /// <returns>List of file paths.</returns>
        private System.Collections.Generic.List<string> CreateMockBA2Files(int count, BA2Type type, BA2Version version)
        {
            var files = new System.Collections.Generic.List<string>();
            for (int i = 0; i < count; i++)
            {
                var fileName = $"{type}_{version}_{i}_{Guid.NewGuid()}.ba2";
                var filePath = Path.Combine(_testDataPath, fileName);
                // Create actual file so Directory.GetFiles finds it
                File.WriteAllText(filePath, "mock ba2 content");
                files.Add(filePath);
            }
            return files;
        }

        /// <summary>
        /// Sets up BA2ArchiveService mocks for the given file paths.
        /// Each file is mocked to return appropriate BA2ArchiveInfo based on its filename.
        /// </summary>
        /// <param name="filePaths">List of BA2 file paths to mock.</param>
        private void SetupBA2ArchiveServiceMocks(System.Collections.Generic.List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var parts = fileName.Split('_');

                // Parse type and version from filename (format: Type_Version_Index_Guid.ba2)
                var type = Enum.Parse<BA2Type>(parts[0]);
                var version = Enum.Parse<BA2Version>(parts[1]);

                _ba2ArchiveService.GetArchiveInfo(filePath).Returns(new BA2ArchiveInfo
                {
                    FilePath = filePath,
                    FileName = fileName,
                    IsValid = true,
                    Type = type,
                    Version = version,
                    FileSizeBytes = 1024
                });
            }
        }

        #endregion
    }
}
