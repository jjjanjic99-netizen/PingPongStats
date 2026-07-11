namespace PingPongStats.Core.Services.Badges;

/// <summary>"Doppel-Spezialist": doubles win rate higher than singles win rate,
/// requiring at least 5 doubles games. A player with zero singles games has a 0%
/// singles win rate by the existing StatsService convention, so any positive
/// doubles win rate qualifies in that case - this follows the given formula
/// literally rather than inventing an extra singles-side minimum.</summary>
public class DoublesSpecialistBadgeRule : IBadgeRule
{
    public const int MinDoublesGames = 5;

    public string Id => "doppel-spezialist";
    public string Name => "Doppel-Spezialist";
    public string Icon => "🤝";
    public string Description => $"Doppel-Siegquote höher als Einzel-Siegquote (ab {MinDoublesGames} Doppel-Spielen)";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var doublesRecord = DoublesStatsService.GetPlayerDoublesRecord(context.DoubleMatches, playerId);
        if (doublesRecord.Played < MinDoublesGames) return null;

        var singlesRecord = StatsService.GetOverallRecord(context.Matches, playerId);
        if (doublesRecord.WinRatePct <= singlesRecord.WinRatePct) return null;

        var mostRecentDoubles = context.DoubleMatches
            .Where(m => m.TeamAPlayer1Id == playerId || m.TeamAPlayer2Id == playerId ||
                        m.TeamBPlayer1Id == playerId || m.TeamBPlayer2Id == playerId)
            .OrderByDescending(m => m.PlayedAt)
            .First();

        return new BadgeAward(Id, Name, Icon, Description, mostRecentDoubles.PlayedAt);
    }
}
