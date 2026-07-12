namespace PingPongStats.Core.Models;

/// <summary>
/// A recorded doubles (2 vs 2) ping pong match. WinningTeam is always derived
/// server-side from the set scores ("A" or "B"), never set directly by UI code.
/// </summary>
public class DoubleMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PlayedAt { get; set; }
    public Guid TeamAPlayer1Id { get; set; }
    public Guid TeamAPlayer2Id { get; set; }
    public Guid TeamBPlayer1Id { get; set; }
    public Guid TeamBPlayer2Id { get; set; }
    public int TeamASets { get; set; }
    public int TeamBSets { get; set; }
    public string WinningTeam { get; set; } = string.Empty; // "A" or "B"
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Optional set-by-set score detail (PointsA/PointsB = TeamA/TeamB).
    /// Empty for matches recorded without this detail - never guess/backfill it.</summary>
    public List<SetResult> SetResults { get; set; } = new();

    /// <summary>Set when this match was played as part of a tournament bracket
    /// (Tournament.Id). Null for ordinary matches.</summary>
    public Guid? TournamentId { get; set; }
}
