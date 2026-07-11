using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using PingPongStats.Core.Services.Badges;

namespace PingPongStats.Core.ViewModels;

/// <summary>One selectable UI scale option (e.g. Value="Small", Label="Klein").</summary>
public record UiScaleOption(string Value, string Label);

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
    public List<BadgeAward> Badges { get; init; } = new();

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
    public string BadgeIconsLabel => string.Join(" ", Badges.Select(b => b.Icon));
    public string? BadgeNamesTooltip => Badges.Count == 0 ? null : string.Join("\n", Badges.Select(b => $"{b.Icon} {b.BadgeName}: {b.Description}"));
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

/// <summary>One ranked team pairing row for the Doubles dashboard.</summary>
public class TeamPairingRow
{
    public required TeamPairingStats Stats { get; init; }
    public required string Player1Name { get; init; }
    public required string Player2Name { get; init; }

    public string PairLabel => $"{Player1Name} & {Player2Name}";
    public int Played => Stats.Played;
    public int Wins => Stats.Wins;
    public int Losses => Stats.Losses;
    public string WinRateLabel => $"{Stats.WinRatePct:F1}%";
    public bool LowSampleSize => DoublesStatsService.IsLowSampleSize(Stats.Played);
    public string LowSampleLabel => LowSampleSize ? "< 3 Spiele" : string.Empty;
}

/// <summary>Doubles match row for the Doubles list with resolved team labels.</summary>
public class DoubleMatchRow
{
    public required DoubleMatch Match { get; init; }
    public required string TeamALabel { get; init; }
    public required string TeamBLabel { get; init; }
    public required string WinnerLabel { get; init; }

    public Guid Id => Match.Id;
    public DateTime PlayedAt => Match.PlayedAt;
    public string ResultLabel => $"{Match.TeamASets}:{Match.TeamBSets}";
    public string Notes => Match.Notes;
}

/// <summary>Raised after a match (singles or doubles) is successfully saved, so
/// MainViewModel can show the Phase 6 win/confetti overlay. WinnerId2 is set only
/// for doubles (the second member of the winning team). QuoteCategory is the
/// Phase 8 trash-talk category to show a random quote for (empty = none).</summary>
public record MatchSavedInfo(
    bool IsDoubles, Guid WinnerId1, Guid? WinnerId2, string ScoreLabel, bool IsComeback, string QuoteCategory);

/// <summary>One point of a player's Elo history line chart, pre-normalized to 0..1 on
/// both axes so the WPF view can position it on a fixed-size canvas without needing
/// any chart math of its own (mirrors the ChartBarItem pattern used elsewhere).</summary>
public record EloChartPoint(DateTime PlayedAt, double Rating, double NormalizedX, double NormalizedY);

/// <summary>One row of the "Bilanz-Countdown" (Phase 9) on Mein Profil: how many
/// more wins are needed to reach an even head-to-head record against this
/// opponent, or - if already even or ahead - the current lead instead.</summary>
public class BalanceCountdownRow
{
    public required string OpponentDisplayName { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }

    public string StatusLabel => Wins < Losses
        ? $"Noch {Losses - Wins} Siege gegen {OpponentDisplayName}"
        : Wins == Losses
            ? $"Ausgeglichen gegen {OpponentDisplayName}"
            : $"Vorsprung: {Wins - Losses} gegen {OpponentDisplayName}";
}

/// <summary>Ranking row for the Dashboard's player table.</summary>
public class PlayerRankingRow
{
    public required PlayerStatsSummary Stats { get; init; }
    public Player? Player { get; init; }
    public List<BadgeAward> Badges { get; init; } = new();

    public string BadgeIconsLabel => string.Join(" ", Badges.Select(b => b.Icon));
    public string? BadgeNamesTooltip => Badges.Count == 0 ? null : string.Join("\n", Badges.Select(b => $"{b.Icon} {b.BadgeName}: {b.Description}"));
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
