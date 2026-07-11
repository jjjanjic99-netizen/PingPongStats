using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services.Badges;

/// <summary>"Der Unbesiegte": at least 10 wins in a row, right now (an ongoing
/// streak that was later broken no longer qualifies).</summary>
public class UnbeatenBadgeRule : IBadgeRule
{
    public const int RequiredStreak = 10;

    public string Id => "der-unbesiegte";
    public string Name => "Der Unbesiegte";
    public string Icon => "🔥";
    public string Description => $"{RequiredStreak} Siege in Folge (aktuell laufend)";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var streak = StatsService.GetCurrentStreak(context.Matches, playerId);
        if (streak.Type != StreakType.Win || streak.Length < RequiredStreak) return null;

        var lastMatch = StatsService.MatchesForPlayer(context.Matches, playerId).LastOrDefault();
        return new BadgeAward(Id, Name, Icon, Description, lastMatch?.PlayedAt ?? context.ReferenceDate);
    }
}
