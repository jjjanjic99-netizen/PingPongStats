using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PingPongStats.App.Converters;

/// <summary>Background brush for the status banner: danger surface if IsErrorStatus,
/// success surface otherwise. Looks up the current theme's brushes at convert time so it
/// stays correct across light/dark theme switches.</summary>
public class StatusBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isError = value is true;
        var key = isError ? "Brush.DangerSurface" : "Brush.SuccessSurface";
        return Application.Current.Resources[key] as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class StatusForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isError = value is true;
        var key = isError ? "Brush.Danger" : "Brush.Success";
        return Application.Current.Resources[key] as Brush ?? Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
