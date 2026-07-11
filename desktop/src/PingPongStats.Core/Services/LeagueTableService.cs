using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One player's standing in a season's league table.</summary>
public record LeagueTableRow(
    Guid PlayerId, int Played, int Wins, int Losses, int Points, int SetsWon, int SetsLost, int SetDifference);

/// <summary>
/// League table for a Season: 3 points per win, 0 per loss; ties broken by set
/// difference, then by the direct head-to-head result between the tied
/// players (a common, if imperfect, tiebreak once more than two players are
/// tied - non-transitive cycles are an inherent limit of "direct comparison",
/// not a bug). Singles and doubles are evaluated completely separately, both
/// producing a per-player table (a doubles match's result credits both team
/// members individually, same convention as elsewhere in this app).
/// </summary>
public static class LeagueTableService
{
    public const int PointsPerWin = 3;
    public const int PointsPerLoss = 0;

    private sealed class Aggregate
    {
        public int Wins;
        public int Losses;
        public int SetsWon;
        public int SetsLost;
    }

    private readonly record struct PlayerPair(Guid First, Guid Second);

    private static PlayerPair NormalizePair(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? new PlayerPair(a, b) : new PlayerPair(b, a);

    public static List<Match> MatchesInSeason(IEnumerable<Match> matches, Season season) =>
        matches.Where(m => m.PlayedAt >= season.StartDate && m.PlayedAt <= season.EndDate).ToList();

    public static List<DoubleMatch> DoubleMatchesInSeason(IEnumerable<DoubleMatch> matches, Season season) =>
        matches.Where(m => m.PlayedAt >= season.StartDate && m.PlayedAt <= season.EndDate).ToList();

    public static List<LeagueTableRow> BuildSinglesTable(IEnumerable<Match> matches, Season season)
    {
        var inSeason = MatchesInSeason(matches, season);
        var aggregates = new Dictionary<Guid, Aggregate>();
        var headToHeadWins = new Dictionary<PlayerPair, (int FirstWins, int SecondWins)>();

        void Record(Guid playerId, bool won, int setsWon, int setsLost)
        {
            if (!aggregates.TryGetValue(playerId, out var agg))
            {
                agg = new Aggregate();
                aggregates[playerId] = agg;
            }

            if (won) agg.Wins++;
            else agg.Losses++;
            agg.SetsWon += setsWon;
            agg.SetsLost += setsLost;
        }

        foreach (var m in inSeason)
        {
            Record(m.PlayerAId, m.WinnerId == m.PlayerAId, m.PlayerASets, m.PlayerBSets);
            Record(m.PlayerBId, m.WinnerId == m.PlayerBId, m.PlayerBSets, m.PlayerASets);
            RecordHeadToHead(headToHeadWins, m.PlayerAId, m.PlayerBId, m.WinnerId == m.PlayerAId);
        }

        return BuildRowsSorted(aggregates, headToHeadWins);
    }

    public static List<LeagueTableRow> BuildDoublesTable(IEnumerable<DoubleMatch> matches, Season season)
    {
        var inSeason = DoubleMatchesInSeason(matches, season);
        var aggregates = new Dictionary<Guid, Aggregate>();
        var headToHeadWins = new Dictionary<PlayerPair, (int FirstWins, int SecondWins)>();

        foreach (var m in inSeason)
        {
            var teamAWon = m.WinningTeam == "A";

            void RecordPlayer(Guid playerId, bool won, int setsWon, int setsLost)
            {
                if (!aggregates.TryGetValue(playerId, out var agg))
                {
                    agg = new Aggregate();
                    aggregates[playerId] = agg;
                }

                if (won) agg.Wins++;
                else agg.Losses++;
                agg.SetsWon += setsWon;
                agg.SetsLost += setsLost;
            }

            RecordPlayer(m.TeamAPlayer1Id, teamAWon, m.TeamASets, m.TeamBSets);
            RecordPlayer(m.TeamAPlayer2Id, teamAWon, m.TeamASets, m.TeamBSets);
            RecordPlayer(m.TeamBPlayer1Id, !teamAWon, m.TeamBSets, m.TeamASets);
            RecordPlayer(m.TeamBPlayer2Id, !teamAWon, m.TeamBSets, m.TeamASets);

            // Direct comparison for doubles: every opposing-team pair counts as one
            // head-to-head result each (the team's win/loss applies to both
            // cross-team pairings).
            foreach (var teamAPlayer in new[] { m.TeamAPlayer1Id, m.TeamAPlayer2Id })
            {
                foreach (var teamBPlayer in new[] { m.TeamBPlayer1Id, m.TeamBPlayer2Id })
                {
                    RecordHeadToHead(headToHeadWins, teamAPlayer, teamBPlayer, teamAWon);
                }
            }
        }

        return BuildRowsSorted(aggregates, headToHeadWins);
    }

    private static void RecordHeadToHead(
        Dictionary<PlayerPair, (int FirstWins, int SecondWins)> headToHeadWins, Guid playerAId, Guid playerBId, bool playerAWon)
    {
        var pair = NormalizePair(playerAId, playerBId);
        var (firstWins, secondWins) = headToHeadWins.GetValueOrDefault(pair);
        var aIsFirst = playerAId == pair.First;

        if (playerAWon == aIsFirst) firstWins++;
        else secondWins++;

        headToHeadWins[pair] = (firstWins, secondWins);
    }

    private static List<LeagueTableRow> BuildRowsSorted(
        Dictionary<Guid, Aggregate> aggregates, Dictionary<PlayerPair, (int FirstWins, int SecondWins)> headToHeadWins)
    {
        var rows = aggregates
            .Select(kv => new LeagueTableRow(
                kv.Key,
                kv.Value.Wins + kv.Value.Losses,
                kv.Value.Wins,
                kv.Value.Losses,
                kv.Value.Wins * PointsPerWin + kv.Value.Losses * PointsPerLoss,
                kv.Value.SetsWon,
                kv.Value.SetsLost,
                kv.Value.SetsWon - kv.Value.SetsLost))
            .ToList();

        rows.Sort((a, b) =>
        {
            var pointsCompare = b.Points.CompareTo(a.Points);
            if (pointsCompare != 0) return pointsCompare;

            var setDifferenceCompare = b.SetDifference.CompareTo(a.SetDifference);
            if (setDifferenceCompare != 0) return setDifferenceCompare;

            var pair = NormalizePair(a.PlayerId, b.PlayerId);
            if (headToHeadWins.TryGetValue(pair, out var wins))
            {
                var aIsFirst = a.PlayerId == pair.First;
                var aWins = aIsFirst ? wins.FirstWins : wins.SecondWins;
                var bWins = aIsFirst ? wins.SecondWins : wins.FirstWins;
                return bWins.CompareTo(aWins);
            }

            return 0;
        });

        return rows;
    }
}
