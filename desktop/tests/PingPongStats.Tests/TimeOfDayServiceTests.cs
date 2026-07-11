using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class TimeOfDayServiceTests
{
    [Theory]
    [InlineData(0, TimeOfDayBlock.BeforeTen)]
    [InlineData(9, TimeOfDayBlock.BeforeTen)]
    [InlineData(10, TimeOfDayBlock.TenToTwelve)]
    [InlineData(11, TimeOfDayBlock.TenToTwelve)]
    [InlineData(12, TimeOfDayBlock.TwelveToFourteen)]
    [InlineData(13, TimeOfDayBlock.TwelveToFourteen)]
    [InlineData(14, TimeOfDayBlock.FourteenToSeventeen)]
    [InlineData(16, TimeOfDayBlock.FourteenToSeventeen)]
    [InlineData(17, TimeOfDayBlock.AfterSeventeen)]
    [InlineData(23, TimeOfDayBlock.AfterSeventeen)]
    public void GetBlock_MapsHourToCorrectBlock(int hour, TimeOfDayBlock expected)
    {
        var playedAt = new DateTime(2026, 1, 1, hour, 0, 0);
        Assert.Equal(expected, TimeOfDayService.GetBlock(playedAt));
    }

    [Fact]
    public void GetStatsByBlock_ReturnsAllFiveBlocksEvenWithoutMatches()
    {
        var stats = TimeOfDayService.GetStatsByBlock(new List<Match>(), Guid.NewGuid());
        Assert.Equal(5, stats.Count);
        Assert.All(stats, s => Assert.Equal(0, s.Played));
    }

    [Fact]
    public void GetStatsByBlock_ComputesWinRatePerBlock()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1, 9, 0, 0), p1, p2, 3, 0), // BeforeTen, win
            M(new DateTime(2026, 1, 2, 9, 0, 0), p1, p2, 0, 3), // BeforeTen, loss
            M(new DateTime(2026, 1, 3, 15, 0, 0), p1, p2, 3, 0), // FourteenToSeventeen, win
        };

        var stats = TimeOfDayService.GetStatsByBlock(matches, p1);

        var beforeTen = stats.Single(s => s.Block == TimeOfDayBlock.BeforeTen);
        Assert.Equal(2, beforeTen.Played);
        Assert.Equal(50, beforeTen.WinRatePct, precision: 5);

        var afternoon = stats.Single(s => s.Block == TimeOfDayBlock.FourteenToSeventeen);
        Assert.Equal(1, afternoon.Played);
        Assert.Equal(100, afternoon.WinRatePct, precision: 5);
    }

    [Fact]
    public void GetBestBlock_RequiresMinimumGamesInThatBlock()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        // Only 4 games before 10, all wins - one short of the 5-game minimum for the callout.
        var matches = Enumerable.Range(0, 4)
            .Select(i => M(new DateTime(2026, 1, 1 + i, 9, 0, 0), p1, p2, 3, 0))
            .ToList();

        var stats = TimeOfDayService.GetStatsByBlock(matches, p1);

        Assert.Null(TimeOfDayService.GetBestBlock(stats));
    }

    [Fact]
    public void GetBestBlock_QualifiesAtExactlyFiveGames()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = Enumerable.Range(0, 5)
            .Select(i => M(new DateTime(2026, 1, 1 + i, 9, 0, 0), p1, p2, 3, 0))
            .ToList();

        var stats = TimeOfDayService.GetStatsByBlock(matches, p1);
        var best = TimeOfDayService.GetBestBlock(stats);

        Assert.NotNull(best);
        Assert.Equal(TimeOfDayBlock.BeforeTen, best!.Block);
    }

    [Fact]
    public void GetBestBlock_PicksHighestWinRateAmongQualifyingBlocks()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>();
        // Morning: 5 games, 100% win rate.
        matches.AddRange(Enumerable.Range(0, 5).Select(i => M(new DateTime(2026, 1, 1 + i, 9, 0, 0), p1, p2, 3, 0)));
        // Evening: 5 games, 60% win rate.
        matches.AddRange(Enumerable.Range(0, 3).Select(i => M(new DateTime(2026, 2, 1 + i, 18, 0, 0), p1, p2, 3, 0)));
        matches.AddRange(Enumerable.Range(0, 2).Select(i => M(new DateTime(2026, 2, 10 + i, 18, 0, 0), p1, p2, 0, 3)));

        var stats = TimeOfDayService.GetStatsByBlock(matches, p1);
        var best = TimeOfDayService.GetBestBlock(stats);

        Assert.NotNull(best);
        Assert.Equal(TimeOfDayBlock.BeforeTen, best!.Block);
    }

    [Fact]
    public void GetBestBlock_ReturnsNullWhenNoBlockQualifies()
    {
        var stats = TimeOfDayService.GetStatsByBlock(new List<Match>(), Guid.NewGuid());
        Assert.Null(TimeOfDayService.GetBestBlock(stats));
    }
}
