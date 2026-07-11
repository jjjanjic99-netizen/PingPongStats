namespace PingPongStats.Core.Models;

/// <summary>
/// A manually-created league season/window. Matches are assigned to a season
/// purely by falling within [StartDate, EndDate] - there is no explicit
/// SeasonId on Match/DoubleMatch, so a match outside every season simply
/// isn't counted in any league table, while remaining a perfectly valid
/// match for all other statistics.
/// </summary>
public class Season
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
