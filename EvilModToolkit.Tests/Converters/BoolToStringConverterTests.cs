using EvilModToolkit.Converters;
using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class BoolToStringConverterTests
{
    private readonly BoolToStringConverter _converter = new();

    [Fact]
    public void Convert_True_WithParameter_ReturnsFirstPart()
    {
        // Act
        var result = _converter.Convert(true, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("Yes");
    }

    [Fact]
    public void Convert_False_WithParameter_ReturnsSecondPart()
    {
        // Act
        var result = _converter.Convert(false, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("No");
    }

    [Fact]
    public void Convert_True_WithoutParameter_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("True");
    }

    [Fact]
    public void Convert_False_WithoutParameter_ReturnsFalse()
    {
        // Act
        var result = _converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("False");
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsToString()
    {
        // Act
        var result = _converter.Convert(42, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void Convert_Null_ReturnsNull()
    {
        // Act
        var result = _converter.Convert(null, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Convert_WithInvalidParameter_ReturnsDefaultTrueFalse()
    {
        // Act - parameter without pipe
        var resultTrue = _converter.Convert(true, typeof(string), "OnlyOne", CultureInfo.InvariantCulture);
        var resultFalse = _converter.Convert(false, typeof(string), "OnlyOne", CultureInfo.InvariantCulture);

        // Assert
        resultTrue.Should().Be("True");
        resultFalse.Should().Be("False");
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Act
        Action act = () => _converter.ConvertBack("Yes", typeof(bool), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }
}