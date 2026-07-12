namespace PingPongStats.Core.Models;

public enum TournamentMode
{
    Singles,
    Doubles,
}

public enum TournamentStatus
{
    InProgress,
    Completed,
    Aborted,
}

/// <summary>One participant in a tournament: a single player (Singles, Player2Id
/// null) or a team of two (Doubles). Seed 1 = strongest (by Elo, or average Elo
/// for a team), used to build the bracket so the strongest entrants meet as
/// late as possible.</summary>
public class TournamentEntrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid Player1Id { get; set; }
    public Guid? Player2Id { get; set; }
    public int Seed { get; set; }
}

/// <summary>
/// One match slot in the bracket tree. Round is 1-based (round 1 = first
/// round). EntrantAId/EntrantBId are null until known (fed by a previous
/// round's winner, or - only possible in round 1 - genuinely absent because
/// this slot is a bye). WinnerEntrantId is set once the slot is decided
/// (either an actual match was played, recorded via MatchId, or the slot was
/// a bye and the present entrant auto-advanced with no match played at all).
/// </summary>
public class TournamentMatchSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Round { get; set; }
    public int PositionInRound { get; set; }
    public Guid? EntrantAId { get; set; }
    public Guid? EntrantBId { get; set; }
    public bool EntrantAIsBye { get; set; }
    public bool EntrantBIsBye { get; set; }
    public Guid? WinnerEntrantId { get; set; }

    /// <summary>Match.Id or DoubleMatch.Id once this slot's match was actually
    /// played (null for byes, which have no match at all).</summary>
    public Guid? MatchId { get; set; }
}

/// <summary>
/// A single-elimination tournament. Its matches are recorded as ordinary
/// Match/DoubleMatch rows (tagged with TournamentId) so they count for Elo and
/// every other statistic exactly like any other match - the Tournament itself
/// only tracks the bracket structure and progress.
/// </summary>
public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TournamentMode Mode { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.InProgress;
    public List<TournamentEntrant> Entrants { get; set; } = new();
    public List<TournamentMatchSlot> Bracket { get; set; } = new();
    public Guid? WinnerEntrantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
