using Avalonia.Data.Converters;
using EvilModToolkit.Models;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts F4SeCompatibility enum to a user-friendly string.
    /// </summary>
    public class F4SeCompatibilityToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is F4SeCompatibility compatibility)
            {
                return compatibility switch
                {
                    F4SeCompatibility.NgOnly => "NG and newer",
                    F4SeCompatibility.OgOnly => "OG Only",
                    F4SeCompatibility.NotF4SePlugin => "Not an F4SE Plugin",
                    F4SeCompatibility.Universal => "Universal",
                    F4SeCompatibility.Unknown => "Unknown",
                    _ => value.ToString()
                };
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
