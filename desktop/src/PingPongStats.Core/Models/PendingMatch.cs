namespace PingPongStats.Core.Models;

/// <summary>
/// An announced but not-yet-played pairing that players can place bets
/// (Phase 15, "Wettbüro") on. Created either from a tournament bracket slot
/// (TournamentId/TournamentSlotId set) or standalone on the "Tippspiel" page.
/// Once the real result is recorded (via PingPongDataService.RecordPending
/// MatchSinglesResult/RecordPendingMatchDoublesResult, or automatically when a
/// linked tournament slot's result is recorded), IsResolved becomes true and
/// every bet on it is scored.
/// </summary>
public class PendingMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TournamentMode Mode { get; set; }

    // Singles: both set. Doubles: both null.
    public Guid? PlayerAId { get; set; }
    public Guid? PlayerBId { get; set; }

    // Doubles: all four set. Singles: all null.
    public Guid? TeamAPlayer1Id { get; set; }
    public Guid? TeamAPlayer2Id { get; set; }
    public Guid? TeamBPlayer1Id { get; set; }
    public Guid? TeamBPlayer2Id { get; set; }

    /// <summary>Set when this pending match was created for a tournament
    /// bracket slot, so its result is resolved automatically as part of the
    /// normal tournament match-recording flow instead of needing its own.</summary>
    public Guid? TournamentId { get; set; }
    public Guid? TournamentSlotId { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }

    /// <summary>Singles Match.Id, once the result was recorded.</summary>
    public Guid? ResolvedMatchId { get; set; }

    /// <summary>Doubles DoubleMatch.Id, once the result was recorded.</summary>
    public Guid? ResolvedDoubleMatchId { get; set; }
}
