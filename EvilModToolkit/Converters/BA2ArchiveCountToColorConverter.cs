using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts BA2 archive count to a color brush based on percentage thresholds.
    /// Green: count &lt; 95% of limit
    /// Yellow: 95% &lt;= count &lt; 100% of limit
    /// Red: count &gt;= 100% of limit (at or over the limit)
    /// </summary>
    /// <remarks>
    /// The parameter should be the limit value (e.g., "256" for GNRL/DX10, "512" for Total).
    /// This converter implements the color coding logic from the Python version:
    /// - Green when under 95% capacity
    /// - Yellow when at 95-99% capacity (warning zone)
    /// - Red when at or over capacity (critical)
    /// </remarks>
    public class BA2ArchiveCountToColorConverter : IValueConverter
    {
        // Color definitions matching the severity levels from the original toolkit
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69));   // Success green
        private static readonly IBrush YellowBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));  // Warning yellow
        private static readonly IBrush RedBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));     // Danger red
        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gray (fallback)

        /// <summary>
        /// Converts a BA2 archive count to a color brush based on the limit.
        /// </summary>
        /// <param name="value">The current count of BA2 archives.</param>
        /// <param name="targetType">The target type (should be IBrush).</param>
        /// <param name="parameter">The limit as a string (e.g., "256" or "512").</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>A colored brush based on the percentage of the limit.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Validate that we have a count value
            if (value is not int count)
                return DefaultBrush;

            // Parse the limit parameter (e.g., "256" for GNRL/DX10, "512" for Total)
            if (parameter is not string limitStr || !int.TryParse(limitStr, out int limit) || limit <= 0)
                return DefaultBrush;

            // Calculate the 95% threshold (warning zone starts here)
            // Using int() matches the Python implementation: int(0.95 * limit)
            int warningThreshold = (int)(0.95 * limit);

            // Apply color coding based on thresholds:
            // Green: count < 95% of limit (safe zone)
            if (count < warningThreshold)
                return GreenBrush;

            // Yellow: 95% <= count < 100% of limit (warning zone)
            if (count < limit)
                return YellowBrush;

            // Red: count >= 100% of limit (critical - at or over capacity)
            return RedBrush;
        }

        /// <summary>
        /// Converts back from a brush to a count (not supported).
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue; // Not supported
        }
    }
}
