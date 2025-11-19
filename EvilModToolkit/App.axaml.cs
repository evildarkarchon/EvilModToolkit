using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EvilModToolkit.Services.Analysis;
using EvilModToolkit.Services.Configuration;
using EvilModToolkit.Services.Game;
using EvilModToolkit.Services.Patching;
using EvilModToolkit.Services.Platform;
using EvilModToolkit.ViewModels;
using EvilModToolkit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Configure services and logging
            _serviceProvider = ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("Evil Modding Toolkit starting...");

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
                builder.AddDebug();

                // TODO: Add file logging provider in future
                // builder.AddFile("logs/EvilModToolkit-{Date}.log");
            });

            // Register Platform Services (Singleton - system-level utilities)
            services.AddSingleton<IFileVersionService, FileVersionService>();
            services.AddSingleton<ISystemInfoService, SystemInfoService>();
            services.AddSingleton<IProcessService, ProcessService>();

            // Register Game Services (Scoped - per-operation state)
            services.AddScoped<IGameDetectionService, GameDetectionService>();
            services.AddScoped<IModManagerService, ModManagerService>();

            // Register Analysis Services (Transient - per-file analysis)
            services.AddTransient<IF4SEPluginService, F4SePluginService>();

            // Register Patching Services (Transient - per-operation)
            services.AddTransient<IBA2ArchiveService, BA2ArchiveService>();
            services.AddTransient<IXDeltaPatcherService, XDeltaPatcherService>();

            // Register Configuration Services (Singleton - app-wide settings)
            services.AddSingleton<ISettingsService, SettingsService>();

            return services.BuildServiceProvider();
        }
    }
}
