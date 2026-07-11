namespace PingPongStats.Core.Services.Badges;

/// <summary>"Eisenmann": 20+ games total (singles + doubles combined).</summary>
public class EisenmannBadgeRule : IBadgeRule
{
    public const int RequiredGames = 20;

    public string Id => "eisenmann";
    public string Name => "Eisenmann";
    public string Icon => "🦾";
    public string Description => $"{RequiredGames}+ Spiele insgesamt (Einzel + Doppel)";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var singleDates = StatsService.MatchesForPlayer(context.Matches, playerId).Select(m => m.PlayedAt);
        var doubleDates = context.DoubleMatches
            .Where(m => m.TeamAPlayer1Id == playerId || m.TeamAPlayer2Id == playerId ||
                        m.TeamBPlayer1Id == playerId || m.TeamBPlayer2Id == playerId)
            .Select(m => m.PlayedAt);

        var orderedDates = singleDates.Concat(doubleDates).OrderBy(d => d).ToList();
        if (orderedDates.Count < RequiredGames) return null;

        // Earned the moment the RequiredGames-th game (chronologically) was played.
        return new BadgeAward(Id, Name, Icon, Description, orderedDates[RequiredGames - 1]);
    }
}
