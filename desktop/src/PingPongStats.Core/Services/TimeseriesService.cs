using System.Globalization;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

public enum TimeseriesGranularity
{
    Week,
    Month,
}

public record TimeseriesBucket(string Key, string Label, int Count);

/// <summary>Groups matches into chronological weekly/monthly buckets for charting.</summary>
public static class TimeseriesService
{
    private static readonly string[] MonthLabels =
    {
        "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
    };

    public static List<TimeseriesBucket> MatchesPerBucket(IEnumerable<Match> matches, TimeseriesGranularity granularity)
    {
        var buckets = new Dictionary<string, TimeseriesBucket>();

        foreach (var match in matches)
        {
            var (key, label) = granularity == TimeseriesGranularity.Week
                ? IsoWeekKey(match.PlayedAt)
                : MonthKey(match.PlayedAt);

            if (buckets.TryGetValue(key, out var existing))
            {
                buckets[key] = existing with { Count = existing.Count + 1 };
            }
            else
            {
                buckets[key] = new TimeseriesBucket(key, label, 1);
            }
        }

        return buckets.Values.OrderBy(b => b.Key, StringComparer.Ordinal).ToList();
    }

    private static (string Key, string Label) IsoWeekKey(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        var key = $"{year}-W{week:00}";
        var label = $"KW{week:00}";
        return (key, label);
    }

    private static (string Key, string Label) MonthKey(DateTime date)
    {
        var key = $"{date.Year}-{date.Month:00}";
        var label = $"{MonthLabels[date.Month - 1]} {date.Year % 100:00}";
        return (key, label);
    }
}
