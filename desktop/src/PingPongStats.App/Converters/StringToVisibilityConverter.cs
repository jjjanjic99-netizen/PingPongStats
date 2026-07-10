using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>Visible when the bound string is non-empty; Collapsed otherwise.
/// Used to show error/status text blocks only when there is a message.</summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
