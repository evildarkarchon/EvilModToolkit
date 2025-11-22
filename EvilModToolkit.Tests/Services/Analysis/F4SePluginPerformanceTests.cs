using System.Diagnostics;
using System.IO;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Platform;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using EvilModToolkit.Models;

namespace EvilModToolkit.Tests.Services.Analysis;

public class F4SePluginPerformanceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _testDataPath;

    public F4SePluginPerformanceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "test_data", "F4SE_Plugins");
    }

    [Fact]
    public void ScanDirectory_PerformanceTest()
    {
        // Arrange
        var logger = Substitute.For<ILogger<F4SePluginService>>();
        var fileVersionService = Substitute.For<IFileVersionService>();
        var service = new F4SePluginService(logger, fileVersionService);

        // Ensure test data directory exists
        Assert.True(Directory.Exists(_testDataPath), $"Test data directory not found: {_testDataPath}");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var plugins = service.ScanDirectory(_testDataPath);
        stopwatch.Stop();

        // Assert
        _testOutputHelper.WriteLine($"ScanDirectory took {stopwatch.ElapsedMilliseconds} ms to scan {plugins.Count} plugins.");
        
        // Example: Assert that it completes within a reasonable time (e.g., 5 seconds)
        // This threshold might need adjustment based on typical system performance and expected number of plugins.
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, "ScanDirectory performance degraded: took longer than 5 seconds.");
        Assert.NotEmpty(plugins);
    }

    [Fact]
    public async Task ModScannerService_ScanAsync_PerformanceTest()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ModScannerService>>();
        var service = new ModScannerService(logger);

        var gameInfo = new GameInfo
        {
            DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "test_data")
        };
        var modManagerInfo = new ModManagerInfo(); // Mocked, as it's not directly used for file system interaction in this service

        // Ensure test data directory exists
        Assert.True(Directory.Exists(gameInfo.DataPath), $"Test data directory not found: {gameInfo.DataPath}");

        var scanOptions = new ScanOptions
        {
            ScanJunkFiles = true,
            ScanWrongFormat = true,
            ScanLoosePrevis = true,
            ScanProblemOverrides = true,
            ScanErrors = true, // To enable CheckComplexSorterIni
            SkipDataScan = false
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = await service.ScanAsync(gameInfo, modManagerInfo, scanOptions);
        stopwatch.Stop();

        // Assert
        _testOutputHelper.WriteLine($"ModScannerService.ScanAsync took {stopwatch.ElapsedMilliseconds} ms to find {results.Count} issues.");

        // Adjust threshold based on test data size and expected performance
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, "ModScannerService.ScanAsync performance degraded: took longer than 5 seconds.");
    }
}
