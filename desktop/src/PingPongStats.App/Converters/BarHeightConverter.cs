using System.Globalization;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>Converts a 0..1 normalized value into a pixel height for the
/// hand-rolled bar charts. ConverterParameter is the max pixel height (defaults to 160).</summary>
public class BarHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var normalized = value is double d ? d : 0.0;
        var maxHeight = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            ? p
            : 160.0;

        var height = normalized * maxHeight;
        return height < 2 ? 2.0 : height; // keep a visible sliver even for very small/zero values
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
