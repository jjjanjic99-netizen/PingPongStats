using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class StatsServiceTests
{
    [Fact]
    public void GetOverallRecord_ComputesWinsPlayedAndRate()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // win
            M(new DateTime(2026, 1, 2), p1, p2, 3, 1), // win
            M(new DateTime(2026, 1, 3), p2, p1, 3, 2), // loss
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0), // win
        };

        var record = StatsService.GetOverallRecord(matches, p1);

        Assert.Equal(4, record.Played);
        Assert.Equal(3, record.Wins);
        Assert.Equal(1, record.Losses);
        Assert.Equal(75, record.WinRatePct, precision: 5);
    }

    [Fact]
    public void GetOverallRecord_ReturnsZeroForPlayerWithoutMatches()
    {
        var record = StatsService.GetOverallRecord(new List<Match>(), Guid.NewGuid());
        Assert.Equal(0, record.Played);
        Assert.Equal(0, record.WinRatePct);
    }

    [Fact]
    public void GetWinRateLastNDays_OnlyCountsMatchesWithinWindow()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var reference = new DateTime(2026, 7, 10, 12, 0, 0);
        var matches = new List<Match>
        {
            M(reference.AddDays(-5), p1, p2, 3, 0), // within 30 days, win
            M(reference.AddDays(-25), p1, p2, 3, 0), // within 30 days, win
            M(reference.AddDays(-70), p1, p2, 0, 3), // outside window
        };

        var record = StatsService.GetWinRateLastNDays(matches, p1, 30, reference);

        Assert.Equal(2, record.Played);
        Assert.Equal(2, record.Wins);
        Assert.Equal(100, record.WinRatePct);
    }

    [Fact]
    public void GetCurrentStreak_CountsConsecutiveWinsFromMostRecentMatch()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3), // loss
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0), // win
            M(new DateTime(2026, 1, 3), p1, p2, 3, 1), // win
            M(new DateTime(2026, 1, 4), p1, p2, 3, 2), // win (most recent)
        };

        var streak = StatsService.GetCurrentStreak(matches, p1);

        Assert.Equal(StreakType.Win, streak.Type);
        Assert.Equal(3, streak.Length);
    }

    [Fact]
    public void GetCurrentStreak_ReturnsLossStreakWhenMostRecentWasLost()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // win
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3), // loss (most recent)
        };

        var streak = StatsService.GetCurrentStreak(matches, p1);

        Assert.Equal(StreakType.Loss, streak.Type);
        Assert.Equal(1, streak.Length);
    }

    [Fact]
    public void GetCurrentStreak_ReturnsNoneForPlayerWithoutMatches()
    {
        var streak = StatsService.GetCurrentStreak(new List<Match>(), Guid.NewGuid());
        Assert.Equal(StreakType.None, streak.Type);
        Assert.Equal(0, streak.Length);
    }

    [Fact]
    public void GetLongestWinStreak_FindsLongestHistoricalRun()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // W
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0), // W
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3), // L - breaks streak
            M(new DateTime(2026, 1, 4), p1, p2, 3, 0), // W
            M(new DateTime(2026, 1, 5), p1, p2, 3, 0), // W
            M(new DateTime(2026, 1, 6), p1, p2, 3, 0), // W (longest run: 3)
            M(new DateTime(2026, 1, 7), p1, p2, 0, 3), // L
        };

        Assert.Equal(3, StatsService.GetLongestWinStreak(matches, p1));
    }

    [Fact]
    public void GetHeadToHead_AggregatesRegardlessOfSide()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // p1 win
            M(new DateTime(2026, 1, 2), p2, p1, 3, 1), // p1 loss
            M(new DateTime(2026, 1, 3), p1, p2, 3, 2), // p1 win
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0), // irrelevant
        };

        var h2h = StatsService.GetHeadToHead(matches, p1, p2);

        Assert.Equal(3, h2h.TotalGames);
        Assert.Equal(2, h2h.PlayerAWins);
        Assert.Equal(1, h2h.PlayerBWins);
        Assert.Equal(2.0 / 3 * 100, h2h.PlayerAWinRatePct, precision: 5);
    }

    [Fact]
    public void GetAverageSetsWonPerMatch_AveragesOwnSetsAcrossMatches()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 1), // p1 won 3 sets
            M(new DateTime(2026, 1, 2), p2, p1, 3, 2), // p1 won 2 sets
        };

        Assert.Equal(2.5, StatsService.GetAverageSetsWonPerMatch(matches, p1), precision: 5);
    }

    [Fact]
    public void GetRecentForm_ReturnsLastNResultsNewestFirst()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // W
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3), // L
            M(new DateTime(2026, 1, 3), p1, p2, 3, 0), // W
        };

        var form = StatsService.GetRecentForm(matches, p1, 2);

        Assert.Equal(new[] { 'W', 'L' }, form.Select(f => f.Result));
    }

    [Fact]
    public void IsLowSampleSize_FlagsFewerThanThreeGames()
    {
        Assert.True(StatsService.IsLowSampleSize(0));
        Assert.True(StatsService.IsLowSampleSize(2));
        Assert.False(StatsService.IsLowSampleSize(3));
    }
}
