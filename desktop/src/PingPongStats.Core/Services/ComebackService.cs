using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>The best qualifying comeback found among a set of singles matches.</summary>
public record ComebackResult(
    Guid MatchId,
    Guid WinnerId,
    Guid LoserId,
    int ComebackValue,
    int WinnerFinalSets,
    int LoserFinalSets,
    DateTime PlayedAt,
    List<SetResult> SetResults,
    bool WinnerIsPlayerA);

/// <summary>
/// "Bestes Comeback": a comeback exists when the winner was down by at least 2
/// sets at some point and still won. Comeback value = the maximum set deficit
/// overcome; ties broken by the closer final score. Requires the Phase 0 set
/// results - matches recorded without set-by-set detail are ignored entirely,
/// never guessed or backfilled. Singles only (Match), same scope as the other
/// personal-stat calculations in this codebase.
/// </summary>
public static class ComebackService
{
    public const int MinDeficitForComeback = 2;

    /// <summary>Best comeback among the given matches, or null if none of them has
    /// both set-by-set detail and a qualifying (>= 2 set) deficit overcome.</summary>
    public static ComebackResult? FindBestComeback(IEnumerable<Match> matches)
    {
        var candidates = new List<ComebackResult>();

        foreach (var match in matches)
        {
            if (match.SetResults.Count == 0) continue;

            var winnerId = match.WinnerId;
            var loserId = winnerId == match.PlayerAId ? match.PlayerBId : match.PlayerAId;
            var winnerIsPlayerA = winnerId == match.PlayerAId;

            var orderedSets = match.SetResults.OrderBy(s => s.SetNumber).ToList();
            var maxDeficit = ComputeMaxDeficit(orderedSets, winnerIsPlayerA);
            if (maxDeficit < MinDeficitForComeback) continue;

            var winnerFinalSets = winnerIsPlayerA ? match.PlayerASets : match.PlayerBSets;
            var loserFinalSets = winnerIsPlayerA ? match.PlayerBSets : match.PlayerASets;

            candidates.Add(new ComebackResult(
                match.Id, winnerId, loserId, maxDeficit, winnerFinalSets, loserFinalSets, match.PlayedAt, orderedSets,
                winnerIsPlayerA));
        }

        if (candidates.Count == 0) return null;

        return candidates
            .OrderByDescending(c => c.ComebackValue)
            .ThenBy(c => c.WinnerFinalSets - c.LoserFinalSets)
            .First();
    }

    /// <summary>Whether this specific singles match was a comeback (false, never
    /// guessed, if it has no set-by-set detail). Used right after saving a match to
    /// decide whether to show the "COMEBACK!" badge on the win animation.</summary>
    public static bool IsComeback(Match match)
    {
        if (match.SetResults.Count == 0) return false;
        var winnerIsPlayerA = match.WinnerId == match.PlayerAId;
        return ComputeMaxDeficit(match.SetResults, winnerIsPlayerA) >= MinDeficitForComeback;
    }

    /// <summary>Doubles equivalent of <see cref="IsComeback"/>.</summary>
    public static bool IsComebackDoubles(DoubleMatch match)
    {
        if (match.SetResults.Count == 0) return false;
        var winnerIsTeamA = match.WinningTeam == "A";
        return ComputeMaxDeficit(match.SetResults, winnerIsTeamA) >= MinDeficitForComeback;
    }

    private static int ComputeMaxDeficit(IEnumerable<SetResult> setResults, bool winnerIsSideA)
    {
        var maxDeficit = 0;
        var winnerSetsWon = 0;
        var loserSetsWon = 0;

        foreach (var set in setResults.OrderBy(s => s.SetNumber))
        {
            var winnerWonThisSet = winnerIsSideA ? set.PointsA > set.PointsB : set.PointsB > set.PointsA;
            if (winnerWonThisSet) winnerSetsWon++;
            else loserSetsWon++;

            var deficit = loserSetsWon - winnerSetsWon;
            if (deficit > maxDeficit) maxDeficit = deficit;
        }

        return maxDeficit;
    }
}
