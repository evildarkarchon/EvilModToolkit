using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts a boolean value to a custom string based on a parameter.
    /// Parameter format: "TrueValue|FalseValue" (e.g., "Installed|Not Installed")
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return value?.ToString();

            var parameterString = parameter as string ?? "True|False";
            var parts = parameterString.Split('|');

            if (parts.Length != 2)
                return boolValue ? "True" : "False";

            return boolValue ? parts[0] : parts[1];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToStringConverter does not support ConvertBack");
        }
    }
}