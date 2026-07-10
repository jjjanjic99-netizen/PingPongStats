using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Central, single-source-of-truth validation for match data. Used by both
/// PingPongDataService (create/update) and can be reused by any ViewModel
/// that needs to pre-validate user input before submitting.
/// </summary>
public static class ValidationService
{
    /// <summary>Validates a match's raw set score and derives the winner.
    /// Throws <see cref="ValidationException"/> with a German, user-facing message.</summary>
    public static Guid ComputeWinnerId(Guid playerAId, Guid playerBId, int playerASets, int playerBSets)
    {
        if (playerAId == Guid.Empty || playerBId == Guid.Empty)
        {
            throw new ValidationException("Beide Spieler müssen ausgewählt sein.");
        }
        if (playerAId == playerBId)
        {
            throw new ValidationException("Spieler A und Spieler B dürfen nicht identisch sein.");
        }
        if (playerASets < 0 || playerBSets < 0)
        {
            throw new ValidationException("Satzwerte dürfen nicht negativ sein.");
        }
        if (playerASets == playerBSets)
        {
            throw new ValidationException(
                "Ein Spiel darf nicht unentschieden enden - ein Spieler muss mehr Sätze gewinnen.");
        }

        return playerASets > playerBSets ? playerAId : playerBId;
    }

    /// <summary>Ensures both referenced players actually exist in the current player list.</summary>
    public static void EnsurePlayersExist(Guid playerAId, Guid playerBId, IEnumerable<Player> players)
    {
        var ids = players.Select(p => p.Id).ToHashSet();
        if (!ids.Contains(playerAId) || !ids.Contains(playerBId))
        {
            throw new ValidationException("Einer der ausgewählten Spieler existiert nicht.");
        }
    }
}
