using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts null values to Visibility values.
    /// Null -> Collapsed, Not Null -> Visible by default.
    /// Use parameter="Inverse" to invert the behavior (Null -> Visible, Not Null -> Collapsed).
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            var isNull = value == null || (value is string str && string.IsNullOrWhiteSpace(str));

            if (isInverse)
                return isNull; // null -> visible (true), not null -> collapsed (false)
            else
                return !isNull; // null -> collapsed (false), not null -> visible (true)
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue; // Not supported
        }
    }
}
