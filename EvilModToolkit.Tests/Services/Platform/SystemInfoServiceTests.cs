using System.Runtime.Versioning;
using EvilModToolkit.Services.Platform;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvilModToolkit.Tests.Services.Platform;

[SupportedOSPlatform("windows")]
public class SystemInfoServiceTests
{
    private readonly ILogger<SystemInfoService> _logger;
    private readonly SystemInfoService _sut;

    public SystemInfoServiceTests()
    {
        _logger = Substitute.For<ILogger<SystemInfoService>>();
        _sut = new SystemInfoService(_logger);
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_ReturnsSystemInfo()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesOperatingSystem()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.OperatingSystem.Should().NotBeNullOrEmpty();
        result.OperatingSystem.Should().Contain("Windows");
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesBuildNumber()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.BuildNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesArchitecture()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Architecture.Should().NotBeNullOrEmpty();
        result.Architecture.Should().Match(arch =>
            arch.Contains("64") || arch.Contains("32") || arch == "Unknown");
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesRamInfo()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalRamGb.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesCpuInfo()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.CpuName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalled_PopulatesGpuInfo()
    {
        // Act
        var result = await _sut.GetSystemInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result!.GpuName.Should().NotBeNullOrEmpty();
        // GPU memory might be 0 for some virtual/integrated GPUs
        result.GpuMemoryMb.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetSystemInfoAsync_WhenCalledMultipleTimes_ReturnsConcurrentResults()
    {
        // Act
        var task1 = _sut.GetSystemInfoAsync();
        var task2 = _sut.GetSystemInfoAsync();
        var task3 = _sut.GetSystemInfoAsync();

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result!.OperatingSystem.Should().NotBeNullOrEmpty();
        });
    }
}
