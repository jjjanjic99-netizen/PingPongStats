using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PingPongStats.Core.Models;

namespace PingPongStats.App.Converters;

/// <summary>Mockup's Elo-Rangliste row-val arrow (▲/▼): a current win streak
/// reads as "trending up", a current loss streak as "trending down" - both
/// already-existing StreakType values, not a new metric.</summary>
public class StreakTypeToArrowConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        StreakType.Win => "▲",
        StreakType.Loss => "▼",
        _ => string.Empty,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Matching color for <see cref="StreakTypeToArrowConverter"/>: Win
/// brush for a win streak, Lose brush for a loss streak, Chalk otherwise.</summary>
public class StreakTypeToTrendBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            StreakType.Win => "Brush.Win",
            StreakType.Loss => "Brush.Lose",
            _ => "Brush.Chalk",
        };
        return Application.Current.Resources[key] as Brush ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
