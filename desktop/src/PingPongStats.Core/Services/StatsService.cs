using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

public record WinLossRecord(int Played, int Wins, int Losses, double WinRatePct);

public record StreakInfo(StreakType Type, int Length);

public record RecentFormEntry(Guid MatchId, DateTime PlayedAt, char Result, Guid OpponentId);

public record HeadToHeadStats(
    Guid PlayerAId,
    Guid PlayerBId,
    int TotalGames,
    int PlayerAWins,
    int PlayerBWins,
    double PlayerAWinRatePct,
    double PlayerBWinRatePct);

/// <summary>
/// Central, pure statistics logic (no I/O). Every method here is deterministic
/// given its inputs, which is what makes it fully unit-testable and keeps the
/// definitions in exactly one place instead of duplicated across ViewModels.
/// </summary>
public static class StatsService
{
    public const int LowSampleSizeThreshold = 3;
    public const int LastMonthDays = 30;

    private static bool InvolvesPlayer(Match match, Guid playerId) =>
        match.PlayerAId == playerId || match.PlayerBId == playerId;

    private static bool IsWin(Match match, Guid playerId) => match.WinnerId == playerId;

    /// <summary>Matches a player took part in, sorted ascending by PlayedAt.</summary>
    public static List<Match> MatchesForPlayer(IEnumerable<Match> matches, Guid playerId) =>
        matches.Where(m => InvolvesPlayer(m, playerId)).OrderBy(m => m.PlayedAt).ToList();

    private static WinLossRecord ToRecord(IReadOnlyCollection<Match> matches, Guid playerId)
    {
        var played = matches.Count;
        var wins = matches.Count(m => IsWin(m, playerId));
        var losses = played - wins;
        var winRatePct = played == 0 ? 0 : wins / (double)played * 100;
        return new WinLossRecord(played, wins, losses, winRatePct);
    }

    /// <summary>Overall win rate: wins / played matches * 100.</summary>
    public static WinLossRecord GetOverallRecord(IEnumerable<Match> matches, Guid playerId) =>
        ToRecord(MatchesForPlayer(matches, playerId), playerId);

    /// <summary>Win rate over the last N days (default 30) relative to a reference date
    /// (defaults to now). A match counts if PlayedAt is within [referenceDate - days, referenceDate].</summary>
    public static WinLossRecord GetWinRateLastNDays(
        IEnumerable<Match> matches, Guid playerId, int days = LastMonthDays, DateTime? referenceDate = null)
    {
        var reference = referenceDate ?? DateTime.Now;
        var cutoff = reference.AddDays(-days);
        var recent = MatchesForPlayer(matches, playerId)
            .Where(m => m.PlayedAt >= cutoff && m.PlayedAt <= reference)
            .ToList();
        return ToRecord(recent, playerId);
    }

    /// <summary>Current streak: consecutive wins (or losses) counted backward from the
    /// player's most recent match, stopping at the first result that breaks it.</summary>
    public static StreakInfo GetCurrentStreak(IEnumerable<Match> matches, Guid playerId)
    {
        var ordered = MatchesForPlayer(matches, playerId);
        ordered.Reverse(); // newest first
        if (ordered.Count == 0) return new StreakInfo(StreakType.None, 0);

        var latestIsWin = IsWin(ordered[0], playerId);
        var length = 0;
        foreach (var match in ordered)
        {
            if (IsWin(match, playerId) == latestIsWin) length++;
            else break;
        }

        return new StreakInfo(latestIsWin ? StreakType.Win : StreakType.Loss, length);
    }

    /// <summary>Longest winning streak in the player's entire history (chronological
    /// run of consecutive wins, regardless of losses in between runs).</summary>
    public static int GetLongestWinStreak(IEnumerable<Match> matches, Guid playerId)
    {
        var ordered = MatchesForPlayer(matches, playerId); // oldest first
        var longest = 0;
        var current = 0;
        foreach (var match in ordered)
        {
            if (IsWin(match, playerId))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    /// <summary>Average number of sets won per match by this player.</summary>
    public static double GetAverageSetsWonPerMatch(IEnumerable<Match> matches, Guid playerId)
    {
        var played = MatchesForPlayer(matches, playerId);
        if (played.Count == 0) return 0;
        var totalSetsWon = played.Sum(m => m.PlayerAId == playerId ? m.PlayerASets : m.PlayerBSets);
        return totalSetsWon / (double)played.Count;
    }

    /// <summary>Recent form: the last N results, newest first.</summary>
    public static List<RecentFormEntry> GetRecentForm(IEnumerable<Match> matches, Guid playerId, int n = 5)
    {
        var ordered = MatchesForPlayer(matches, playerId);
        ordered.Reverse();
        return ordered.Take(n)
            .Select(m => new RecentFormEntry(
                m.Id,
                m.PlayedAt,
                IsWin(m, playerId) ? 'W' : 'L',
                m.PlayerAId == playerId ? m.PlayerBId : m.PlayerAId))
            .ToList();
    }

    /// <summary>Head-to-head statistics between two specific players, independent of
    /// which side (A/B) either player was on in any given match.</summary>
    public static HeadToHeadStats GetHeadToHead(IEnumerable<Match> matches, Guid playerAId, Guid playerBId)
    {
        var relevant = matches.Where(m =>
            (m.PlayerAId == playerAId && m.PlayerBId == playerBId) ||
            (m.PlayerAId == playerBId && m.PlayerBId == playerAId)).ToList();

        var totalGames = relevant.Count;
        var playerAWins = relevant.Count(m => m.WinnerId == playerAId);
        var playerBWins = relevant.Count(m => m.WinnerId == playerBId);

        return new HeadToHeadStats(
            playerAId,
            playerBId,
            totalGames,
            playerAWins,
            playerBWins,
            totalGames == 0 ? 0 : playerAWins / (double)totalGames * 100,
            totalGames == 0 ? 0 : playerBWins / (double)totalGames * 100);
    }

    public static bool IsLowSampleSize(int played) => played < LowSampleSizeThreshold;
}
