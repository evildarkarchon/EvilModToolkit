using Avalonia.Data.Converters;
using Avalonia.Media;
using EvilModToolkit.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts F4SeCompatibility enum to a ColorBrush based on the installed game version.
    /// </summary>
    public class F4SeCompatibilityToColorConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2)
                return Brushes.White; // Default

            var compatibilityObj = values[0];
            var isNextGenObj = values[1];

            if (compatibilityObj is F4SeCompatibility compatibility && isNextGenObj is bool isNextGen)
            {
                return compatibility switch
                {
                    F4SeCompatibility.NgOnly => Brushes.Yellow,
                    F4SeCompatibility.OgOnly => isNextGen ? Brushes.Red : Brushes.Green,
                    F4SeCompatibility.NotF4SePlugin => Brushes.Red,
                    F4SeCompatibility.Universal => Brushes.Green, // Implicit good
                    F4SeCompatibility.Unknown => Brushes.Gray,
                    _ => Brushes.White
                };
            }

            // Fallback if binding fails or types are wrong
            return Brushes.White;
        }
    }
}
