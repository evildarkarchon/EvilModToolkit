using Avalonia;
using EvilModToolkit.Converters;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_False_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_Null_ReturnsUnsetValue()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(AvaloniaProperty.UnsetValue);
    }

    [Fact]
    public void Convert_NonBool_ReturnsUnsetValue()
    {
        // Act
        var result = _converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(AvaloniaProperty.UnsetValue);
    }

    [Fact]
    public void Convert_TrueWithInverseParameter_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(true, typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_FalseWithInverseParameter_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(false, typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_True_ReturnsTrue()
    {
        // Act
        var result = _converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_False_ReturnsFalse()
    {
        // Act
        var result = _converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_WithInverseParameter_InvertsValue()
    {
        // Act
        var resultTrue = _converter.ConvertBack(true, typeof(bool), "Inverse", CultureInfo.InvariantCulture);
        var resultFalse = _converter.ConvertBack(false, typeof(bool), "Inverse", CultureInfo.InvariantCulture);

        // Assert
        resultTrue.Should().Be(false);
        resultFalse.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_NonBool_ReturnsUnsetValue()
    {
        // Act
        var result = _converter.ConvertBack("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(AvaloniaProperty.UnsetValue);
    }
}