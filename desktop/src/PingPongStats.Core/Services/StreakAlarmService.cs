using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One player currently on a qualifying win streak.</summary>
public record StreakAlarmEntry(Guid PlayerId, int StreakLength);

/// <summary>
/// "Streak-Alarm": players with a currently ongoing win streak of at least
/// <see cref="MinStreakForAlarm"/> games. Singles and doubles both count
/// toward the same streak, combined purely chronologically by PlayedAt - the
/// first loss (of either kind), working backward from the player's most
/// recent game, ends it.
/// </summary>
public static class StreakAlarmService
{
    public const int MinStreakForAlarm = 3;
    public const int FireStreakThreshold = 5;

    /// <summary>The player's current win streak length: 0 if their most recent
    /// game (singles or doubles) was a loss, or if they have no games at all.</summary>
    public static int GetCurrentCombinedWinStreak(
        IEnumerable<Match> matches, IEnumerable<DoubleMatch> doubleMatches, Guid playerId)
    {
        var results = new List<(DateTime PlayedAt, bool IsWin)>();

        foreach (var m in matches)
        {
            if (m.PlayerAId != playerId && m.PlayerBId != playerId) continue;
            results.Add((m.PlayedAt, m.WinnerId == playerId));
        }

        foreach (var m in doubleMatches)
        {
            var onTeamA = m.TeamAPlayer1Id == playerId || m.TeamAPlayer2Id == playerId;
            var onTeamB = m.TeamBPlayer1Id == playerId || m.TeamBPlayer2Id == playerId;
            if (!onTeamA && !onTeamB) continue;
            var won = (onTeamA && m.WinningTeam == "A") || (onTeamB && m.WinningTeam == "B");
            results.Add((m.PlayedAt, won));
        }

        if (results.Count == 0) return 0;

        var ordered = results.OrderByDescending(r => r.PlayedAt).ToList();
        if (!ordered[0].IsWin) return 0;

        var length = 0;
        foreach (var r in ordered)
        {
            if (r.IsWin) length++;
            else break;
        }

        return length;
    }

    /// <summary>All players currently on a qualifying streak (&gt;= MinStreakForAlarm),
    /// longest streak first.</summary>
    public static List<StreakAlarmEntry> GetPlayersOnStreak(
        IEnumerable<Player> players, IEnumerable<Match> matches, IEnumerable<DoubleMatch> doubleMatches)
    {
        var matchList = matches as IReadOnlyCollection<Match> ?? matches.ToList();
        var doubleMatchList = doubleMatches as IReadOnlyCollection<DoubleMatch> ?? doubleMatches.ToList();

        return players
            .Select(p => new StreakAlarmEntry(p.Id, GetCurrentCombinedWinStreak(matchList, doubleMatchList, p.Id)))
            .Where(e => e.StreakLength >= MinStreakForAlarm)
            .OrderByDescending(e => e.StreakLength)
            .ToList();
    }
}
