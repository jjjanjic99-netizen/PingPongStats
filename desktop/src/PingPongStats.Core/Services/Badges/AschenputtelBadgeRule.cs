namespace PingPongStats.Core.Services.Badges;

/// <summary>"Aschenputtel": beat a player currently ranked in the Elo top 3
/// (singles only). Awarded for the most recent such win.</summary>
public class AschenputtelBadgeRule : IBadgeRule
{
    public const int TopN = 3;

    public string Id => "aschenputtel";
    public string Name => "Aschenputtel";
    public string Icon => "🥿";
    public string Description => $"Sieg gegen einen Spieler aus den Top {TopN} der Elo-Rangliste";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var topPlayerIds = context.EloRatings
            .OrderByDescending(kv => kv.Value)
            .Take(TopN)
            .Select(kv => kv.Key)
            .ToHashSet();

        var qualifyingWin = StatsService.MatchesForPlayer(context.Matches, playerId)
            .Where(m => m.WinnerId == playerId)
            .Where(m => topPlayerIds.Contains(m.PlayerAId == playerId ? m.PlayerBId : m.PlayerAId))
            .OrderByDescending(m => m.PlayedAt)
            .FirstOrDefault();

        return qualifyingWin is null ? null : new BadgeAward(Id, Name, Icon, Description, qualifyingWin.PlayedAt);
    }
}
