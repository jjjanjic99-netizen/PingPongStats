using System.Globalization;
using System.Windows.Data;

namespace PingPongStats.App.Converters;

/// <summary>True when the bound string equals ConverterParameter (case-sensitive).
/// Used to drive RadioButton.IsChecked from a single string property.</summary>
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter ?? Binding.DoNothing : Binding.DoNothing;
}
