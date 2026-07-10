using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>A single labeled value for the hand-rolled WPF bar charts.
/// NormalizedHeight is 0..1, precomputed relative to the chart's own max value,
/// so the View can bind bar height directly without needing chart-aware XAML converters.</summary>
public record ChartBarItem(string Label, double Value, string DisplayValue, double NormalizedHeight);

/// <summary>One row of the quick-result picker (e.g. "3:1").</summary>
public record QuickResultOption(int WinnerSets, int LoserSets)
{
    public string LabelForSideA => $"{WinnerSets}:{LoserSets}";
    public string LabelForSideB => $"{LoserSets}:{WinnerSets}";
}

/// <summary>Player row for the Players screen: base data + computed stats.</summary>
public class PlayerRow
{
    public required Player Player { get; init; }
    public required PlayerStatsSummary Stats { get; init; }

    public Guid Id => Player.Id;
    public string DisplayName => Player.DisplayName;
    public string FullName => Player.FullName;
    public string Email => Player.Email;
    public bool IsActive => Player.IsActive;
    public DateTime CreatedAt => Player.CreatedAt;
    public int Played => Stats.Played;
    public int Wins => Stats.Wins;
    public int Losses => Stats.Losses;
    public string WinRateLabel => $"{Stats.WinRatePct:F1}%";
    public string EloLabel => Stats.EloRating.ToString("F0");
    public bool LowSampleSize => Stats.LowSampleSize;
}

/// <summary>Match row for the Matches screen with resolved player display names.</summary>
public class MatchRow
{
    public required Match Match { get; init; }
    public required string PlayerAName { get; init; }
    public required string PlayerBName { get; init; }
    public required string WinnerName { get; init; }

    public Guid Id => Match.Id;
    public DateTime PlayedAt => Match.PlayedAt;
    public string ResultLabel => $"{Match.PlayerASets}:{Match.PlayerBSets}";
    public string Notes => Match.Notes;
}

/// <summary>Ranking row for the Dashboard's player table.</summary>
public class PlayerRankingRow
{
    public required PlayerStatsSummary Stats { get; init; }

    public string DisplayName => Stats.DisplayName;
    public bool IsActive => Stats.IsActive;
    public int EloRounded => (int)Math.Round(Stats.EloRating);
    public int Played => Stats.Played;
    public string RecordLabel => $"{Stats.Wins} / {Stats.Losses}";
    public string WinRateLabel => $"{Stats.WinRatePct:F1}%";
    public string WinRateLast30dLabel => $"{Stats.WinRateLast30dPct:F1}%";
    public string StreakLabel => Stats.CurrentStreak.Type switch
    {
        StreakType.Win => $"W x{Stats.CurrentStreak.Length}",
        StreakType.Loss => $"L x{Stats.CurrentStreak.Length}",
        _ => "-",
    };
    public string RecentFormLabel => string.Join(" ", Stats.RecentForm.Select(f => f.Result));
    public bool LowSampleSize => Stats.LowSampleSize;
}
