using EvilModToolkit.Converters;
using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_PositiveCount_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(5, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_One_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(1, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Convert_Zero_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NegativeCount_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(-5, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_Null_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NonInteger_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert("not an integer", typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Convert_LargePositiveNumber_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(int.MaxValue, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Act
        Action act = () => _converter.ConvertBack(true, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }
}