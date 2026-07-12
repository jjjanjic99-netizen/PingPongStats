using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Tests;

internal static class TestHelpers
{
    public static Match M(DateTime playedAt, Guid playerAId, Guid playerBId, int aSets, int bSets)
    {
        var winnerId = ValidationService.ComputeWinnerId(playerAId, playerBId, aSets, bSets);
        return new Match
        {
            Id = Guid.NewGuid(),
            PlayedAt = playedAt,
            PlayerAId = playerAId,
            PlayerBId = playerBId,
            PlayerASets = aSets,
            PlayerBSets = bSets,
            WinnerId = winnerId,
            CreatedAt = playedAt,
            UpdatedAt = playedAt,
        };
    }

    public static DoubleMatch DM(
        DateTime playedAt, Guid teamAP1, Guid teamAP2, Guid teamBP1, Guid teamBP2, int aSets, int bSets)
    {
        var winningTeam = ValidationService.ComputeWinningTeam(teamAP1, teamAP2, teamBP1, teamBP2, aSets, bSets);
        return new DoubleMatch
        {
            Id = Guid.NewGuid(),
            PlayedAt = playedAt,
            TeamAPlayer1Id = teamAP1,
            TeamAPlayer2Id = teamAP2,
            TeamBPlayer1Id = teamBP1,
            TeamBPlayer2Id = teamBP2,
            TeamASets = aSets,
            TeamBSets = bSets,
            WinningTeam = winningTeam,
            CreatedAt = playedAt,
            UpdatedAt = playedAt,
        };
    }
}
