using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One player's aggregated singles+doubles performance within the "Player
/// of the Week" window, plus the derived score used to rank them.</summary>
public record PlayerOfTheWeekResult(
    Guid PlayerId,
    int Wins,
    int Losses,
    int Played,
    double WinRatePct,
    int SetDifference,
    double Score,
    double EloRating);

/// <summary>
/// "Player of the Week": exact formula per spec - Score = (Wins x 2) - Losses +
/// SetDifference x 0.5, over the last 7 days, singles and doubles both counting
/// (a doubles match counts for both members of a team). Requires at least 3 games
/// in the window to qualify. Ties broken by win rate, then games played, then
/// overall Elo rating.
/// </summary>
public static class PlayerOfTheWeekService
{
    public const int WindowDays = 7;
    public const int MinGames = 3;

    private class Accumulator
    {
        public int Wins;
        public int Losses;
        public int SetDifference;
    }

    public static PlayerOfTheWeekResult? Compute(
        IEnumerable<Match> singlesMatches,
        IEnumerable<DoubleMatch> doubleMatches,
        IEnumerable<Guid> playerIds,
        IReadOnlyDictionary<Guid, double> eloRatings,
        DateTime? referenceDate = null,
        int windowDays = WindowDays,
        int minGames = MinGames)
    {
        var reference = referenceDate ?? DateTime.Now;
        var cutoff = reference.AddDays(-windowDays);

        var stats = playerIds.ToDictionary(id => id, _ => new Accumulator());

        void Accumulate(Guid playerId, bool won, int setDifference)
        {
            if (!stats.TryGetValue(playerId, out var acc))
            {
                acc = new Accumulator();
                stats[playerId] = acc;
            }

            if (won) acc.Wins++;
            else acc.Losses++;
            acc.SetDifference += setDifference;
        }

        foreach (var m in singlesMatches.Where(m => m.PlayedAt >= cutoff && m.PlayedAt <= reference))
        {
            Accumulate(m.PlayerAId, m.WinnerId == m.PlayerAId, m.PlayerASets - m.PlayerBSets);
            Accumulate(m.PlayerBId, m.WinnerId == m.PlayerBId, m.PlayerBSets - m.PlayerASets);
        }

        foreach (var m in doubleMatches.Where(m => m.PlayedAt >= cutoff && m.PlayedAt <= reference))
        {
            var teamAWon = m.WinningTeam == "A";
            var setDifferenceA = m.TeamASets - m.TeamBSets;

            Accumulate(m.TeamAPlayer1Id, teamAWon, setDifferenceA);
            Accumulate(m.TeamAPlayer2Id, teamAWon, setDifferenceA);
            Accumulate(m.TeamBPlayer1Id, !teamAWon, -setDifferenceA);
            Accumulate(m.TeamBPlayer2Id, !teamAWon, -setDifferenceA);
        }

        var results = stats
            .Select(kv =>
            {
                var played = kv.Value.Wins + kv.Value.Losses;
                var winRatePct = played == 0 ? 0 : kv.Value.Wins / (double)played * 100;
                var score = kv.Value.Wins * 2 - kv.Value.Losses + kv.Value.SetDifference * 0.5;
                var elo = eloRatings.GetValueOrDefault(kv.Key, EloService.DefaultInitialRating);
                return new PlayerOfTheWeekResult(
                    kv.Key, kv.Value.Wins, kv.Value.Losses, played, winRatePct, kv.Value.SetDifference, score, elo);
            })
            .Where(r => r.Played >= minGames)
            .ToList();

        if (results.Count == 0) return null;

        return results
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.WinRatePct)
            .ThenByDescending(r => r.Played)
            .ThenByDescending(r => r.EloRating)
            .First();
    }
}
