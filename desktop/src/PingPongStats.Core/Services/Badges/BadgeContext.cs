using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services.Badges;

/// <summary>Shared, precomputed data every badge rule needs (all players/matches,
/// current Elo ratings, "now"). Built once per evaluation run via
/// <see cref="BadgeEngine.BuildContext"/> so individual rules never recompute Elo
/// or re-scan the full match lists themselves.</summary>
public class BadgeContext
{
    public required IReadOnlyList<Player> Players { get; init; }
    public required IReadOnlyList<Match> Matches { get; init; }
    public required IReadOnlyList<DoubleMatch> DoubleMatches { get; init; }
    public required IReadOnlyDictionary<Guid, double> EloRatings { get; init; }
    public IReadOnlyList<Tournament> Tournaments { get; init; } = Array.Empty<Tournament>();
    public DateTime ReferenceDate { get; init; } = DateTime.Now;
}

/// <summary>One badge a player has earned, with the description shown in the UI
/// tooltip and the date it was (first) earned. Icon is a single emoji glyph (no
/// external icon library), kept alongside the rest of the badge's identity here
/// rather than split into a separate UI-side lookup table.</summary>
public record BadgeAward(string BadgeId, string BadgeName, string Icon, string Description, DateTime EarnedAt);

/// <summary>
/// One badge rule. Adding a new badge means implementing this interface and
/// registering it in <see cref="BadgeEngine.AllRules"/> - no changes anywhere
/// else are required (no growing if/else chain).
/// </summary>
public interface IBadgeRule
{
    string Id { get; }
    string Name { get; }
    string Icon { get; }
    string Description { get; }

    /// <summary>Returns the award if this player currently qualifies for the badge,
    /// otherwise null.</summary>
    BadgeAward? Evaluate(Guid playerId, BadgeContext context);
}
