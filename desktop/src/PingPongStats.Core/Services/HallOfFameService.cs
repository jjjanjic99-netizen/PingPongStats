using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>One all-time record on the Rekord-Tafel: who holds it, when they
/// set it, and a ready-to-display value label. AchievedAt is always the date
/// of the specific match that set (or, for the two running-total records,
/// most recently updated) this record - see HallOfFameService's remarks.</summary>
public record HallOfFameRecord(Guid PlayerId, string ValueLabel, DateTime AchievedAt);

/// <summary>One completed season's Hall-of-Fame summary: league tables (whose
/// first row is that discipline's season winner - doubles credits both team
/// members individually, same convention as the Liga page itself) and total
/// game count.</summary>
public record SeasonHallOfFameSummary(
    Season Season, List<LeagueTableRow> SinglesTable, List<LeagueTableRow> DoublesTable, int TotalGames);

/// <summary>The complete all-time record board. Any entry is null if nobody
/// currently qualifies (e.g. no player has 20+ singles games yet) - shown as a
/// plain "no record yet" placeholder, never a guessed value.</summary>
public record RecordBoard(
    HallOfFameRecord? LongestWinStreak,
    HallOfFameRecord? HighestElo,
    HallOfFameRecord? MostGamesInOneDay,
    HallOfFameRecord? BiggestComeback,
    HallOfFameRecord? BiggestUpset,
    HallOfFameRecord? MostTotalGames,
    HallOfFameRecord? BestWinRate);

/// <summary>
/// "Hall of Fame" (Phase 16): completed seasons/tournaments and an all-time
/// record board. Every record function's tie-break is the same: whoever
/// reached the tied value first (earliest AchievedAt) wins - "längste
/// Siegserie", "höchstes Elo", "Meiste Spiele an einem Tag" and "grösste
/// Elo-Überraschung" are each tied to one specific match's date; "Meiste
/// Spiele gesamt" and "Beste Siegquote" are running totals, so AchievedAt is
/// the date of the player's most recent qualifying match (when the number was
/// last updated) - both consistently break ties toward the earlier date.
/// "Grösster Comeback" reuses ComebackService.FindBestComeback unchanged,
/// including its own established tie-break (closer final score), rather than
/// overriding an already-shipped formula.
/// "Längste Siegserie"/"Beste Siegquote" are singles-only, matching the
/// existing StatsService definitions already shown on Mein Profil (Phase 13's
/// Dashboard Streak-Alarm explicitly combines singles+doubles for a different,
/// "currently active" purpose - the all-time record here intentionally stays
/// consistent with the number already displayed elsewhere per player).
/// </summary>
public static class HallOfFameService
{
    public const int MinGamesForBestWinRate = 20;

    public static List<Season> GetCompletedSeasons(IEnumerable<Season> seasons, DateTime referenceDate) =>
        seasons.Where(s => s.EndDate < referenceDate).OrderByDescending(s => s.EndDate).ToList();

    public static SeasonHallOfFameSummary BuildSeasonSummary(
        Season season, IReadOnlyList<Match> matches, IReadOnlyList<DoubleMatch> doubleMatches)
    {
        var singlesTable = LeagueTableService.BuildSinglesTable(matches, season);
        var doublesTable = LeagueTableService.BuildDoublesTable(doubleMatches, season);
        var totalGames = LeagueTableService.MatchesInSeason(matches, season).Count
            + LeagueTableService.DoubleMatchesInSeason(doubleMatches, season).Count;

        return new SeasonHallOfFameSummary(season, singlesTable, doublesTable, totalGames);
    }

    public static RecordBoard BuildRecordBoard(
        IReadOnlyList<Player> players, IReadOnlyList<Match> matches, IReadOnlyList<DoubleMatch> doubleMatches)
    {
        return new RecordBoard(
            LongestWinStreak: GetLongestWinStreakRecord(players, matches),
            HighestElo: GetHighestEloRecord(players, matches),
            MostGamesInOneDay: GetMostGamesInOneDayRecord(players, matches, doubleMatches),
            BiggestComeback: GetBiggestComebackRecord(matches),
            BiggestUpset: GetBiggestUpsetRecord(players, matches),
            MostTotalGames: GetMostTotalGamesRecord(players, matches, doubleMatches),
            BestWinRate: GetBestWinRateRecord(players, matches));
    }

    private record Candidate(Guid PlayerId, double Value, DateTime AchievedAt);

    /// <summary>Picks the best candidate: highest Value, ties broken toward the
    /// earliest AchievedAt, then (only for the - extremely unlikely - case of a
    /// full tie on both) by player id, purely for a fully deterministic result.</summary>
    private static Candidate? PickBest(IEnumerable<Candidate> candidates) =>
        candidates.OrderByDescending(c => c.Value).ThenBy(c => c.AchievedAt).ThenBy(c => c.PlayerId).FirstOrDefault();

    private static HallOfFameRecord? GetLongestWinStreakRecord(IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        var candidates = players
            .Select(p => FindLongestWinStreak(matches, p.Id))
            .Where(c => c is not null)
            .Select(c => c!);

        var best = PickBest(candidates);
        return best is null ? null : new HallOfFameRecord(best.PlayerId, $"{(int)best.Value} Siege in Folge", best.AchievedAt);
    }

    private static Candidate? FindLongestWinStreak(IReadOnlyList<Match> matches, Guid playerId)
    {
        var ordered = StatsService.MatchesForPlayer(matches, playerId); // singles only, oldest first
        var longest = 0;
        var current = 0;
        DateTime? achievedAt = null;

        foreach (var match in ordered)
        {
            if (match.WinnerId == playerId)
            {
                current++;
                if (current > longest)
                {
                    longest = current;
                    achievedAt = match.PlayedAt;
                }
            }
            else
            {
                current = 0;
            }
        }

        return achievedAt is DateTime date ? new Candidate(playerId, longest, date) : null;
    }

    private static HallOfFameRecord? GetHighestEloRecord(IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        var candidates = new List<Candidate>();
        foreach (var player in players)
        {
            var history = EloService.GetRatingHistory(matches, player.Id);
            if (history.Count == 0) continue;

            var peak = history.OrderByDescending(h => h.Rating).ThenBy(h => h.PlayedAt).First();
            candidates.Add(new Candidate(player.Id, peak.Rating, peak.PlayedAt));
        }

        var best = PickBest(candidates);
        return best is null ? null : new HallOfFameRecord(best.PlayerId, $"{best.Value:F0} Elo", best.AchievedAt);
    }

    private static HallOfFameRecord? GetMostGamesInOneDayRecord(
        IReadOnlyList<Player> players, IReadOnlyList<Match> matches, IReadOnlyList<DoubleMatch> doubleMatches)
    {
        var countsByPlayerAndDay = new Dictionary<(Guid PlayerId, DateTime Day), int>();

        void Increment(Guid playerId, DateTime playedAt)
        {
            var key = (playerId, playedAt.Date);
            countsByPlayerAndDay[key] = countsByPlayerAndDay.GetValueOrDefault(key) + 1;
        }

        foreach (var m in matches)
        {
            Increment(m.PlayerAId, m.PlayedAt);
            Increment(m.PlayerBId, m.PlayedAt);
        }

        foreach (var m in doubleMatches)
        {
            Increment(m.TeamAPlayer1Id, m.PlayedAt);
            Increment(m.TeamAPlayer2Id, m.PlayedAt);
            Increment(m.TeamBPlayer1Id, m.PlayedAt);
            Increment(m.TeamBPlayer2Id, m.PlayedAt);
        }

        var candidates = countsByPlayerAndDay
            .Where(kv => players.Any(p => p.Id == kv.Key.PlayerId))
            .Select(kv => new Candidate(kv.Key.PlayerId, kv.Value, kv.Key.Day));

        var best = PickBest(candidates);
        return best is null ? null : new HallOfFameRecord(best.PlayerId, $"{(int)best.Value} Spiele an einem Tag", best.AchievedAt);
    }

    private static HallOfFameRecord? GetBiggestComebackRecord(IReadOnlyList<Match> matches)
    {
        var comeback = ComebackService.FindBestComeback(matches);
        return comeback is null
            ? null
            : new HallOfFameRecord(comeback.WinnerId, $"{comeback.ComebackValue} Sätze Rückstand aufgeholt", comeback.PlayedAt);
    }

    private static HallOfFameRecord? GetBiggestUpsetRecord(IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        var ordered = matches.OrderBy(m => m.PlayedAt).ToList();
        var candidates = new List<Candidate>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var match = ordered[i];
            var priorMatches = ordered.Take(i).ToList();
            var priorRatings = EloService.ComputeRatings(priorMatches, players.Select(p => p.Id));

            var ratingA = priorRatings.GetValueOrDefault(match.PlayerAId, EloService.DefaultInitialRating);
            var ratingB = priorRatings.GetValueOrDefault(match.PlayerBId, EloService.DefaultInitialRating);
            var winProbabilityA = EloPredictionService.ComputeWinProbability(ratingA, ratingB);
            var winnerWinProbability = match.WinnerId == match.PlayerAId ? winProbabilityA : 1.0 - winProbabilityA;

            // Lower predicted win probability = bigger upset, so invert for PickBest's "highest Value wins".
            candidates.Add(new Candidate(match.WinnerId, 1.0 - winnerWinProbability, match.PlayedAt));
        }

        var best = PickBest(candidates);
        if (best is null) return null;

        var upsetProbabilityPercent = (1.0 - best.Value) * 100;
        return new HallOfFameRecord(best.PlayerId, $"Sieg mit nur {upsetProbabilityPercent:F0}% Prognose", best.AchievedAt);
    }

    private static HallOfFameRecord? GetMostTotalGamesRecord(
        IReadOnlyList<Player> players, IReadOnlyList<Match> matches, IReadOnlyList<DoubleMatch> doubleMatches)
    {
        var candidates = new List<Candidate>();
        foreach (var player in players)
        {
            var singlesMatches = StatsService.MatchesForPlayer(matches, player.Id);
            var doublesMatches = doubleMatches
                .Where(m => m.TeamAPlayer1Id == player.Id || m.TeamAPlayer2Id == player.Id
                    || m.TeamBPlayer1Id == player.Id || m.TeamBPlayer2Id == player.Id)
                .ToList();

            var total = singlesMatches.Count + doublesMatches.Count;
            if (total == 0) continue;

            var lastPlayedAt = singlesMatches.Select(m => m.PlayedAt)
                .Concat(doublesMatches.Select(m => m.PlayedAt))
                .Max();

            candidates.Add(new Candidate(player.Id, total, lastPlayedAt));
        }

        var best = PickBest(candidates);
        return best is null ? null : new HallOfFameRecord(best.PlayerId, $"{(int)best.Value} Spiele gesamt", best.AchievedAt);
    }

    private static HallOfFameRecord? GetBestWinRateRecord(IReadOnlyList<Player> players, IReadOnlyList<Match> matches)
    {
        var candidates = new List<Candidate>();
        foreach (var player in players)
        {
            var record = StatsService.GetOverallRecord(matches, player.Id);
            if (record.Played < MinGamesForBestWinRate) continue;

            var lastPlayedAt = StatsService.MatchesForPlayer(matches, player.Id).Max(m => m.PlayedAt);
            candidates.Add(new Candidate(player.Id, record.WinRatePct, lastPlayedAt));
        }

        var best = PickBest(candidates);
        return best is null ? null : new HallOfFameRecord(best.PlayerId, $"{best.Value:F1}% Siegquote", best.AchievedAt);
    }
}
