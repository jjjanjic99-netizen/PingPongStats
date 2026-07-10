using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Generates deterministic (fixed random seed) demo data for manual testing
/// and screenshots: 8 players and 65 matches spread over the last 120 days
/// with skill-weighted, realistic outcomes. Intended to be triggered only via
/// a Debug-only button in Settings, never automatically in production use.
/// </summary>
public static class SeedDataService
{
    private static readonly (string DisplayName, string FirstName, string LastName, int Skill, bool IsActive)[] PlayerDefs =
    {
        ("Anna Berger", "Anna", "Berger", 1650, true),
        ("Ben Hofer", "Ben", "Hofer", 1580, true),
        ("Clara Wagner", "Clara", "Wagner", 1720, true),
        ("David Keller", "David", "Keller", 1500, true),
        ("Elena Frei", "Elena", "Frei", 1610, true),
        ("Fabian Roth", "Fabian", "Roth", 1470, true),
        ("Giulia Meier", "Giulia", "Meier", 1550, true),
        ("Hannes Stark", "Hannes", "Stark", 1440, false),
    };

    private static readonly (int Loser, int Winner)[] ResultModes =
    {
        (0, 1), (0, 2), (1, 2), (0, 3), (1, 3), (2, 3),
    };

    private const int MatchCount = 65;
    private const int DoubleMatchCount = 25;
    private const int DaysSpan = 120;

    public static (List<Player> Players, List<Match> Matches, List<DoubleMatch> DoubleMatches) Generate(int randomSeed = 42)
    {
        var random = new Random(randomSeed);
        var now = Clock.Now();

        var players = PlayerDefs.Select(def => new Player
        {
            Id = Guid.NewGuid(),
            DisplayName = def.DisplayName,
            FirstName = def.FirstName,
            LastName = def.LastName,
            Email = $"{def.FirstName.ToLowerInvariant()}.{def.LastName.ToLowerInvariant()}@firma.example",
            IsActive = def.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        var skillById = players
            .Select((p, i) => (p.Id, Skill: PlayerDefs[i].Skill))
            .ToDictionary(x => x.Id, x => x.Skill);

        var matchDates = Enumerable.Range(0, MatchCount)
            .Select(_ =>
            {
                var daysAgo = random.Next(0, DaysSpan + 1);
                var date = now.Date.AddDays(-daysAgo)
                    .AddHours(random.Next(8, 19))
                    .AddMinutes(new[] { 0, 15, 30, 45 }[random.Next(4)]);
                return date;
            })
            .OrderBy(d => d)
            .ToList();

        var matches = new List<Match>();
        foreach (var playedAt in matchDates)
        {
            var playerA = players[random.Next(players.Count)];
            Player playerB;
            do
            {
                playerB = players[random.Next(players.Count)];
            } while (playerB.Id == playerA.Id);

            var skillA = skillById[playerA.Id];
            var skillB = skillById[playerB.Id];
            var expectedA = 1.0 / (1.0 + Math.Pow(10, (skillB - skillA) / 400.0));
            var aWins = random.NextDouble() < expectedA;

            var (loserSets, winnerSets) = ResultModes[random.Next(ResultModes.Length)];
            var playerASets = aWins ? winnerSets : loserSets;
            var playerBSets = aWins ? loserSets : winnerSets;

            var winnerId = ValidationService.ComputeWinnerId(playerA.Id, playerB.Id, playerASets, playerBSets);

            matches.Add(new Match
            {
                Id = Guid.NewGuid(),
                PlayedAt = playedAt,
                PlayerAId = playerA.Id,
                PlayerBId = playerB.Id,
                PlayerASets = playerASets,
                PlayerBSets = playerBSets,
                WinnerId = winnerId,
                Notes = random.NextDouble() < 0.15
                    ? new[] { "Knappes Spiel!", "Rematch gefordert", "Gutes Niveau heute" }[random.Next(3)]
                    : string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        var activePlayers = players.Where(p => p.IsActive).ToList();
        var doubleMatches = new List<DoubleMatch>();

        if (activePlayers.Count >= 4)
        {
            var doubleDates = Enumerable.Range(0, DoubleMatchCount)
                .Select(_ =>
                {
                    var daysAgo = random.Next(0, DaysSpan + 1);
                    return now.Date.AddDays(-daysAgo)
                        .AddHours(random.Next(8, 19))
                        .AddMinutes(new[] { 0, 15, 30, 45 }[random.Next(4)]);
                })
                .OrderBy(d => d)
                .ToList();

            foreach (var playedAt in doubleDates)
            {
                var fourPlayers = activePlayers.OrderBy(_ => random.Next()).Take(4).ToList();
                var teamA1 = fourPlayers[0];
                var teamA2 = fourPlayers[1];
                var teamB1 = fourPlayers[2];
                var teamB2 = fourPlayers[3];

                var teamASkill = (skillById[teamA1.Id] + skillById[teamA2.Id]) / 2.0;
                var teamBSkill = (skillById[teamB1.Id] + skillById[teamB2.Id]) / 2.0;
                var expectedA = 1.0 / (1.0 + Math.Pow(10, (teamBSkill - teamASkill) / 400.0));
                var teamAWins = random.NextDouble() < expectedA;

                var (loserSets, winnerSets) = ResultModes[random.Next(ResultModes.Length)];
                var teamASets = teamAWins ? winnerSets : loserSets;
                var teamBSets = teamAWins ? loserSets : winnerSets;

                var winningTeam = ValidationService.ComputeWinningTeam(
                    teamA1.Id, teamA2.Id, teamB1.Id, teamB2.Id, teamASets, teamBSets);

                doubleMatches.Add(new DoubleMatch
                {
                    Id = Guid.NewGuid(),
                    PlayedAt = playedAt,
                    TeamAPlayer1Id = teamA1.Id,
                    TeamAPlayer2Id = teamA2.Id,
                    TeamBPlayer1Id = teamB1.Id,
                    TeamBPlayer2Id = teamB2.Id,
                    TeamASets = teamASets,
                    TeamBSets = teamBSets,
                    WinningTeam = winningTeam,
                    Notes = string.Empty,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        return (players, matches, doubleMatches);
    }
}
