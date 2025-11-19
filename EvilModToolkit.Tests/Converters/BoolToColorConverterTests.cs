using Avalonia.Media;
using EvilModToolkit.Converters;
using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class BoolToColorConverterTests
{
    private readonly BoolToColorConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsGreenBrush()
    {
        // Act
        var result = _converter.Convert(true, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(40, 167, 69));
    }

    [Fact]
    public void Convert_False_ReturnsRedBrush()
    {
        // Act
        var result = _converter.Convert(false, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(220, 53, 69));
    }

    [Fact]
    public void Convert_Null_ReturnsRedBrush()
    {
        // Act
        var result = _converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(220, 53, 69));
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsRedBrush()
    {
        // Act
        var result = _converter.Convert("not a bool", typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(220, 53, 69));
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Act
        Action act = () => _converter.ConvertBack(Brushes.Green, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }
}
