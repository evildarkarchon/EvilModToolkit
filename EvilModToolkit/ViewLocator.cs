using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EvilModToolkit.ViewModels;
using EvilModToolkit.Views;

namespace EvilModToolkit
{
    /// <summary>
    /// Trim-friendly and AOT-compatible ViewLocator using switch pattern.
    /// Maps ViewModels to Views without reflection, enabling PublishTrimmed and PublishAot.
    /// </summary>
    /// <remarks>
    /// When adding new ViewModel/View pairs, add them to the switch statement below.
    /// This approach is preferred over reflection-based discovery because it:
    /// - Works with .NET trimming and Native AOT
    /// - Provides compile-time type safety
    /// - Enables IDE refactoring support
    /// - Has zero runtime overhead
    /// </remarks>
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            // Switch pattern is AOT/trim-friendly - no reflection required
            return data switch
            {
                MainWindowViewModel vm => new MainWindow { DataContext = vm },
                OverviewViewModel vm => new OverviewView { DataContext = vm },
                F4SEViewModel vm => new F4SEView { DataContext = vm },
                ScannerViewModel vm => new ScannerView { DataContext = vm },
                ToolsViewModel vm => new ToolsView { DataContext = vm },
                SettingsViewModel vm => new SettingsView { DataContext = vm },

                _ => new TextBlock
                {
                    Text = $"View not found for {data.GetType().Name}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}