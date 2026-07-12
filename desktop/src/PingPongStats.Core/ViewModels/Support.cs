using CommunityToolkit.Mvvm.ComponentModel;
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
/// Phase 8 trash-talk category to show a random quote for (empty = none).
/// IsTournamentFinal is true when this save just completed a tournament (Phase
/// 12), triggering the bigger trophy overlay instead of the plain win overlay.</summary>
public record MatchSavedInfo(
    bool IsDoubles, Guid WinnerId1, Guid? WinnerId2, string ScoreLabel, bool IsComeback, string QuoteCategory,
    bool IsTournamentFinal = false);

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

/// <summary>One bar of the "Tageszeit-Statistik" chart on Mein Profil.
/// IsLowSample (fewer than TimeOfDayService.MinGamesForDisplay games) tells the
/// view to grey the bar out rather than hide it.</summary>
public record TimeOfDayChartRow(string Label, int Played, string DisplayValue, double NormalizedHeight, bool IsLowSample);

/// <summary>One ranked row of a Season's league table (Liga page), joined with the
/// player's display data for the view.</summary>
public class LeagueTableDisplayRow
{
    public required LeagueTableRow Row { get; init; }
    public Player? Player { get; init; }
    public int Rank { get; init; }

    public string DisplayName => Player?.DisplayName ?? "?";
    public int Played => Row.Played;
    public int Wins => Row.Wins;
    public int Losses => Row.Losses;
    public int Points => Row.Points;
    public string SetDifferenceLabel => Row.SetDifference > 0 ? $"+{Row.SetDifference}" : Row.SetDifference.ToString();
}

/// <summary>Season row for the Settings screen's season list.</summary>
public class SeasonRow
{
    public required Season Season { get; init; }

    public Guid Id => Season.Id;
    public string Name => Season.Name;
    public bool IsActive => Season.IsActive;
    public string DateRangeLabel => $"{Season.StartDate:dd.MM.yyyy} - {Season.EndDate:dd.MM.yyyy}";
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

/// <summary>Pre-fills and locks a singles match entry for a tournament bracket
/// slot: the two players are fixed (the entrants the bracket assigned to this
/// slot), and on save the result is recorded via
/// PingPongDataService.RecordTournamentSinglesResult instead of CreateMatch.</summary>
public record TournamentMatchContext(Guid TournamentId, Guid SlotId, Guid PlayerAId, Guid PlayerBId);

/// <summary>Pre-fills and locks a singles match entry for a standalone
/// (non-tournament) pending-match tip fixture (Phase 15): the two players are
/// fixed (whoever the pending match announced), and on save the result is
/// recorded via PingPongDataService.RecordPendingMatchSinglesResult, which
/// also resolves every bet placed on it.</summary>
public record PendingMatchContext(Guid PendingMatchId, Guid PlayerAId, Guid PlayerBId);

/// <summary>Doubles equivalent of <see cref="PendingMatchContext"/>.</summary>
public record PendingMatchDoublesContext(
    Guid PendingMatchId, Guid TeamAPlayer1Id, Guid TeamAPlayer2Id, Guid TeamBPlayer1Id, Guid TeamBPlayer2Id);

/// <summary>Doubles equivalent of <see cref="TournamentMatchContext"/>.</summary>
public record TournamentDoublesMatchContext(
    Guid TournamentId, Guid SlotId,
    Guid TeamAPlayer1Id, Guid TeamAPlayer2Id, Guid TeamBPlayer1Id, Guid TeamBPlayer2Id);

/// <summary>One side (entrant) of a bracket slot, resolved to actual Player
/// objects for display. Players has 1 entry for singles, 2 for doubles.</summary>
public class TournamentEntrantDisplay
{
    public Guid? EntrantId { get; init; }
    public List<Player> Players { get; init; } = new();
    public bool IsBye { get; init; }

    public bool IsTbd => EntrantId is null && !IsBye;
    public string Label => IsBye ? "Freilos" : IsTbd ? "TBD" : string.Join(" & ", Players.Select(p => p.DisplayName));
}

/// <summary>One bracket slot ready for display, with both sides resolved to
/// player data and playability precomputed.</summary>
public class TournamentSlotRow
{
    public required TournamentMatchSlot Slot { get; init; }
    public required TournamentEntrantDisplay EntrantA { get; init; }
    public required TournamentEntrantDisplay EntrantB { get; init; }
    public string WinnerLabel { get; init; } = string.Empty;

    public bool IsPlayable => BracketService.IsPlayable(Slot);
    public bool IsDecided => Slot.WinnerEntrantId is not null;
}

/// <summary>One round of the bracket tree, with a human label ("Finale",
/// "Halbfinale", "Runde 1", ...) for display left-to-right.</summary>
public class TournamentRoundGroup
{
    public required int Round { get; init; }
    public required string RoundLabel { get; init; }
    public List<TournamentSlotRow> Slots { get; init; } = new();
}

/// <summary>One row of the tournament setup's participant checkbox list.</summary>
public partial class TournamentParticipantOption : ObservableObject
{
    public required Player Player { get; init; }

    [ObservableProperty] private bool isSelected;
}

/// <summary>One editable doubles team row in the tournament setup (manual
/// assembly, or filled in by "Teams auslosen").</summary>
public partial class TournamentTeamSlot : ObservableObject
{
    [ObservableProperty] private Player? player1;
    [ObservableProperty] private Player? player2;
}

/// <summary>One row of the Dashboard's "Streak-Alarm" card (Phase 13): a player
/// currently on a qualifying combined (singles + doubles) win streak.</summary>
public class StreakAlarmRow
{
    public required Player Player { get; init; }
    public required int StreakLength { get; init; }

    public bool IsOnFire => StreakLength >= StreakAlarmService.FireStreakThreshold;
    public string MessageLabel => $"{StreakLength} Siege in Folge – wer stoppt {Player.DisplayName}?";
}

/// <summary>One selectable tip option on a "Tippspiel" pending-match row
/// (Phase 15) - exactly two per row (the two sides), mirroring the
/// QuickResultOption pattern used for match entry.</summary>
public class BetPickOption
{
    public required Guid PendingMatchId { get; init; }
    public Guid? PredictedWinnerId { get; init; }
    public string PredictedWinningTeam { get; init; } = string.Empty;
    public required string Label { get; init; }
    public bool IsCurrentPick { get; init; }
}

/// <summary>One open (unresolved) pending match on the "Tippspiel" page,
/// together with the currently logged-in player's own betting state on it.</summary>
public class PendingMatchRow
{
    public required PendingMatch PendingMatch { get; init; }
    public required string Label { get; init; }
    public required List<BetPickOption> PickOptions { get; init; }
    public bool CurrentPlayerIsParticipant { get; init; }

    public Guid Id => PendingMatch.Id;
    public bool IsTournamentLinked => PendingMatch.TournamentId is not null;
    public string SourceLabel => IsTournamentLinked ? "Turnier" : "Freundschaftlich";
    public bool CanPlaceBet => !CurrentPlayerIsParticipant;
}

/// <summary>One row of the "Tippspiel" leaderboard.</summary>
public class BettingLeaderboardDisplayRow
{
    public required BettingLeaderboardRow Row { get; init; }
    public Player? Player { get; init; }
    public int Rank { get; init; }

    public string DisplayName => Player?.DisplayName ?? "?";
    public int TotalPoints => Row.TotalPoints;
    public int BetsPlaced => Row.BetsPlaced;
    public int CorrectPicks => Row.CorrectPicks;
    public string HitRateLabel => $"{Row.HitRatePct:F0}%";
}

/// <summary>One row of the completed-tournaments list.</summary>
public class TournamentSummaryRow
{
    public required Tournament Tournament { get; init; }
    public string WinnerLabel { get; init; } = string.Empty;

    public Guid Id => Tournament.Id;
    public string Name => Tournament.Name;
    public TournamentStatus Status => Tournament.Status;
    public string ModeLabel => Tournament.Mode == TournamentMode.Doubles ? "Doppel" : "Einzel";
    public string StatusLabel => Tournament.Status switch
    {
        TournamentStatus.Completed => "Abgeschlossen",
        TournamentStatus.Aborted => "Abgebrochen",
        _ => "Laufend",
    };
}
