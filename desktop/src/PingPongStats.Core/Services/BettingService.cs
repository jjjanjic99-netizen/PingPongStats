using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One player's aggregated standing on the tip leaderboard.</summary>
public record BettingLeaderboardRow(Guid PlayerId, int TotalPoints, int BetsPlaced, int CorrectPicks, double HitRatePct);

/// <summary>
/// "Wettbüro" (Phase 15): players tip the winner of an announced-but-not-yet-
/// played pairing (PendingMatch) for points only - never real money. Correct
/// pick = 1 point; if the tipped winner was, per the Elo pre-match
/// prediction, an underdog (win probability below UnderdogProbabilityThreshold),
/// a correct pick is worth PointsForUnderdogPick instead. Wrong pick = 0.
/// </summary>
public static class BettingService
{
    public const int PointsForCorrectPick = 1;
    public const int PointsForUnderdogPick = 3;
    public const double UnderdogProbabilityThreshold = 0.40;

    public static PendingMatch CreateSinglesPendingMatch(
        Guid playerAId, Guid playerBId, Guid? tournamentId = null, Guid? tournamentSlotId = null)
    {
        if (playerAId == playerBId)
        {
            throw new ValidationException("Spieler A und Spieler B dürfen nicht identisch sein.");
        }

        return new PendingMatch
        {
            Mode = TournamentMode.Singles,
            PlayerAId = playerAId,
            PlayerBId = playerBId,
            TournamentId = tournamentId,
            TournamentSlotId = tournamentSlotId,
            CreatedAt = Clock.Now(),
        };
    }

    public static PendingMatch CreateDoublesPendingMatch(
        Guid teamAPlayer1Id, Guid teamAPlayer2Id, Guid teamBPlayer1Id, Guid teamBPlayer2Id,
        Guid? tournamentId = null, Guid? tournamentSlotId = null)
    {
        var ids = new[] { teamAPlayer1Id, teamAPlayer2Id, teamBPlayer1Id, teamBPlayer2Id };
        if (ids.Distinct().Count() != 4)
        {
            throw new ValidationException("Alle vier Spieler eines Doppels müssen unterschiedlich sein.");
        }

        return new PendingMatch
        {
            Mode = TournamentMode.Doubles,
            TeamAPlayer1Id = teamAPlayer1Id,
            TeamAPlayer2Id = teamAPlayer2Id,
            TeamBPlayer1Id = teamBPlayer1Id,
            TeamBPlayer2Id = teamBPlayer2Id,
            TournamentId = tournamentId,
            TournamentSlotId = tournamentSlotId,
            CreatedAt = Clock.Now(),
        };
    }

    /// <summary>Every player id participating in this pending match (2 for
    /// singles, 4 for doubles) - used to block self-bets.</summary>
    public static List<Guid> GetParticipantIds(PendingMatch match) => match.Mode == TournamentMode.Doubles
        ? new List<Guid> { match.TeamAPlayer1Id!.Value, match.TeamAPlayer2Id!.Value, match.TeamBPlayer1Id!.Value, match.TeamBPlayer2Id!.Value }
        : new List<Guid> { match.PlayerAId!.Value, match.PlayerBId!.Value };

    /// <summary>Places a new bet, or updates the bettor's existing bet on this
    /// same pending match (one bet per player per match, changeable until
    /// resolved). Throws if the match is already resolved, if the bettor is one
    /// of the participants, or if the prediction doesn't name one of the two
    /// actual sides.</summary>
    public static Bet PlaceOrUpdateBet(
        List<Bet> bets, PendingMatch pendingMatch, Guid bettorPlayerId,
        Guid? predictedWinnerId, string? predictedWinningTeam)
    {
        if (pendingMatch.IsResolved)
        {
            throw new ValidationException("Diese Partie ist bereits abgeschlossen - ein Tipp ist nicht mehr möglich.");
        }

        if (GetParticipantIds(pendingMatch).Contains(bettorPlayerId))
        {
            throw new ValidationException("Auf eine Partie, an der man selbst beteiligt ist, kann nicht getippt werden.");
        }

        var normalizedTeam = (predictedWinningTeam ?? string.Empty).Trim();

        if (pendingMatch.Mode == TournamentMode.Doubles)
        {
            if (normalizedTeam != "A" && normalizedTeam != "B")
            {
                throw new ValidationException("Es muss Team A oder Team B getippt werden.");
            }
        }
        else if (predictedWinnerId != pendingMatch.PlayerAId && predictedWinnerId != pendingMatch.PlayerBId)
        {
            throw new ValidationException("Der getippte Sieger muss einer der beiden Spieler dieser Partie sein.");
        }

        var existing = bets.FirstOrDefault(b => b.PendingMatchId == pendingMatch.Id && b.BettorPlayerId == bettorPlayerId);
        if (existing is not null)
        {
            existing.PredictedWinnerId = pendingMatch.Mode == TournamentMode.Doubles ? null : predictedWinnerId;
            existing.PredictedWinningTeam = pendingMatch.Mode == TournamentMode.Doubles ? normalizedTeam : string.Empty;
            existing.PlacedAt = Clock.Now();
            return existing;
        }

        var bet = new Bet
        {
            PendingMatchId = pendingMatch.Id,
            BettorPlayerId = bettorPlayerId,
            PredictedWinnerId = pendingMatch.Mode == TournamentMode.Doubles ? null : predictedWinnerId,
            PredictedWinningTeam = pendingMatch.Mode == TournamentMode.Doubles ? normalizedTeam : string.Empty,
            PlacedAt = Clock.Now(),
        };
        bets.Add(bet);
        return bet;
    }

    /// <summary>Resolves every unresolved bet on this pending match once its
    /// real result is known. winnerSide is "A" or "B" - the side (single player
    /// or team) that actually won. priorEloRatings must be computed excluding
    /// the resolving match itself (same convention already used for the
    /// "isUnderdogWin" win-overlay flag) so the underdog bonus reflects the
    /// genuine pre-match prediction, not a rating already updated by this
    /// result.</summary>
    public static void ResolveBets(
        List<Bet> bets, PendingMatch pendingMatch, string winnerSide, DateTime resolvedMatchPlayedAt,
        IReadOnlyDictionary<Guid, double> priorEloRatings)
    {
        double eloA, eloB;
        if (pendingMatch.Mode == TournamentMode.Doubles)
        {
            eloA = TeamElo(priorEloRatings, pendingMatch.TeamAPlayer1Id!.Value, pendingMatch.TeamAPlayer2Id!.Value);
            eloB = TeamElo(priorEloRatings, pendingMatch.TeamBPlayer1Id!.Value, pendingMatch.TeamBPlayer2Id!.Value);
        }
        else
        {
            eloA = priorEloRatings.GetValueOrDefault(pendingMatch.PlayerAId!.Value, EloService.DefaultInitialRating);
            eloB = priorEloRatings.GetValueOrDefault(pendingMatch.PlayerBId!.Value, EloService.DefaultInitialRating);
        }

        var winProbabilityA = EloPredictionService.ComputeWinProbability(eloA, eloB);
        var winnerWinProbability = winnerSide == "A" ? winProbabilityA : 1.0 - winProbabilityA;
        var isUnderdogWin = winnerWinProbability < UnderdogProbabilityThreshold;

        var winningPlayerId = pendingMatch.Mode == TournamentMode.Singles
            ? (winnerSide == "A" ? pendingMatch.PlayerAId : pendingMatch.PlayerBId)
            : (Guid?)null;

        foreach (var bet in bets.Where(b => b.PendingMatchId == pendingMatch.Id && !b.IsResolved))
        {
            var isCorrect = pendingMatch.Mode == TournamentMode.Doubles
                ? bet.PredictedWinningTeam == winnerSide
                : bet.PredictedWinnerId == winningPlayerId;

            bet.Points = isCorrect ? (isUnderdogWin ? PointsForUnderdogPick : PointsForCorrectPick) : 0;
            bet.IsResolved = true;
            bet.ResolvedAt = resolvedMatchPlayedAt;
        }
    }

    private static double TeamElo(IReadOnlyDictionary<Guid, double> ratings, Guid player1Id, Guid player2Id) =>
        EloPredictionService.ComputeTeamElo(
            ratings.GetValueOrDefault(player1Id, EloService.DefaultInitialRating),
            ratings.GetValueOrDefault(player2Id, EloService.DefaultInitialRating));

    /// <summary>Leaderboard over the given (already filtered, e.g. by season)
    /// set of bets: total points, then hit rate, then player id, all
    /// descending/ascending as appropriate for a fully deterministic order.
    /// Only resolved bets count.</summary>
    public static List<BettingLeaderboardRow> GetLeaderboard(IEnumerable<Bet> bets)
    {
        return bets
            .Where(b => b.IsResolved)
            .GroupBy(b => b.BettorPlayerId)
            .Select(g =>
            {
                var placed = g.Count();
                var correct = g.Count(b => (b.Points ?? 0) > 0);
                return new BettingLeaderboardRow(
                    g.Key, g.Sum(b => b.Points ?? 0), placed, correct, placed == 0 ? 0 : correct / (double)placed * 100);
            })
            .OrderByDescending(r => r.TotalPoints)
            .ThenByDescending(r => r.HitRatePct)
            .ThenBy(r => r.PlayerId)
            .ToList();
    }
}
