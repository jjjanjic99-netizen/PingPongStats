using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>The singles pairing with the most head-to-head games in the rivalry
/// window (see RivalryService.WindowDays).</summary>
public record RivalryResult(Guid Player1Id, Guid Player2Id, int TotalGames, int Player1Wins, int Player2Wins);

/// <summary>
/// "Rivalität des Monats": the singles pairing with the most games against
/// each other in the last 30 days, requiring at least 3 shared games. Ties
/// (equal game count) are broken by the closer record (smaller win
/// difference).
/// </summary>
public static class RivalryService
{
    public const int WindowDays = 30;
    public const int MinGames = 3;

    private readonly record struct PlayerPair(Guid Player1Id, Guid Player2Id);

    private static PlayerPair NormalizePair(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? new PlayerPair(a, b) : new PlayerPair(b, a);

    public static RivalryResult? FindRivalryOfTheMonth(
        IEnumerable<Match> matches, DateTime? referenceDate = null, int windowDays = WindowDays, int minGames = MinGames)
    {
        var reference = referenceDate ?? DateTime.Now;
        var cutoff = reference.AddDays(-windowDays);
        var recentMatches = matches.Where(m => m.PlayedAt >= cutoff && m.PlayedAt <= reference).ToList();

        var wins = new Dictionary<PlayerPair, (int Player1Wins, int Player2Wins)>();

        foreach (var match in recentMatches)
        {
            var pair = NormalizePair(match.PlayerAId, match.PlayerBId);
            var (p1Wins, p2Wins) = wins.GetValueOrDefault(pair);

            if (match.WinnerId == pair.Player1Id) p1Wins++;
            else p2Wins++;

            wins[pair] = (p1Wins, p2Wins);
        }

        return wins
            .Select(kv => new RivalryResult(kv.Key.Player1Id, kv.Key.Player2Id, kv.Value.Player1Wins + kv.Value.Player2Wins, kv.Value.Player1Wins, kv.Value.Player2Wins))
            .Where(r => r.TotalGames >= minGames)
            .OrderByDescending(r => r.TotalGames)
            .ThenBy(r => Math.Abs(r.Player1Wins - r.Player2Wins))
            .FirstOrDefault();
    }
}
