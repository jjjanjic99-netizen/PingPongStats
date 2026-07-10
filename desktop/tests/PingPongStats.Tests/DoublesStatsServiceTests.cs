using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class DoublesStatsServiceTests
{
    private static DoubleMatch M(
        DateTime playedAt, Guid a1, Guid a2, Guid b1, Guid b2, int aSets, int bSets)
    {
        var winningTeam = ValidationService.ComputeWinningTeam(a1, a2, b1, b2, aSets, bSets);
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
            WinningTeam = winningTeam,
        };
    }

    [Fact]
    public void GetPairingRankings_AggregatesRegardlessOfSideOrOrder()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        var matches = new List<DoubleMatch>
        {
            // p1+p2 win as team A
            M(new DateTime(2026, 1, 1), p1, p2, p3, p4, 3, 0),
            // p1+p2 win as team B this time (order within pair swapped too)
            M(new DateTime(2026, 1, 2), p3, p4, p2, p1, 1, 3),
            // p1+p2 lose
            M(new DateTime(2026, 1, 3), p1, p2, p3, p4, 0, 3),
        };

        var rankings = DoublesStatsService.GetPairingRankings(matches);

        var p1p2 = rankings.Single(r =>
            (r.Player1Id == p1 && r.Player2Id == p2) || (r.Player1Id == p2 && r.Player2Id == p1));

        Assert.Equal(3, p1p2.Played);
        Assert.Equal(2, p1p2.Wins);
        Assert.Equal(1, p1p2.Losses);
        Assert.Equal(2.0 / 3 * 100, p1p2.WinRatePct, precision: 5);
    }

    [Fact]
    public void GetPairingRankings_OrdersByWinRateDescending()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var p5 = Guid.NewGuid();
        var p6 = Guid.NewGuid();

        var matches = new List<DoubleMatch>
        {
            M(new DateTime(2026, 1, 1), p1, p2, p3, p4, 3, 0), // p1+p2 win (1/1 so far)
            M(new DateTime(2026, 1, 2), p1, p2, p3, p4, 0, 3), // p1+p2 lose (1/2 = 50%)
            M(new DateTime(2026, 1, 3), p5, p6, p3, p4, 3, 0), // p5+p6 win (1/1 = 100%)
        };

        var rankings = DoublesStatsService.GetPairingRankings(matches);

        Assert.True(rankings[0].Player1Id == p5 || rankings[0].Player1Id == p6);
        Assert.Equal(100, rankings[0].WinRatePct);
    }

    [Fact]
    public void GetPlayerDoublesRecord_CountsWinsAcrossBothTeamSides()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        var matches = new List<DoubleMatch>
        {
            M(new DateTime(2026, 1, 1), p1, p2, p3, p4, 3, 0), // p1 on team A, wins
            M(new DateTime(2026, 1, 2), p3, p4, p1, p2, 3, 0), // p1 on team B, loses
        };

        var record = DoublesStatsService.GetPlayerDoublesRecord(matches, p1);

        Assert.Equal(2, record.Played);
        Assert.Equal(1, record.Wins);
        Assert.Equal(1, record.Losses);
    }

    [Fact]
    public void IsLowSampleSize_FlagsFewerThanThreeGames()
    {
        Assert.True(DoublesStatsService.IsLowSampleSize(2));
        Assert.False(DoublesStatsService.IsLowSampleSize(3));
    }
}
