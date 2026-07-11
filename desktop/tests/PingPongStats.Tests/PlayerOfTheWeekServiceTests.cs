using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class PlayerOfTheWeekServiceTests
{
    private static DoubleMatch MD(DateTime playedAt, Guid a1, Guid a2, Guid b1, Guid b2, int aSets, int bSets)
    {
        return new DoubleMatch
        {
            Id = Guid.NewGuid(),
            PlayedAt = playedAt,
            TeamAPlayer1Id = a1,
            TeamAPlayer2Id = a2,
            TeamBPlayer1Id = b1,
            TeamBPlayer2Id = b2,
            TeamASets = aSets,
            TeamBSets = bSets,
            WinningTeam = ValidationService.ComputeWinningTeam(a1, a2, b1, b2, aSets, bSets),
        };
    }

    [Fact]
    public void Compute_ReturnsNullWhenNoOneQualifies()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0), // only 2 games in window
        };

        var result = PlayerOfTheWeekService.Compute(
            matches, new List<DoubleMatch>(), new[] { p1, p2 },
            new Dictionary<Guid, double>(), now);

        Assert.Null(result);
    }

    [Fact]
    public void Compute_IgnoresMatchesOutsideTheWindow()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-3), p1, p2, 3, 0),
            M(now.AddDays(-10), p1, p2, 3, 0), // outside 7-day window
        };

        var result = PlayerOfTheWeekService.Compute(
            matches, new List<DoubleMatch>(), new[] { p1, p2 },
            new Dictionary<Guid, double>(), now);

        Assert.NotNull(result);
        Assert.Equal(p1, result!.PlayerId);
        Assert.Equal(3, result.Played);
    }

    [Fact]
    public void Compute_UsesExactScoreFormula()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 1), // p1 win, set diff +2
            M(now.AddDays(-2), p1, p2, 3, 0), // p1 win, set diff +3
            M(now.AddDays(-3), p2, p1, 3, 2), // p1 loss, set diff -1
        };

        var result = PlayerOfTheWeekService.Compute(
            matches, new List<DoubleMatch>(), new[] { p1, p2 },
            new Dictionary<Guid, double>(), now);

        Assert.NotNull(result);
        Assert.Equal(p1, result!.PlayerId);
        // Wins=2, Losses=1, SetDifference=+2+3-1=4 => Score = 2*2 - 1 + 4*0.5 = 5
        Assert.Equal(5.0, result.Score, precision: 6);
    }

    [Fact]
    public void Compute_DoublesMatchCountsForBothTeamMembers()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var doubles = new List<DoubleMatch>
        {
            MD(now.AddDays(-1), p1, p2, p3, p4, 3, 1),
            MD(now.AddDays(-2), p1, p2, p3, p4, 3, 0),
            MD(now.AddDays(-3), p1, p2, p3, p4, 3, 2),
        };

        var result = PlayerOfTheWeekService.Compute(
            new List<Match>(), doubles, new[] { p1, p2, p3, p4 },
            new Dictionary<Guid, double>(), now);

        Assert.NotNull(result);
        Assert.Contains(result!.PlayerId, new[] { p1, p2 });
        Assert.Equal(3, result.Wins);
        Assert.Equal(0, result.Losses);
    }

    [Fact]
    public void Compute_RequiresMinimumThreeGamesCombinedSinglesAndDoubles()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var singles = new List<Match> { M(now.AddDays(-1), p1, p2, 3, 0) };
        var doubles = new List<DoubleMatch>
        {
            MD(now.AddDays(-2), p1, p2, p3, p4, 3, 0),
            MD(now.AddDays(-3), p1, p2, p3, p4, 3, 0),
        };

        var result = PlayerOfTheWeekService.Compute(
            singles, doubles, new[] { p1, p2, p3, p4 },
            new Dictionary<Guid, double>(), now);

        Assert.NotNull(result);
        Assert.Equal(p1, result!.PlayerId); // p1: 1 singles + 2 doubles = 3 games, qualifies
        Assert.Equal(3, result.Played);
    }

    [Fact]
    public void Compute_TiebreaksByWinRateThenGamesThenElo()
    {
        var p1 = Guid.NewGuid(); // 3-0 (100% win rate), Score = 3*2 - 0 + 9*0.5 = 10.5
        var p2 = Guid.NewGuid(); // 4-1 (80% win rate), Score = 4*2 - 1 + 7*0.5 = 10.5 (tied score, more games)
        var opponent = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);

        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, opponent, 3, 0),
            M(now.AddDays(-2), p1, opponent, 3, 0),
            M(now.AddDays(-3), p1, opponent, 3, 0),
            M(now.AddDays(-1), p2, opponent, 3, 1),
            M(now.AddDays(-2), p2, opponent, 3, 1),
            M(now.AddDays(-3), p2, opponent, 3, 1),
            M(now.AddDays(-4), p2, opponent, 3, 1),
            M(now.AddDays(-5), opponent, p2, 3, 2),
        };

        var result = PlayerOfTheWeekService.Compute(
            matches, new List<DoubleMatch>(), new[] { p1, p2, opponent },
            new Dictionary<Guid, double>(), now);

        Assert.NotNull(result);
        Assert.Equal(10.5, result!.Score, precision: 6);
        // Scores tie: p1's 100% win rate beats p2's 80%, despite p2 having more games
        Assert.Equal(p1, result.PlayerId);
    }

    [Fact]
    public void Compute_TiebreaksByEloWhenScoreAndWinRateAndGamesAllTie()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var opponent = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, opponent, 3, 0),
            M(now.AddDays(-2), p1, opponent, 3, 0),
            M(now.AddDays(-3), p1, opponent, 3, 0),
            M(now.AddDays(-1), p2, opponent, 3, 0),
            M(now.AddDays(-2), p2, opponent, 3, 0),
            M(now.AddDays(-3), p2, opponent, 3, 0),
        };
        var eloRatings = new Dictionary<Guid, double> { [p1] = 1200, [p2] = 1500 };

        var result = PlayerOfTheWeekService.Compute(
            matches, new List<DoubleMatch>(), new[] { p1, p2, opponent }, eloRatings, now);

        Assert.NotNull(result);
        Assert.Equal(p2, result!.PlayerId);
    }
}
