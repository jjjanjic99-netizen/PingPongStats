namespace PingPongStats.Core.Services.Badges;

/// <summary>"Hellseher": leads the betting-tip leaderboard (Phase 15) for the
/// currently active league season. No active season, or nobody has scored a
/// single point yet, means no award to anyone. Ties for the lead are broken
/// deterministically by BettingService.GetLeaderboard itself (hit rate, then
/// player id) so exactly one player ever holds this badge at a time.</summary>
public class HellseherBadgeRule : IBadgeRule
{
    public string Id => "hellseher";
    public string Name => "Hellseher";
    public string Icon => "🔮";
    public string Description => "Führt die Tipp-Rangliste der laufenden Saison an";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        if (context.ActiveSeason is null) return null;

        var season = context.ActiveSeason;
        var seasonBets = context.Bets
            .Where(b => b.IsResolved && b.ResolvedAt is DateTime d && d >= season.StartDate && d <= season.EndDate)
            .ToList();

        var leaderboard = BettingService.GetLeaderboard(seasonBets);
        if (leaderboard.Count == 0) return null;

        var leader = leaderboard[0];
        if (leader.TotalPoints <= 0 || leader.PlayerId != playerId) return null;

        return new BadgeAward(Id, Name, Icon, Description, context.ReferenceDate);
    }
}
