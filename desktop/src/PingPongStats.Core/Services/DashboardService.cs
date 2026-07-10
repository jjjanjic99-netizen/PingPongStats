using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

public record PlayerStatsSummary(
    Guid PlayerId,
    string DisplayName,
    bool IsActive,
    int Played,
    int Wins,
    int Losses,
    double WinRatePct,
    double WinRateLast30dPct,
    StreakInfo CurrentStreak,
    int LongestWinStreak,
    double AvgSetsWonPerMatch,
    double EloRating,
    List<RecentFormEntry> RecentForm,
    bool LowSampleSize);

public record DashboardSnapshot(
    int TotalMatches,
    int ActivePlayerCount,
    List<PlayerStatsSummary> Players,
    PlayerStatsSummary? MostWins,
    PlayerStatsSummary? HighestOverallWinRate,
    PlayerStatsSummary? HighestWinRateLastMonth,
    PlayerStatsSummary? BestCurrentStreak,
    List<TimeseriesBucket> MatchesPerWeek,
    List<TimeseriesBucket> MatchesPerMonth,
    double AverageSetsWonPerMatchOverall);

/// <summary>
/// Aggregates the individual StatsService/EloService calculations into the
/// complete dashboard snapshot shown in the UI. Pure function of its inputs -
/// no I/O, fully unit-testable.
/// </summary>
public static class DashboardService
{
    public static DashboardSnapshot BuildDashboard(IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        var eloRatings = EloService.ComputeRatings(matches, players.Select(p => p.Id));

        var summaries = players.Select(p => BuildPlayerSummary(p, matches, eloRatings)).ToList();
        var withGames = summaries.Where(s => s.Played > 0).ToList();

        var totalSetsWon = matches.Sum(m => m.PlayerASets + m.PlayerBSets);
        var averageSetsOverall = matches.Count == 0 ? 0 : totalSetsWon / (double)(matches.Count * 2);

        return new DashboardSnapshot(
            TotalMatches: matches.Count,
            ActivePlayerCount: players.Count(p => p.IsActive),
            Players: summaries,
            MostWins: PickBy(withGames, s => s.Wins),
            HighestOverallWinRate: PickBy(withGames, s => s.WinRatePct),
            HighestWinRateLastMonth: PickBy(withGames, s => s.WinRateLast30dPct),
            BestCurrentStreak: PickBy(withGames, s => s.CurrentStreak.Type == StreakType.Win ? s.CurrentStreak.Length : -1),
            MatchesPerWeek: TimeseriesService.MatchesPerBucket(matches, TimeseriesGranularity.Week),
            MatchesPerMonth: TimeseriesService.MatchesPerBucket(matches, TimeseriesGranularity.Month),
            AverageSetsWonPerMatchOverall: averageSetsOverall);
    }

    private static PlayerStatsSummary BuildPlayerSummary(
        Player player, IReadOnlyList<Match> matches, Dictionary<Guid, double> eloRatings)
    {
        var overall = StatsService.GetOverallRecord(matches, player.Id);
        var last30d = StatsService.GetWinRateLastNDays(matches, player.Id);

        return new PlayerStatsSummary(
            PlayerId: player.Id,
            DisplayName: player.DisplayName,
            IsActive: player.IsActive,
            Played: overall.Played,
            Wins: overall.Wins,
            Losses: overall.Losses,
            WinRatePct: overall.WinRatePct,
            WinRateLast30dPct: last30d.WinRatePct,
            CurrentStreak: StatsService.GetCurrentStreak(matches, player.Id),
            LongestWinStreak: StatsService.GetLongestWinStreak(matches, player.Id),
            AvgSetsWonPerMatch: StatsService.GetAverageSetsWonPerMatch(matches, player.Id),
            EloRating: eloRatings.GetValueOrDefault(player.Id, EloService.DefaultInitialRating),
            RecentForm: StatsService.GetRecentForm(matches, player.Id, 5),
            LowSampleSize: StatsService.IsLowSampleSize(overall.Played));
    }

    private static T? PickBy<T>(List<T> items, Func<T, double> key) where T : class
    {
        if (items.Count == 0) return null;
        return items.Aggregate((best, item) => key(item) > key(best) ? item : best);
    }
}
