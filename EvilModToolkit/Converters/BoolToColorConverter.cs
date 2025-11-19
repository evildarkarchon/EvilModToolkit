using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts a boolean value to a color (green for true, red for false).
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        private static readonly IBrush TrueBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
        private static readonly IBrush FalseBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }

            return FalseBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToColorConverter does not support ConvertBack");
        }
    }
}
