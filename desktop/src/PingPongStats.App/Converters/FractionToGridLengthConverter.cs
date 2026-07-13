using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>Converts a 0..1 fraction into a star-sized GridLength, for
/// proportional bar fills built from two ColumnDefinitions (mockup's
/// prognosis bar) without a fixed pixel width. ConverterParameter="Invert"
/// gives the remainder (1 - fraction) for the second column.</summary>
public class FractionToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = value is double d ? Math.Clamp(d, 0.0, 1.0) : 0.5;
        if (string.Equals(parameter as string, "Invert", StringComparison.Ordinal)) fraction = 1.0 - fraction;
        return new GridLength(Math.Max(fraction, 0.0001), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
