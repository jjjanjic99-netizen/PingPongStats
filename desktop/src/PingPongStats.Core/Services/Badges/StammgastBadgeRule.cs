namespace PingPongStats.Core.Services.Badges;

/// <summary>"Stammgast": the player(s) with the most games (singles + doubles
/// combined, a doubles match counting for both members) in the current calendar
/// month. Ties (equal max count) are all awarded.</summary>
public class StammgastBadgeRule : IBadgeRule
{
    public string Id => "stammgast";
    public string Name => "Stammgast";
    public string Icon => "📆";
    public string Description => "Die meisten Spiele im laufenden Kalendermonat";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var monthStart = new DateTime(context.ReferenceDate.Year, context.ReferenceDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var counts = context.Players.ToDictionary(p => p.Id, _ => 0);
        var lastPlayedAtByPlayer = new Dictionary<Guid, DateTime>();

        void Record(Guid id, DateTime playedAt)
        {
            if (playedAt < monthStart || playedAt >= monthEnd) return;
            if (!counts.ContainsKey(id)) return;

            counts[id]++;
            if (!lastPlayedAtByPlayer.TryGetValue(id, out var last) || playedAt > last)
            {
                lastPlayedAtByPlayer[id] = playedAt;
            }
        }

        foreach (var m in context.Matches)
        {
            Record(m.PlayerAId, m.PlayedAt);
            Record(m.PlayerBId, m.PlayedAt);
        }

        foreach (var m in context.DoubleMatches)
        {
            Record(m.TeamAPlayer1Id, m.PlayedAt);
            Record(m.TeamAPlayer2Id, m.PlayedAt);
            Record(m.TeamBPlayer1Id, m.PlayedAt);
            Record(m.TeamBPlayer2Id, m.PlayedAt);
        }

        if (counts.Count == 0) return null;
        var maxCount = counts.Values.Max();
        if (maxCount == 0) return null;

        if (!counts.TryGetValue(playerId, out var playerCount) || playerCount != maxCount) return null;

        return new BadgeAward(Id, Name, Icon, Description, lastPlayedAtByPlayer.GetValueOrDefault(playerId, context.ReferenceDate));
    }
}
