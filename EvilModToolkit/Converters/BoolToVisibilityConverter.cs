using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts boolean values to Visibility values.
    /// True -> Visible, False -> Collapsed by default.
    /// Use parameter="Inverse" to invert the behavior.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return AvaloniaProperty.UnsetValue;

            var isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (isInverse)
                boolValue = !boolValue;

            return boolValue ? true : false; // Avalonia uses bool for visibility binding
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return AvaloniaProperty.UnsetValue;

            var isInverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (isInverse)
                return !boolValue;

            return boolValue;
        }
    }
}