using Avalonia;
using EvilModToolkit.Converters;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class NullToVisibilityConverterTests
{
    private readonly NullToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_Null_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NotNull_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert("some value", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(string.Empty, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_WhitespaceString_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert("   ", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NonNullNonString_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(42, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_NullWithInverseParameter_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_NotNullWithInverseParameter_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert("value", typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_EmptyStringWithInverseParameter_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(string.Empty, typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_ZeroValue_ReturnsTrue()
    {
        // Act - Zero is not null
        var result = _converter.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
    {
        // Act
        var result = _converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(AvaloniaProperty.UnsetValue);
    }
}
