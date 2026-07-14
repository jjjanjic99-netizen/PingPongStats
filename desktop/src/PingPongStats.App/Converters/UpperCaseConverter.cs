using System.Globalization;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>Uppercases bound text (mockup's eyebrow labels use CSS
/// text-transform:uppercase, which WPF has no declarative equivalent for on
/// arbitrary bound text). Used wherever an eyebrow-style TextBlock's Text is
/// data-bound rather than a static literal (which can just be written in
/// upper case directly in XAML).</summary>
public class UpperCaseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string ?? string.Empty).ToUpper(CultureInfo.CurrentCulture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
