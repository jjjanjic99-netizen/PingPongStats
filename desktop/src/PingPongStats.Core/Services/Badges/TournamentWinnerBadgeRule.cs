using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services.Badges;

/// <summary>"Turniersieger": won at least one completed tournament (singles or
/// doubles - for doubles, both team members earn it).</summary>
public class TournamentWinnerBadgeRule : IBadgeRule
{
    public string Id => "turniersieger";
    public string Name => "Turniersieger";
    public string Icon => "🏆";
    public string Description => "Ein Turnier gewonnen";

    public BadgeAward? Evaluate(Guid playerId, BadgeContext context)
    {
        var win = context.Tournaments
            .Where(t => t.Status == TournamentStatus.Completed && t.WinnerEntrantId is not null)
            .Where(t =>
            {
                var winnerEntrant = t.Entrants.FirstOrDefault(e => e.Id == t.WinnerEntrantId);
                return winnerEntrant is not null && (winnerEntrant.Player1Id == playerId || winnerEntrant.Player2Id == playerId);
            })
            .OrderByDescending(t => t.CompletedAt)
            .FirstOrDefault();

        return win is null ? null : new BadgeAward(Id, Name, Icon, Description, win.CompletedAt ?? context.ReferenceDate);
    }
}
