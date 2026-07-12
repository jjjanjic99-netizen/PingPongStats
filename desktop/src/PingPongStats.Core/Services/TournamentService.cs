using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Pure tournament setup logic: seeding entrants by Elo (singles) or average
/// team Elo (doubles), and randomly drawing doubles teams from a player pool.
/// Bracket construction/progression itself lives in BracketService.
/// </summary>
public static class TournamentService
{
    /// <summary>One entrant per player, seeded strongest (highest Elo) first.
    /// Ties broken by display name then player id, for a fully deterministic
    /// seed order.</summary>
    public static List<TournamentEntrant> BuildSinglesEntrants(
        IEnumerable<Player> players, IReadOnlyDictionary<Guid, double> eloRatings)
    {
        var ordered = players
            .OrderByDescending(p => eloRatings.GetValueOrDefault(p.Id, EloService.DefaultInitialRating))
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Id)
            .ToList();

        return ordered.Select((p, i) => new TournamentEntrant { Player1Id = p.Id, Seed = i + 1 }).ToList();
    }

    /// <summary>One entrant per team, seeded by team Elo (average of both
    /// players' ratings, same convention as EloPredictionService.ComputeTeamElo).</summary>
    public static List<TournamentEntrant> BuildDoublesEntrants(
        IEnumerable<(Guid Player1Id, Guid Player2Id)> teams,
        IReadOnlyDictionary<Guid, double> eloRatings,
        IReadOnlyDictionary<Guid, string> displayNamesByPlayerId)
    {
        var ordered = teams
            .Select(t => new
            {
                t.Player1Id,
                t.Player2Id,
                TeamElo = EloPredictionService.ComputeTeamElo(
                    eloRatings.GetValueOrDefault(t.Player1Id, EloService.DefaultInitialRating),
                    eloRatings.GetValueOrDefault(t.Player2Id, EloService.DefaultInitialRating)),
            })
            .OrderByDescending(t => t.TeamElo)
            .ThenBy(t => displayNamesByPlayerId.GetValueOrDefault(t.Player1Id, string.Empty), StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Player1Id)
            .ToList();

        return ordered
            .Select((t, i) => new TournamentEntrant { Player1Id = t.Player1Id, Player2Id = t.Player2Id, Seed = i + 1 })
            .ToList();
    }

    /// <summary>Randomly pairs an even number of players into 2-player teams
    /// ("Teams auslosen"). Pass shuffle to get a deterministic order in tests;
    /// defaults to a real Fisher-Yates shuffle.</summary>
    public static List<(Guid Player1Id, Guid Player2Id)> DrawRandomTeams(
        IReadOnlyList<Guid> playerIds, Func<IReadOnlyList<Guid>, IReadOnlyList<Guid>>? shuffle = null)
    {
        if (playerIds.Count % 2 != 0)
        {
            throw new ValidationException("Für die Zufallsauslosung wird eine gerade Anzahl Spieler benötigt.");
        }

        var shuffled = shuffle is not null ? shuffle(playerIds) : FisherYatesShuffle(playerIds);
        var teams = new List<(Guid, Guid)>();
        for (var i = 0; i < shuffled.Count; i += 2)
        {
            teams.Add((shuffled[i], shuffled[i + 1]));
        }

        return teams;
    }

    private static List<Guid> FisherYatesShuffle(IReadOnlyList<Guid> items)
    {
        var list = items.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
