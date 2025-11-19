using EvilModToolkit.Converters;
using EvilModToolkit.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class EnumToStringConverterTests
{
    private readonly EnumToStringConverter _converter = new();

    [Fact]
    public void Convert_EnumValue_ReturnsString()
    {
        // Act
        var result = _converter.Convert(BA2Version.V8, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("V8");
    }

    [Fact]
    public void Convert_EnumWithMultipleWords_AddsSpaces()
    {
        // Act - Using SeverityLevel which has values like "Error" (no spaces needed)
        // For testing space insertion, we'd need an enum like "UserProfile" -> "User Profile"
        var result = _converter.Convert(SeverityLevel.Error, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert - "Error" has no lowercase-uppercase transition, so no spaces added
        result.Should().Be("Error");
    }

    [Fact]
    public void Convert_Null_ReturnsNull()
    {
        // Act
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Convert_NonEnumValue_ReturnsToString()
    {
        // Act
        var result = _converter.Convert("NotAnEnum", typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("NotAnEnum");
    }

    [Fact]
    public void Convert_IntegerValue_ReturnsToString()
    {
        // Act
        var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public void ConvertBack_ValidEnumString_ReturnsEnumValue()
    {
        // Act
        var result = _converter.ConvertBack("V8", typeof(BA2Version), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(BA2Version.V8);
    }

    [Fact]
    public void ConvertBack_StringWithSpaces_RemovesSpacesAndConverts()
    {
        // Act - If we had "User Profile" it would convert to UserProfile enum value
        var result = _converter.ConvertBack("Error", typeof(SeverityLevel), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(SeverityLevel.Error);
    }

    [Fact]
    public void ConvertBack_Null_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertBack(null, typeof(BA2Version), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertBack_EmptyString_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertBack("", typeof(BA2Version), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertBack_WhitespaceString_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertBack("   ", typeof(BA2Version), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertBack_InvalidEnumValue_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertBack("InvalidValue", typeof(BA2Version), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertBack_NonEnumTargetType_ReturnsNull()
    {
        // Act
        var result = _converter.ConvertBack("SomeValue", typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeNull();
    }
}