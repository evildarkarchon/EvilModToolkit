using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Configuration;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Extensions;

/// <summary>
/// Extension methods for configuring services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the service collection.
    /// </summary>
    public static IServiceCollection AddEvilModToolkitServices(this IServiceCollection services)
    {
        services.AddLoggingServices();
        services.AddPlatformServices();
        services.AddGameServices();
        services.AddAnalysisServices();
        services.AddPatchingServices();
        services.AddConfigurationServices();
        services.AddViewModels();

        return services;
    }

    /// <summary>
    /// Configures logging services (Console, Debug).
    /// </summary>
    private static IServiceCollection AddLoggingServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();

            // TODO: Add file logging provider in future
            // builder.AddFile("logs/EvilModToolkit-{Date}.log");
        });

        return services;
    }

    /// <summary>
    /// Registers platform-level services (Singleton - system utilities).
    /// These services provide OS-level functionality and are shared across the application.
    /// </summary>
    private static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileVersionService, FileVersionService>();
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        services.AddSingleton<IProcessService, ProcessService>();

        return services;
    }

    /// <summary>
    /// Registers game detection and mod manager services (Scoped - per-operation state).
    /// These services maintain state during game/mod manager detection operations.
    /// </summary>
    private static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services.AddScoped<IGameDetectionService, GameDetectionService>();
        services.AddScoped<IModManagerService, ModManagerService>();

        return services;
    }

    /// <summary>
    /// Registers analysis services (Transient - per-file analysis).
    /// These services analyze individual files and should be created per operation.
    /// </summary>
    private static IServiceCollection AddAnalysisServices(this IServiceCollection services)
    {
        services.AddTransient<IF4SEPluginService, F4SePluginService>();

        return services;
    }

    /// <summary>
    /// Registers patching and archive services (Transient - per-operation).
    /// These services perform file manipulation operations.
    /// </summary>
    private static IServiceCollection AddPatchingServices(this IServiceCollection services)
    {
        services.AddTransient<IBA2ArchiveService, BA2ArchiveService>();
        services.AddTransient<IXDeltaPatcherService, XDeltaPatcherService>();

        return services;
    }

    /// <summary>
    /// Registers configuration services (Singleton - app-wide settings).
    /// These services manage application-wide configuration and settings.
    /// </summary>
    private static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();

        return services;
    }

    /// <summary>
    /// Registers ViewModels (Transient - new instance per resolution).
    /// ViewModels are created fresh for each view to ensure proper state isolation.
    /// </summary>
    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainWindowViewModel>();

        // TODO: Add other ViewModels as they are created in Phase 4:
        // services.AddTransient<OverviewViewModel>();
        // services.AddTransient<F4SEViewModel>();
        // services.AddTransient<ScannerViewModel>();
        // services.AddTransient<ToolsViewModel>();
        // services.AddTransient<SettingsViewModel>();

        return services;
    }
}
