namespace PingPongStats.Core.Models;

/// <summary>
/// One set's point score within a match. Shared by both Match (singles,
/// "A"/"B" = PlayerA/PlayerB) and DoubleMatch (doubles, "A"/"B" =
/// TeamA/TeamB). Recording set-by-set detail is optional - matches without
/// it simply have an empty/absent SetResults list.
/// </summary>
public class SetResult
{
    public int SetNumber { get; set; }
    public int PointsA { get; set; }
    public int PointsB { get; set; }
}
