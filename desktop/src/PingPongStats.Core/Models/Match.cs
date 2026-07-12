namespace PingPongStats.Core.Models;

/// <summary>
/// A recorded ping pong match between two players. WinnerId is always
/// derived server-side (in ValidationService) from the set scores - it is
/// never set directly by UI code.
/// </summary>
public class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PlayedAt { get; set; }
    public Guid PlayerAId { get; set; }
    public Guid PlayerBId { get; set; }
    public int PlayerASets { get; set; }
    public int PlayerBSets { get; set; }
    public Guid WinnerId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Optional set-by-set score detail. Empty for matches recorded
    /// without this detail (including all data from before this field existed) -
    /// never guess/backfill it. (XmlSerializer always instantiates List&lt;T&gt;
    /// properties, so "empty" rather than null is the natural "no data" marker,
    /// consistent with how other optional fields on this model default to empty.)</summary>
    public List<SetResult> SetResults { get; set; } = new();

    /// <summary>Set when this match was played as part of a tournament bracket
    /// (Tournament.Id). Null for ordinary matches. Tournament matches are
    /// otherwise completely ordinary rows - they count for Elo/stats like any
    /// other match.</summary>
    public Guid? TournamentId { get; set; }
}
