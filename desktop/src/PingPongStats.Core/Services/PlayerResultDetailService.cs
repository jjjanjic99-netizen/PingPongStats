using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One match/doubles match for the Dashboard S/N mouseover tooltip
/// (R4): opponent (or opponent team, for doubles), the set score from the
/// requested player's perspective, and the date.</summary>
public record PlayerMatchDetailRow(
    Guid MatchId,
    DateTime PlayedAt,
    string OpponentLabel,
    string ScoreLabel,
    bool IsDoubles);

/// <summary>Truncated result for <see cref="PlayerResultDetailService.GetRecentResults"/>:
/// the rows to show plus the total number of matching games, so the caller can
/// render "... und {TotalCount - Rows.Count} weitere" when truncated.</summary>
public record PlayerResultDetail(IReadOnlyList<PlayerMatchDetailRow> Rows, int TotalCount)
{
    public int OverflowCount => TotalCount - Rows.Count;
}

/// <summary>Builds the match-detail list behind the Dashboard Elo-Rangliste's
/// S (Siege)/N (Niederlagen) mouseover tooltip (R4). Pure Core logic - the
/// WPF ToolTip only binds to this result, no logic lives in the View/ViewModel.</summary>
public static class PlayerResultDetailService
{
    public const int MaxRows = 8;

    public static PlayerResultDetail GetRecentResults(
        IReadOnlyList<Player> players,
        IReadOnlyList<Match> matches,
        IReadOnlyList<DoubleMatch> doubleMatches,
        Guid playerId,
        bool winsOnly,
        int maxRows = MaxRows)
    {
        var playersById = players.ToDictionary(p => p.Id);
        string NameOf(Guid id) => playersById.TryGetValue(id, out var p) ? p.DisplayName : "?";

        var rows = new List<PlayerMatchDetailRow>();

        foreach (var m in matches)
        {
            if (m.PlayerAId != playerId && m.PlayerBId != playerId) continue;
            if ((m.WinnerId == playerId) != winsOnly) continue;

            var opponentId = m.PlayerAId == playerId ? m.PlayerBId : m.PlayerAId;
            var (myScore, opponentScore) = m.PlayerAId == playerId
                ? (m.PlayerASets, m.PlayerBSets)
                : (m.PlayerBSets, m.PlayerASets);

            rows.Add(new PlayerMatchDetailRow(m.Id, m.PlayedAt, NameOf(opponentId), $"{myScore}:{opponentScore}", IsDoubles: false));
        }

        foreach (var dm in doubleMatches)
        {
            var onTeamA = dm.TeamAPlayer1Id == playerId || dm.TeamAPlayer2Id == playerId;
            var onTeamB = dm.TeamBPlayer1Id == playerId || dm.TeamBPlayer2Id == playerId;
            if (!onTeamA && !onTeamB) continue;

            var isWin = (onTeamA && dm.WinningTeam == "A") || (onTeamB && dm.WinningTeam == "B");
            if (isWin != winsOnly) continue;

            var opponentLabel = onTeamA
                ? $"{NameOf(dm.TeamBPlayer1Id)} & {NameOf(dm.TeamBPlayer2Id)}"
                : $"{NameOf(dm.TeamAPlayer1Id)} & {NameOf(dm.TeamAPlayer2Id)}";
            var (myScore, opponentScore) = onTeamA
                ? (dm.TeamASets, dm.TeamBSets)
                : (dm.TeamBSets, dm.TeamASets);

            rows.Add(new PlayerMatchDetailRow(dm.Id, dm.PlayedAt, opponentLabel, $"{myScore}:{opponentScore}", IsDoubles: true));
        }

        var ordered = rows.OrderByDescending(r => r.PlayedAt).ToList();
        return new PlayerResultDetail(ordered.Take(maxRows).ToList(), ordered.Count);
    }
}
