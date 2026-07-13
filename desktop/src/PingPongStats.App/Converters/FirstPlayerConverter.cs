using System.Globalization;
using System.Linq;
using System.Windows.Data;
using PingPongStats.Core.Models;

namespace PingPongStats.App.Converters;

/// <summary>Turnier bracket slots resolve each side to a list of Players (1 for
/// singles, 2 for doubles) rather than a single Player - AvatarControl only
/// shows one face, so this picks the first (for doubles, one of the two team
/// members stands in for the pair; see ABWEICHUNGEN.md).</summary>
public class FirstPlayerConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as List<Player>)?.FirstOrDefault()!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
