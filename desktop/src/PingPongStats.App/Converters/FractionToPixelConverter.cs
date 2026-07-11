using System.Globalization;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>Converts a 0..1 normalized value into a pixel coordinate for the
/// hand-rolled Elo history line chart. ConverterParameter is the axis length in
/// pixels. Pass ConverterParameter as e.g. "600" for X, or use
/// InvertForYAxis to flip top/bottom for Y (canvas Y grows downward, but a
/// higher rating should sit higher on screen).</summary>
public class FractionToPixelConverter : IValueConverter
{
    public bool InvertForYAxis { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var normalized = value is double d ? d : 0.0;
        var length = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            ? p
            : 100.0;

        var fraction = InvertForYAxis ? 1.0 - normalized : normalized;
        return fraction * length;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
