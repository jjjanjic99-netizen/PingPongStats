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
}
