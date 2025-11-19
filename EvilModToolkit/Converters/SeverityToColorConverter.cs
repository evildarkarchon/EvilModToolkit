using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using EvilModToolkit.Models;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts SeverityLevel enum values to color brushes.
    /// Error -> Red, Warning -> Orange/Yellow, Info -> Blue
    /// </summary>
    public class SeverityToColorConverter : IValueConverter
    {
        private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        private static readonly IBrush WarningBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow/Orange
        private static readonly IBrush InfoBrush = new SolidColorBrush(Color.FromRgb(13, 202, 240)); // Light Blue
        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gray

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SeverityLevel severity)
                return DefaultBrush;

            return severity switch
            {
                SeverityLevel.Error => ErrorBrush,
                SeverityLevel.Warning => WarningBrush,
                SeverityLevel.Info => InfoBrush,
                _ => DefaultBrush
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue; // Not supported
        }
    }
}
