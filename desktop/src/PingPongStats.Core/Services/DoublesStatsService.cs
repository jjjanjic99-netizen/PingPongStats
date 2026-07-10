using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>Aggregate stats for one specific pairing of two players playing as a team,
/// regardless of which match/side they were recorded on.</summary>
public record TeamPairingStats(Guid Player1Id, Guid Player2Id, int Played, int Wins, int Losses, double WinRatePct);

/// <summary>
/// Statistics specific to doubles (2 vs 2) matches: the central place that
/// answers "which team constellations perform better than others" by
/// aggregating results per unordered pair of teammates.
/// </summary>
public static class DoublesStatsService
{
    public const int LowSampleSizeThreshold = 3;

    private sealed class PairCounter
    {
        public int Played;
        public int Wins;
    }

    private readonly record struct PlayerPair(Guid Player1Id, Guid Player2Id);

    private static PlayerPair NormalizePair(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? new PlayerPair(a, b) : new PlayerPair(b, a);

    /// <summary>Win/loss record per unique pairing of teammates, ranked by win rate
    /// (ties broken by matches played). This is the core "which team constellation
    /// is better" analysis.</summary>
    public static List<TeamPairingStats> GetPairingRankings(IEnumerable<DoubleMatch> matches)
    {
        var counts = new Dictionary<PlayerPair, PairCounter>();

        void Record(Guid player1, Guid player2, bool won)
        {
            var key = NormalizePair(player1, player2);
            if (!counts.TryGetValue(key, out var counter))
            {
                counter = new PairCounter();
                counts[key] = counter;
            }

            counter.Played++;
            if (won) counter.Wins++;
        }

        foreach (var match in matches)
        {
            var teamAWon = match.WinningTeam == "A";
            Record(match.TeamAPlayer1Id, match.TeamAPlayer2Id, teamAWon);
            Record(match.TeamBPlayer1Id, match.TeamBPlayer2Id, !teamAWon);
        }

        return counts
            .Select(kv => new TeamPairingStats(
                kv.Key.Player1Id,
                kv.Key.Player2Id,
                kv.Value.Played,
                kv.Value.Wins,
                kv.Value.Played - kv.Value.Wins,
                kv.Value.Played == 0 ? 0 : kv.Value.Wins / (double)kv.Value.Played * 100))
            .OrderByDescending(s => s.WinRatePct)
            .ThenByDescending(s => s.Played)
            .ToList();
    }

    /// <summary>A player's overall doubles record (played/won/lost as part of any team).</summary>
    public static WinLossRecord GetPlayerDoublesRecord(IEnumerable<DoubleMatch> matches, Guid playerId)
    {
        var relevant = matches
            .Where(m => m.TeamAPlayer1Id == playerId || m.TeamAPlayer2Id == playerId ||
                        m.TeamBPlayer1Id == playerId || m.TeamBPlayer2Id == playerId)
            .ToList();

        var played = relevant.Count;
        var wins = relevant.Count(m =>
        {
            var onTeamA = m.TeamAPlayer1Id == playerId || m.TeamAPlayer2Id == playerId;
            return onTeamA ? m.WinningTeam == "A" : m.WinningTeam == "B";
        });
        var losses = played - wins;

        return new WinLossRecord(played, wins, losses, played == 0 ? 0 : wins / (double)played * 100);
    }

    public static bool IsLowSampleSize(int played) => played < LowSampleSizeThreshold;
}
