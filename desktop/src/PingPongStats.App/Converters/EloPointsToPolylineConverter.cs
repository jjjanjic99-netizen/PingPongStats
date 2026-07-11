using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Converters;

/// <summary>Turns the profile page's normalized Elo history points into a WPF
/// PointCollection for a Polyline. ConverterParameter is "width,height" of the
/// chart canvas (must match the Canvas the Polyline is drawn on).</summary>
public class EloPointsToPolylineConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var points = new PointCollection();
        if (value is not IEnumerable<EloChartPoint> chartPoints) return points;

        var (width, height) = ParseSize(parameter as string);
        foreach (var p in chartPoints)
        {
            points.Add(new Point(p.NormalizedX * width, (1.0 - p.NormalizedY) * height));
        }

        return points;
    }

    private static (double Width, double Height) ParseSize(string? parameter)
    {
        if (parameter is not null)
        {
            var parts = parameter.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var w) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var h))
            {
                return (w, h);
            }
        }

        return (600.0, 160.0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
