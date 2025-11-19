using System;
using System.Globalization;
using Avalonia.Media;
using EvilModToolkit.Converters;
using FluentAssertions;
using Xunit;

namespace EvilModToolkit.Tests.Converters
{
    /// <summary>
    /// Comprehensive tests for BA2ArchiveCountToColorConverter.
    /// Tests validate color thresholds based on the Python implementation:
    /// - Green: count &lt; 95% of limit
    /// - Yellow: 95% &lt;= count &lt; 100% of limit
    /// - Red: count &gt;= 100% of limit
    /// </summary>
    public class BA2ArchiveCountToColorConverterTests
    {
        private readonly BA2ArchiveCountToColorConverter _converter;
        private readonly CultureInfo _culture;

        public BA2ArchiveCountToColorConverterTests()
        {
            _converter = new BA2ArchiveCountToColorConverter();
            _culture = CultureInfo.InvariantCulture;
        }

        #region Green Color Tests (< 95%)

        /// <summary>
        /// Tests that counts well below the limit return green color.
        /// This validates the safe zone coloring.
        /// </summary>
        [Theory]
        [InlineData(0, "256")]      // 0% of limit
        [InlineData(50, "256")]     // ~19.5% of limit
        [InlineData(100, "256")]    // ~39% of limit
        [InlineData(200, "256")]    // ~78% of limit
        [InlineData(242, "256")]    // ~94.5% of limit (just under 95%)
        public void Convert_WhenCountBelowWarningThreshold_ReturnsGreen(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>("converter should return a SolidColorBrush");
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeLessThan(100, "green brush should have low red component");
            brush.Color.G.Should().BeGreaterThan(150, "green brush should have high green component");
        }

        /// <summary>
        /// Tests the exact boundary at 95% - 1 count (should be green).
        /// For limit 256: 95% = int(0.95 * 256) = 243, so 242 should be green.
        /// </summary>
        [Fact]
        public void Convert_AtExactlyOneBelowWarningThreshold_ReturnsGreen()
        {
            // Arrange - For 256 limit: int(0.95 * 256) = 243, so 242 is just below threshold
            int count = 242;
            string limit = "256";

            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.G.Should().BeGreaterThan(150, "should be green (safe zone)");
        }

        /// <summary>
        /// Tests green color with the 512 total limit.
        /// Validates that the converter works correctly with different limit values.
        /// </summary>
        [Theory]
        [InlineData(0, "512")]      // 0% of limit
        [InlineData(256, "512")]    // 50% of limit
        [InlineData(400, "512")]    // ~78% of limit
        [InlineData(485, "512")]    // ~94.7% of limit (just under 95%)
        public void Convert_WithTotalLimit_WhenBelowWarningThreshold_ReturnsGreen(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.G.Should().BeGreaterThan(150, "should be green");
        }

        #endregion

        #region Yellow Color Tests (95% <= count < 100%)

        /// <summary>
        /// Tests that counts at or above 95% but below limit return yellow color.
        /// This validates the warning zone coloring.
        /// </summary>
        [Theory]
        [InlineData(243, "256")]    // Exactly at 95% threshold: int(0.95 * 256) = 243
        [InlineData(244, "256")]    // Just above 95%
        [InlineData(250, "256")]    // ~97.6% of limit
        [InlineData(255, "256")]    // 99.6% of limit (just under 100%)
        public void Convert_WhenCountInWarningZone_ReturnsYellow(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>("converter should return a SolidColorBrush");
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "yellow brush should have high red component");
            brush.Color.G.Should().BeGreaterThan(150, "yellow brush should have high green component");
            brush.Color.B.Should().BeLessThan(50, "yellow brush should have low blue component");
        }

        /// <summary>
        /// Tests the exact 95% boundary (should be yellow).
        /// For limit 256: 95% = int(0.95 * 256) = 243, so 243 should be yellow.
        /// </summary>
        [Fact]
        public void Convert_AtExactlyWarningThreshold_ReturnsYellow()
        {
            // Arrange - For 256 limit: int(0.95 * 256) = 243
            int count = 243;
            string limit = "256";

            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "should be yellow (warning zone)");
            brush.Color.G.Should().BeGreaterThan(150, "should be yellow (warning zone)");
        }

        /// <summary>
        /// Tests yellow color with the 512 total limit.
        /// Validates warning zone for larger limits.
        /// </summary>
        [Theory]
        [InlineData(486, "512")]    // Exactly at 95% threshold: int(0.95 * 512) = 486
        [InlineData(500, "512")]    // ~97.6% of limit
        [InlineData(511, "512")]    // 99.8% of limit (just under 100%)
        public void Convert_WithTotalLimit_WhenInWarningZone_ReturnsYellow(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "should be yellow");
            brush.Color.G.Should().BeGreaterThan(150, "should be yellow");
        }

        #endregion

        #region Red Color Tests (>= 100%)

        /// <summary>
        /// Tests that counts at or above the limit return red color.
        /// This validates the critical/danger zone coloring.
        /// </summary>
        [Theory]
        [InlineData(256, "256")]    // Exactly at limit (100%)
        [InlineData(257, "256")]    // Just over limit
        [InlineData(300, "256")]    // Well over limit
        [InlineData(1000, "256")]   // Far over limit
        public void Convert_WhenCountAtOrOverLimit_ReturnsRed(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>("converter should return a SolidColorBrush");
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "red brush should have high red component");
            brush.Color.G.Should().BeLessThan(100, "red brush should have low green component");
            brush.Color.B.Should().BeLessThan(100, "red brush should have low blue component");
        }

        /// <summary>
        /// Tests the exact limit boundary (should be red).
        /// Count exactly equal to limit should be considered critical.
        /// </summary>
        [Fact]
        public void Convert_AtExactlyLimit_ReturnsRed()
        {
            // Arrange
            int count = 256;
            string limit = "256";

            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "should be red (at capacity)");
        }

        /// <summary>
        /// Tests red color with the 512 total limit.
        /// Validates critical zone for larger limits.
        /// </summary>
        [Theory]
        [InlineData(512, "512")]    // Exactly at limit
        [InlineData(513, "512")]    // Just over limit
        [InlineData(600, "512")]    // Well over limit
        public void Convert_WithTotalLimit_WhenAtOrOverLimit_ReturnsRed(int count, string limit)
        {
            // Act
            var result = _converter.Convert(count, typeof(IBrush), limit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            brush.Color.R.Should().BeGreaterThan(200, "should be red");
        }

        #endregion

        #region Edge Cases and Error Handling

        /// <summary>
        /// Tests that invalid value types return default brush.
        /// Validates defensive programming for type safety.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("not a number")]
        [InlineData(3.14)]
        [InlineData(true)]
        public void Convert_WithInvalidValueType_ReturnsDefaultBrush(object? invalidValue)
        {
            // Act
            var result = _converter.Convert(invalidValue, typeof(IBrush), "256", _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>("should return a default brush");
            // Default brush should be gray/neutral color
        }

        /// <summary>
        /// Tests that invalid parameter (limit) returns default brush.
        /// Validates error handling for malformed parameters.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not a number")]
        [InlineData("0")]       // Zero limit is invalid
        [InlineData("-256")]    // Negative limit is invalid
        public void Convert_WithInvalidLimitParameter_ReturnsDefaultBrush(object? invalidLimit)
        {
            // Act
            var result = _converter.Convert(100, typeof(IBrush), invalidLimit, _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>("should return a default brush");
        }

        /// <summary>
        /// Tests that negative counts are handled gracefully.
        /// Although negative counts shouldn't occur in practice, the converter should handle them.
        /// </summary>
        [Fact]
        public void Convert_WithNegativeCount_ReturnsGreen()
        {
            // Act
            var result = _converter.Convert(-10, typeof(IBrush), "256", _culture);

            // Assert
            result.Should().BeOfType<SolidColorBrush>();
            var brush = (SolidColorBrush)result!;
            // Negative is technically less than 95%, so should be green
            brush.Color.G.Should().BeGreaterThan(150, "negative count is below threshold");
        }

        /// <summary>
        /// Tests ConvertBack (not supported, should return UnsetValue).
        /// Validates that reverse conversion is properly marked as unsupported.
        /// </summary>
        [Fact]
        public void ConvertBack_ReturnsUnsetValue()
        {
            // Arrange
            var brush = new SolidColorBrush(Colors.Red);

            // Act
            var result = _converter.ConvertBack(brush, typeof(int), "256", _culture);

            // Assert
            result.Should().Be(Avalonia.AvaloniaProperty.UnsetValue, "ConvertBack is not supported");
        }

        #endregion

        #region Threshold Calculation Tests

        /// <summary>
        /// Tests that the 95% threshold calculation matches the Python implementation.
        /// Python uses: int(0.95 * limit), which truncates decimals.
        /// </summary>
        [Theory]
        [InlineData(256, 243)]  // int(0.95 * 256) = int(243.2) = 243
        [InlineData(512, 486)]  // int(0.95 * 512) = int(486.4) = 486
        [InlineData(100, 95)]   // int(0.95 * 100) = int(95.0) = 95
        [InlineData(1000, 950)] // int(0.95 * 1000) = int(950.0) = 950
        public void Convert_CalculatesWarningThresholdCorrectly(int limit, int expectedThreshold)
        {
            // Arrange - Count one below expected threshold should be green
            int greenCount = expectedThreshold - 1;
            // Count at expected threshold should be yellow
            int yellowCount = expectedThreshold;

            // Act
            var greenResult = _converter.Convert(greenCount, typeof(IBrush), limit.ToString(), _culture);
            var yellowResult = _converter.Convert(yellowCount, typeof(IBrush), limit.ToString(), _culture);

            // Assert
            var greenBrush = (SolidColorBrush)greenResult!;
            var yellowBrush = (SolidColorBrush)yellowResult!;

            greenBrush.Color.G.Should().BeGreaterThan(150, $"count {greenCount} should be green (< {expectedThreshold})");
            yellowBrush.Color.R.Should().BeGreaterThan(200, $"count {yellowCount} should be yellow (>= {expectedThreshold})");
            yellowBrush.Color.G.Should().BeGreaterThan(150, $"count {yellowCount} should be yellow (>= {expectedThreshold})");
        }

        /// <summary>
        /// Tests a comprehensive range of counts for 256 limit to verify all color zones.
        /// This validates the complete color gradient from safe to critical.
        /// </summary>
        [Fact]
        public void Convert_With256Limit_ShowsCorrectColorProgression()
        {
            // Arrange - Test various percentages
            var testCases = new[]
            {
                (Count: 0, ExpectedZone: "green"),
                (Count: 100, ExpectedZone: "green"),
                (Count: 200, ExpectedZone: "green"),
                (Count: 242, ExpectedZone: "green"),   // Just below 95%
                (Count: 243, ExpectedZone: "yellow"),  // At 95%
                (Count: 250, ExpectedZone: "yellow"),  // In warning zone
                (Count: 255, ExpectedZone: "yellow"),  // Just below 100%
                (Count: 256, ExpectedZone: "red"),     // At 100%
                (Count: 300, ExpectedZone: "red")      // Over 100%
            };

            foreach (var (count, expectedZone) in testCases)
            {
                // Act
                var result = _converter.Convert(count, typeof(IBrush), "256", _culture);

                // Assert
                var brush = (SolidColorBrush)result!;
                switch (expectedZone)
                {
                    case "green":
                        brush.Color.G.Should().BeGreaterThan(150, $"count {count} should be green");
                        break;
                    case "yellow":
                        brush.Color.R.Should().BeGreaterThan(200, $"count {count} should be yellow");
                        brush.Color.G.Should().BeGreaterThan(150, $"count {count} should be yellow");
                        break;
                    case "red":
                        brush.Color.R.Should().BeGreaterThan(200, $"count {count} should be red");
                        brush.Color.G.Should().BeLessThan(100, $"count {count} should be red");
                        break;
                }
            }
        }

        #endregion
    }
}
