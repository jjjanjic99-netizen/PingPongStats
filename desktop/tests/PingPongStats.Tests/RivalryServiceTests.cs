using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class RivalryServiceTests
{
    [Fact]
    public void FindRivalryOfTheMonth_ReturnsNullWhenNoPairMeetsMinimum()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0), // only 2 games
        };

        Assert.Null(RivalryService.FindRivalryOfTheMonth(matches, now));
    }

    [Fact]
    public void FindRivalryOfTheMonth_QualifiesAtExactlyThreeGames()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-3), p2, p1, 3, 0),
        };

        var rivalry = RivalryService.FindRivalryOfTheMonth(matches, now);

        Assert.NotNull(rivalry);
        Assert.Equal(3, rivalry!.TotalGames);
    }

    [Fact]
    public void FindRivalryOfTheMonth_IgnoresMatchesOutsideThirtyDayWindow()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-40), p1, p2, 3, 0), // outside window
        };

        Assert.Null(RivalryService.FindRivalryOfTheMonth(matches, now));
    }

    [Fact]
    public void FindRivalryOfTheMonth_PicksPairWithMostGames()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-3), p1, p2, 3, 0),
            M(now.AddDays(-1), p3, p4, 3, 0),
            M(now.AddDays(-2), p3, p4, 3, 0),
            M(now.AddDays(-3), p3, p4, 3, 0),
            M(now.AddDays(-4), p3, p4, 3, 0),
        };

        var rivalry = RivalryService.FindRivalryOfTheMonth(matches, now);

        Assert.NotNull(rivalry);
        Assert.Equal(4, rivalry!.TotalGames);
        Assert.True(
            (rivalry.Player1Id == p3 && rivalry.Player2Id == p4) ||
            (rivalry.Player1Id == p4 && rivalry.Player2Id == p3));
    }

    [Fact]
    public void FindRivalryOfTheMonth_TiedGameCountBreaksTowardCloserRecord()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        // p1 vs p2: 3 games, lopsided 3-0
        var matches = new List<Match>
        {
            M(now.AddDays(-1), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-3), p1, p2, 3, 0),
            // p3 vs p4: 3 games, close 2-1
            M(now.AddDays(-1), p3, p4, 3, 0),
            M(now.AddDays(-2), p3, p4, 3, 0),
            M(now.AddDays(-3), p4, p3, 3, 0),
        };

        var rivalry = RivalryService.FindRivalryOfTheMonth(matches, now);

        Assert.NotNull(rivalry);
        Assert.True(
            (rivalry!.Player1Id == p3 && rivalry.Player2Id == p4) ||
            (rivalry.Player1Id == p4 && rivalry.Player2Id == p3));
    }

    [Fact]
    public void FindRivalryOfTheMonth_ReturnsNullForEmptyMatchList()
    {
        Assert.Null(RivalryService.FindRivalryOfTheMonth(new List<Match>(), new DateTime(2026, 7, 15)));
    }
}
