using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Configuration;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Tests.Integration;

/// <summary>
/// Integration tests for the Dependency Injection container configuration.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DependencyInjectionTests()
    {
        // Create the same service configuration as App.axaml.cs
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
        });

        // Register Platform Services (Singleton)
        services.AddSingleton<IFileVersionService, FileVersionService>();
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        services.AddSingleton<IProcessService, ProcessService>();

        // Register Game Services (Scoped)
        services.AddScoped<IGameDetectionService, GameDetectionService>();
        services.AddScoped<IModManagerService, ModManagerService>();

        // Register Analysis Services (Transient)
        services.AddTransient<IF4SEPluginService, F4SePluginService>();

        // Register Patching Services (Transient)
        services.AddTransient<IBA2ArchiveService, BA2ArchiveService>();
        services.AddTransient<IXDeltaPatcherService, XDeltaPatcherService>();

        // Register Configuration Services (Singleton)
        services.AddSingleton<ISettingsService, SettingsService>();

        // Register ViewModels (Transient)
        services.AddTransient<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void AllServices_CanBeResolved_WithoutErrors()
    {
        // Act & Assert - All services should resolve without throwing
        var fileVersionService = _serviceProvider.GetRequiredService<IFileVersionService>();
        var systemInfoService = _serviceProvider.GetRequiredService<ISystemInfoService>();
        var processService = _serviceProvider.GetRequiredService<IProcessService>();
        var gameDetectionService = _serviceProvider.GetRequiredService<IGameDetectionService>();
        var modManagerService = _serviceProvider.GetRequiredService<IModManagerService>();
        var f4sePluginService = _serviceProvider.GetRequiredService<IF4SEPluginService>();
        var ba2ArchiveService = _serviceProvider.GetRequiredService<IBA2ArchiveService>();
        var xdeltaPatcherService = _serviceProvider.GetRequiredService<IXDeltaPatcherService>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

        // All services should be non-null
        fileVersionService.Should().NotBeNull();
        systemInfoService.Should().NotBeNull();
        processService.Should().NotBeNull();
        gameDetectionService.Should().NotBeNull();
        modManagerService.Should().NotBeNull();
        f4sePluginService.Should().NotBeNull();
        ba2ArchiveService.Should().NotBeNull();
        xdeltaPatcherService.Should().NotBeNull();
        settingsService.Should().NotBeNull();
    }

    [Fact]
    public void SingletonServices_ReturnSameInstance()
    {
        // Arrange & Act
        var fileVersionService1 = _serviceProvider.GetRequiredService<IFileVersionService>();
        var fileVersionService2 = _serviceProvider.GetRequiredService<IFileVersionService>();

        var systemInfoService1 = _serviceProvider.GetRequiredService<ISystemInfoService>();
        var systemInfoService2 = _serviceProvider.GetRequiredService<ISystemInfoService>();

        var processService1 = _serviceProvider.GetRequiredService<IProcessService>();
        var processService2 = _serviceProvider.GetRequiredService<IProcessService>();

        var settingsService1 = _serviceProvider.GetRequiredService<ISettingsService>();
        var settingsService2 = _serviceProvider.GetRequiredService<ISettingsService>();

        // Assert - Singleton services should return the same instance
        fileVersionService1.Should().BeSameAs(fileVersionService2);
        systemInfoService1.Should().BeSameAs(systemInfoService2);
        processService1.Should().BeSameAs(processService2);
        settingsService1.Should().BeSameAs(settingsService2);
    }

    [Fact]
    public void TransientServices_ReturnDifferentInstances()
    {
        // Arrange & Act
        var f4sePluginService1 = _serviceProvider.GetRequiredService<IF4SEPluginService>();
        var f4sePluginService2 = _serviceProvider.GetRequiredService<IF4SEPluginService>();

        var ba2ArchiveService1 = _serviceProvider.GetRequiredService<IBA2ArchiveService>();
        var ba2ArchiveService2 = _serviceProvider.GetRequiredService<IBA2ArchiveService>();

        var xdeltaPatcherService1 = _serviceProvider.GetRequiredService<IXDeltaPatcherService>();
        var xdeltaPatcherService2 = _serviceProvider.GetRequiredService<IXDeltaPatcherService>();

        // Assert - Transient services should return different instances
        f4sePluginService1.Should().NotBeSameAs(f4sePluginService2);
        ba2ArchiveService1.Should().NotBeSameAs(ba2ArchiveService2);
        xdeltaPatcherService1.Should().NotBeSameAs(xdeltaPatcherService2);
    }

    [Fact]
    public void ScopedServices_ReturnSameInstanceWithinScope()
    {
        // Arrange & Act
        using var scope1 = _serviceProvider.CreateScope();
        var gameDetectionService1a = scope1.ServiceProvider.GetRequiredService<IGameDetectionService>();
        var gameDetectionService1b = scope1.ServiceProvider.GetRequiredService<IGameDetectionService>();

        using var scope2 = _serviceProvider.CreateScope();
        var gameDetectionService2 = scope2.ServiceProvider.GetRequiredService<IGameDetectionService>();

        // Assert - Same instance within scope, different across scopes
        gameDetectionService1a.Should().BeSameAs(gameDetectionService1b);
        gameDetectionService1a.Should().NotBeSameAs(gameDetectionService2);
    }

    [Fact]
    public void MainWindowViewModel_CanBeResolved()
    {
        // Act
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void MainWindowViewModel_IsTransient()
    {
        // Arrange & Act
        var viewModel1 = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var viewModel2 = _serviceProvider.GetRequiredService<MainWindowViewModel>();

        // Assert - ViewModels should be transient (different instances)
        viewModel1.Should().NotBeSameAs(viewModel2);
    }

    [Fact]
    public void Logger_CanBeResolved()
    {
        // Act
        var logger = _serviceProvider.GetRequiredService<ILogger<DependencyInjectionTests>>();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ServiceProvider_DisposesCorrectly()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddSingleton<IFileVersionService, FileVersionService>()
            .BuildServiceProvider();

        // Act
        var disposing = () => provider.Dispose();

        // Assert - Should not throw
        disposing.Should().NotThrow();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
