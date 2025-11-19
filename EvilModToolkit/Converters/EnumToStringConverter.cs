using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EvilModToolkit.Converters
{
    /// <summary>
    /// Converts enum values to their string representation.
    /// Supports formatting enum values with spaces (e.g., "UserProfile" becomes "User Profile").
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!value.GetType().IsEnum)
                return value.ToString();

            var enumString = value.ToString();
            if (string.IsNullOrEmpty(enumString))
                return null;

            // Add spaces before capital letters (e.g., "UserProfile" -> "User Profile")
            return System.Text.RegularExpressions.Regex.Replace(
                enumString,
                "([a-z])([A-Z])",
                "$1 $2");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || !targetType.IsEnum)
                return null;

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            // Remove spaces for parsing
            stringValue = stringValue.Replace(" ", "");

            try
            {
                return Enum.Parse(targetType, stringValue, ignoreCase: true);
            }
            catch
            {
                return null;
            }
        }
    }
}