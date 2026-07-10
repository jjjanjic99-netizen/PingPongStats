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
}
