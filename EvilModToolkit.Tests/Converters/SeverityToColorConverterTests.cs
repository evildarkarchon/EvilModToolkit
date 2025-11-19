using Avalonia;
using Avalonia.Media;
using EvilModToolkit.Converters;
using EvilModToolkit.Models;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace EvilModToolkit.Tests.Converters;

public class SeverityToColorConverterTests
{
    private readonly SeverityToColorConverter _converter = new();

    [Fact]
    public void Convert_Error_ReturnsRedBrush()
    {
        // Act
        var result = _converter.Convert(SeverityLevel.Error, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(220, 53, 69));
    }

    [Fact]
    public void Convert_Warning_ReturnsYellowBrush()
    {
        // Act
        var result = _converter.Convert(SeverityLevel.Warning, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(255, 193, 7));
    }

    [Fact]
    public void Convert_Info_ReturnsLightBlueBrush()
    {
        // Act
        var result = _converter.Convert(SeverityLevel.Info, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(13, 202, 240));
    }

    [Fact]
    public void Convert_Null_ReturnsGrayBrush()
    {
        // Act
        var result = _converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(108, 117, 125));
    }

    [Fact]
    public void Convert_InvalidType_ReturnsGrayBrush()
    {
        // Act
        var result = _converter.Convert("not a severity", typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(108, 117, 125));
    }

    [Fact]
    public void Convert_IntegerValue_ReturnsGrayBrush()
    {
        // Act
        var result = _converter.Convert(42, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result!;
        brush.Color.Should().Be(Color.FromRgb(108, 117, 125));
    }

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
    {
        // Act
        var result = _converter.ConvertBack(Brushes.Red, typeof(SeverityLevel), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(AvaloniaProperty.UnsetValue);
    }
}
