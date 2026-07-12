using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services.Badges;

/// <summary>
/// Runs every registered <see cref="IBadgeRule"/> against a player. Adding a new
/// badge never requires touching this class beyond one line in
/// <see cref="AllRules"/> - there is no if/else chain to extend.
/// </summary>
public static class BadgeEngine
{
    public static IReadOnlyList<IBadgeRule> AllRules { get; } = new IBadgeRule[]
    {
        new UnbeatenBadgeRule(),
        new AschenputtelBadgeRule(),
        new StammgastBadgeRule(),
        new EisenmannBadgeRule(),
        new DoublesSpecialistBadgeRule(),
        new TournamentWinnerBadgeRule(),
        new HellseherBadgeRule(),
    };

    public static BadgeContext BuildContext(
        IReadOnlyList<Player> players,
        IReadOnlyList<Match> matches,
        IReadOnlyList<DoubleMatch> doubleMatches,
        IReadOnlyList<Tournament>? tournaments = null,
        IReadOnlyList<Bet>? bets = null,
        Season? activeSeason = null,
        DateTime? referenceDate = null)
    {
        var eloRatings = EloService.ComputeRatings(matches, players.Select(p => p.Id));
        return new BadgeContext
        {
            Players = players,
            Matches = matches,
            DoubleMatches = doubleMatches,
            EloRatings = eloRatings,
            Tournaments = tournaments ?? Array.Empty<Tournament>(),
            Bets = bets ?? Array.Empty<Bet>(),
            ActiveSeason = activeSeason,
            ReferenceDate = referenceDate ?? DateTime.Now,
        };
    }

    /// <summary>All badges the given player currently qualifies for, in the order
    /// the rules are registered.</summary>
    public static List<BadgeAward> EvaluateForPlayer(Guid playerId, BadgeContext context)
    {
        var awards = new List<BadgeAward>();
        foreach (var rule in AllRules)
        {
            var award = rule.Evaluate(playerId, context);
            if (award is not null) awards.Add(award);
        }

        return awards;
    }
}
