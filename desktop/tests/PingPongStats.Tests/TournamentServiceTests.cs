using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class TournamentServiceTests
{
    private static Player P(string name) => new() { Id = Guid.NewGuid(), DisplayName = name, IsActive = true };

    [Fact]
    public void BuildSinglesEntrants_SeedsStrongestPlayerFirst()
    {
        var weak = P("Weak");
        var strong = P("Strong");
        var elo = new Dictionary<Guid, double> { [weak.Id] = 900, [strong.Id] = 1300 };

        var entrants = TournamentService.BuildSinglesEntrants(new[] { weak, strong }, elo);

        Assert.Equal(1, entrants.Single(e => e.Player1Id == strong.Id).Seed);
        Assert.Equal(2, entrants.Single(e => e.Player1Id == weak.Id).Seed);
    }

    [Fact]
    public void BuildSinglesEntrants_BreaksTiesByDisplayNameForDeterminism()
    {
        var a = P("Anna");
        var b = P("Ben");
        var elo = new Dictionary<Guid, double> { [a.Id] = 1000, [b.Id] = 1000 };

        var entrants = TournamentService.BuildSinglesEntrants(new[] { b, a }, elo);

        Assert.Equal(1, entrants.Single(e => e.Player1Id == a.Id).Seed);
        Assert.Equal(2, entrants.Single(e => e.Player1Id == b.Id).Seed);
    }

    [Fact]
    public void BuildSinglesEntrants_UsesDefaultInitialRatingForUnknownPlayers()
    {
        var known = P("Known");
        var unknown = P("Unknown");
        var elo = new Dictionary<Guid, double> { [known.Id] = 1200 };

        var entrants = TournamentService.BuildSinglesEntrants(new[] { known, unknown }, elo);

        Assert.Equal(1, entrants.Single(e => e.Player1Id == known.Id).Seed);
    }

    [Fact]
    public void BuildDoublesEntrants_SeedsByAverageTeamElo()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var elo = new Dictionary<Guid, double> { [a1] = 1400, [a2] = 1400, [b1] = 900, [b2] = 900 };
        var names = new Dictionary<Guid, string> { [a1] = "A1", [a2] = "A2", [b1] = "B1", [b2] = "B2" };
        var teams = new List<(Guid, Guid)> { (b1, b2), (a1, a2) };

        var entrants = TournamentService.BuildDoublesEntrants(teams, elo, names);

        var strongTeam = entrants.Single(e => e.Player1Id == a1 || e.Player2Id == a1);
        Assert.Equal(1, strongTeam.Seed);
    }

    [Fact]
    public void DrawRandomTeams_RejectsOddPlayerCount()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        Assert.Throws<ValidationException>(() => TournamentService.DrawRandomTeams(ids));
    }

    [Fact]
    public void DrawRandomTeams_PairsEveryPlayerExactlyOnce()
    {
        var ids = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();

        var teams = TournamentService.DrawRandomTeams(ids);

        Assert.Equal(4, teams.Count);
        var allPaired = teams.SelectMany(t => new[] { t.Player1Id, t.Player2Id }).ToList();
        Assert.Equal(ids.Count, allPaired.Distinct().Count());
        Assert.Equal(ids.OrderBy(x => x), allPaired.OrderBy(x => x));
    }

    [Fact]
    public void DrawRandomTeams_UsesProvidedShuffleForDeterminism()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var teams = TournamentService.DrawRandomTeams(ids, items => items); // identity "shuffle"

        Assert.Equal((ids[0], ids[1]), teams[0]);
        Assert.Equal((ids[2], ids[3]), teams[1]);
    }
}
