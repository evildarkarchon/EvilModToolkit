using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EvilModToolkit.Extensions;
using EvilModToolkit.ViewModels;
using EvilModToolkit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit
{
    public partial class App : Application, IDisposable
    {
        private ServiceProvider? _serviceProvider;
        private bool _disposed;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        [SupportedOSPlatform("windows")]
        public override void OnFrameworkInitializationCompleted()
        {
            // Configure services and logging
            _serviceProvider = ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                logger.LogInformation("Evil Modding Toolkit starting...");

                // Resolve MainWindowViewModel from DI instead of using 'new'
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
                };

                // Wire up application shutdown for proper disposal
                desktop.ShutdownRequested += (_, _) =>
                {
                    logger.LogInformation("Evil Modding Toolkit shutting down...");
                    Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        [SupportedOSPlatform("windows")]
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register all application services using extension method
            services.AddEvilModToolkitServices();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Disposes of resources used by the application.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _serviceProvider?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}