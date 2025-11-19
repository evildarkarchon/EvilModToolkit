using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Game;

[SupportedOSPlatform("windows")]
public class ModManagerServiceTests : IDisposable
{
    private readonly ILogger<ModManagerService> _logger;
    private readonly IProcessService _processService;
    private readonly IFileVersionService _fileVersionService;
    private readonly ModManagerService _sut;
    private readonly string _testDirectory;

    public ModManagerServiceTests()
    {
        _logger = Substitute.For<ILogger<ModManagerService>>();
        _processService = Substitute.For<IProcessService>();
        _fileVersionService = Substitute.For<IFileVersionService>();
        _sut = new ModManagerService(_logger, _processService, _fileVersionService);
        _testDirectory = Path.Combine(Path.GetTempPath(), "ModManagerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task DetectModManagerAsync_WhenNotLaunchedFromModManager_ReturnsNone()
    {
        // Arrange
        _processService.FindModManager().Returns(new ModManagerInfo
        {
            Type = ModManagerType.None
        });

        // Act
        var result = await _sut.DetectModManagerAsync();

        // Assert
        result.Type.Should().Be(ModManagerType.None);
        result.ExecutablePath.Should().BeEmpty();
        result.Version.Should().Be("N/A");
    }

    [Fact]
    public async Task DetectModManagerAsync_WhenLaunchedFromMO2_DetectsMO2()
    {
        // Arrange
        var mo2ExePath = Path.Combine(_testDirectory, "ModOrganizer.exe");
        File.WriteAllText(mo2ExePath, "dummy exe");

        _processService.FindModManager().Returns(new ModManagerInfo
        {
            Type = ModManagerType.ModOrganizer2,
            ExecutablePath = mo2ExePath,
            WorkingDirectory = _testDirectory,
            ProcessId = 1234
        });

        _fileVersionService.GetFileVersion(mo2ExePath).Returns(new VersionInfo
        {
            FileVersion = "2.5.0"
        });

        // Act
        var result = await _sut.DetectModManagerAsync();

        // Assert
        result.Type.Should().Be(ModManagerType.ModOrganizer2);
        result.ExecutablePath.Should().Be(mo2ExePath);
        result.Version.Should().Be("2.5.0");
    }

    [Fact]
    public async Task DetectModManagerAsync_WhenLaunchedFromVortex_DetectsVortex()
    {
        // Arrange
        var vortexExePath = Path.Combine(_testDirectory, "Vortex.exe");
        File.WriteAllText(vortexExePath, "dummy exe");

        _processService.FindModManager().Returns(new ModManagerInfo
        {
            Type = ModManagerType.Vortex,
            ExecutablePath = vortexExePath,
            WorkingDirectory = _testDirectory,
            ProcessId = 5678
        });

        _fileVersionService.GetFileVersion(vortexExePath).Returns(new VersionInfo
        {
            FileVersion = "1.8.5"
        });

        // Act
        var result = await _sut.DetectModManagerAsync();

        // Assert
        result.Type.Should().Be(ModManagerType.Vortex);
        result.ExecutablePath.Should().Be(vortexExePath);
        result.Version.Should().Be("1.8.5");
    }

    [Fact]
    public void ParseMO2Config_WithValidIni_ParsesCorrectly()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"[General]
gameName=Fallout 4
gamePath=C:\Games\Fallout4
selected_profile=Default

[Settings]
base_directory=C:\MO2
cache_directory=%BASE_DIR%\webcache
download_directory=%BASE_DIR%\downloads
mod_directory=%BASE_DIR%\mods
overwrite_directory=%BASE_DIR%\overwrite
profiles_directory=%BASE_DIR%\profiles
profile_local_inis=true
profile_local_saves=false
skip_file_suffixes=.mohidden, .backup
skip_directories=__pycache__, .git
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.GameName.Should().Be("Fallout 4");
        result.GamePath.Should().Be(@"C:\Games\Fallout4");
        result.SelectedProfile.Should().Be("Default");
        result.BaseDirectory.Should().Be(@"C:\MO2");
        result.CacheDirectory.Should().Be(@"C:\MO2\webcache");
        result.DownloadDirectory.Should().Be(@"C:\MO2\downloads");
        result.ModDirectory.Should().Be(@"C:\MO2\mods");
        result.OverwriteDirectory.Should().Be(@"C:\MO2\overwrite");
        result.ProfilesDirectory.Should().Be(@"C:\MO2\profiles");
        result.ProfileLocalInis.Should().BeTrue();
        result.ProfileLocalSaves.Should().BeFalse();
        result.SkipFileSuffixes.Should().Contain(".mohidden");
        result.SkipFileSuffixes.Should().Contain(".backup");
        result.SkipDirectories.Should().Contain("__pycache__");
        result.SkipDirectories.Should().Contain(".git");
    }

    [Fact]
    public void ParseMO2Config_WithByteArrayWrapper_RemovesWrapper()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"[General]
gameName=Fallout 4
selected_profile=@ByteArray(Default)

[Settings]
base_directory=@ByteArray(C:\MO2)
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.SelectedProfile.Should().Be("Default");
        result.BaseDirectory.Should().Be(@"C:\MO2");
    }

    [Fact]
    public void ParseMO2Config_WithCustomExecutables_ParsesTools()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"[General]
gameName=Fallout 4

[customExecutables]
1\binary=C:\Tools\FO4Edit.exe
2\binary=C:\Tools\BSArch.exe
3\binary=C:\Tools\LOOT.exe
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.CustomExecutables.Should().ContainKey("xEdit");
        result.CustomExecutables.Should().ContainKey("BSArch");
        result.CustomExecutables.Should().ContainKey("LOOT");
    }

    [Fact]
    public void ParseMO2Config_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "NonExistent.ini");

        // Act
        Action act = () => _sut.ParseMO2Config(nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ParseMO2Config_WithCommentsAndEmptyLines_SkipsThem()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"; This is a comment
[General]
gameName=Fallout 4
; Another comment

# Hash comment
selected_profile=Default

[Settings]
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.GameName.Should().Be("Fallout 4");
        result.SelectedProfile.Should().Be("Default");
    }

    [Fact]
    public void ParseMO2Config_WithRelativePaths_ExpandsFromBaseDir()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"[General]
gameName=Fallout 4

[Settings]
base_directory=C:\MO2
mod_directory=mods
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.ModDirectory.Should().Be(@"C:\MO2\mods");
    }

    [Fact]
    public void ParseMO2Config_WithAbsolutePaths_KeepsThemAbsolute()
    {
        // Arrange
        var iniPath = Path.Combine(_testDirectory, "ModOrganizer.ini");
        var iniContent = @"[General]
gameName=Fallout 4

[Settings]
base_directory=C:\MO2
mod_directory=D:\Mods\Fallout4
";
        File.WriteAllText(iniPath, iniContent);

        // Act
        var result = _sut.ParseMO2Config(iniPath);

        // Assert
        result.ModDirectory.Should().Be(@"D:\Mods\Fallout4");
    }

    [Fact]
    public void FindMO2Installation_WhenNotInstalled_ReturnsNull()
    {
        // Arrange - use a testable service that returns null for registry checks
        var testService = new TestableModManagerService(_logger, _processService, _fileVersionService, null, null);

        // Act
        var result = testService.FindMO2Installation();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindVortexInstallation_WhenNotInstalled_ReturnsNull()
    {
        // Arrange - use a testable service that returns null for vortex checks
        var testService = new TestableModManagerService(_logger, _processService, _fileVersionService, null, null);

        // Act
        var result = testService.FindVortexInstallation();

        // Assert
        result.Should().BeNull();
    }

    // Helper class to allow testing with controlled registry/file system access
    private class TestableModManagerService : ModManagerService
    {
        private readonly string? _mo2Path;
        private readonly string? _vortexPath;

        public TestableModManagerService(
            ILogger<ModManagerService> logger,
            IProcessService processService,
            IFileVersionService fileVersionService,
            string? mo2Path,
            string? vortexPath)
            : base(logger, processService, fileVersionService)
        {
            _mo2Path = mo2Path;
            _vortexPath = vortexPath;
        }

        public override string? FindMO2Installation() => _mo2Path;
        public override string? FindVortexInstallation() => _vortexPath;
    }
}