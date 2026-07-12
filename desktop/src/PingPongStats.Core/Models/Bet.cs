namespace PingPongStats.Core.Models;

/// <summary>
/// One player's tip on the winner of a PendingMatch (Phase 15, "Wettbüro").
/// Points only - never real money. One bet per player per PendingMatch,
/// changeable until the match is resolved (enforced by BettingService, not
/// this plain data model). MatchId/DoubleMatchId stay null until the real
/// result is recorded, at which point IsResolved, Points, and ResolvedAt are
/// filled in together.
/// </summary>
public class Bet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PendingMatchId { get; set; }
    public Guid BettorPlayerId { get; set; }

    /// <summary>Singles: the predicted winning player. Null for doubles bets.</summary>
    public Guid? PredictedWinnerId { get; set; }

    /// <summary>Doubles: "A" or "B" (the predicted winning team/side). Empty for
    /// singles bets.</summary>
    public string PredictedWinningTeam { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }
    public int? Points { get; set; }
    public bool IsResolved { get; set; }

    /// <summary>The resolved match's PlayedAt (not the resolution timestamp) -
    /// used to scope a season's leaderboard the same way LeagueTableService
    /// scopes matches, by when the game was actually played.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Singles Match.Id, once the result was recorded.</summary>
    public Guid? MatchId { get; set; }

    /// <summary>Doubles DoubleMatch.Id, once the result was recorded.</summary>
    public Guid? DoubleMatchId { get; set; }
}
