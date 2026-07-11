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

    [Fact]
    public void GetLongestLossStreak_FindsLongestHistoricalRun()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3), // L
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0), // W - breaks streak
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3), // L
            M(new DateTime(2026, 1, 4), p1, p2, 0, 3), // L
            M(new DateTime(2026, 1, 5), p1, p2, 0, 3), // L (longest run: 3)
            M(new DateTime(2026, 1, 6), p1, p2, 3, 0), // W
        };

        Assert.Equal(3, StatsService.GetLongestLossStreak(matches, p1));
    }

    [Fact]
    public void GetLongestLossStreak_ReturnsZeroForPlayerWithoutMatches()
    {
        Assert.Equal(0, StatsService.GetLongestLossStreak(new List<Match>(), Guid.NewGuid()));
    }

    [Fact]
    public void GetSetDifference_SumsSetsWonMinusSetsLostAcrossBothSides()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 1), // +2 as side A
            M(new DateTime(2026, 1, 2), p2, p1, 3, 2), // p1 won 2, lost 3 as side B => -1
        };

        Assert.Equal(1, StatsService.GetSetDifference(matches, p1));
    }

    [Fact]
    public void GetSetDifference_ReturnsZeroForPlayerWithoutMatches()
    {
        Assert.Equal(0, StatsService.GetSetDifference(new List<Match>(), Guid.NewGuid()));
    }

    [Fact]
    public void GetNemesis_ReturnsNullWhenNoOpponentMeetsMinimum()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3), // only 2 games vs p2
        };

        Assert.Null(StatsService.GetNemesis(matches, p1));
    }

    [Fact]
    public void GetNemesis_ReturnsNullForPlayerWithoutMatches()
    {
        Assert.Null(StatsService.GetNemesis(new List<Match>(), Guid.NewGuid()));
    }

    [Fact]
    public void GetNemesis_QualifiesAtExactlyThreeGames()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3), // exactly 3 games vs p2, all losses
        };

        var nemesis = StatsService.GetNemesis(matches, p1);

        Assert.NotNull(nemesis);
        Assert.Equal(p2, nemesis!.OpponentId);
        Assert.Equal(0, nemesis.WinRatePct);
    }

    [Fact]
    public void GetNemesis_PicksWorstWinRateOpponent()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid(); // 0% win rate vs p1
        var p3 = Guid.NewGuid(); // 100% win rate vs p1
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 5), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 6), p1, p3, 3, 0),
        };

        var nemesis = StatsService.GetNemesis(matches, p1);

        Assert.Equal(p2, nemesis!.OpponentId);
    }

    [Fact]
    public void GetNemesis_TiedWinRateBreaksTowardMoreGamesPlayed()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid(); // 0/3 = 0%, 3 games
        var p3 = Guid.NewGuid(); // 0/4 = 0%, 4 games - same rate, more games => nemesis
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 4), p1, p3, 0, 3),
            M(new DateTime(2026, 1, 5), p1, p3, 0, 3),
            M(new DateTime(2026, 1, 6), p1, p3, 0, 3),
            M(new DateTime(2026, 1, 7), p1, p3, 0, 3),
        };

        var nemesis = StatsService.GetNemesis(matches, p1);

        Assert.Equal(p3, nemesis!.OpponentId);
    }

    [Fact]
    public void GetFavoriteOpponent_ReturnsNullWhenNoOpponentMeetsMinimum()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0),
        };

        Assert.Null(StatsService.GetFavoriteOpponent(matches, p1));
    }

    [Fact]
    public void GetFavoriteOpponent_PicksBestWinRateOpponent()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid(); // 0% win rate vs p1
        var p3 = Guid.NewGuid(); // 100% win rate vs p1
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 2), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 5), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 6), p1, p3, 3, 0),
        };

        var favorite = StatsService.GetFavoriteOpponent(matches, p1);

        Assert.Equal(p3, favorite!.OpponentId);
    }

    [Fact]
    public void GetFavoriteOpponent_TiedWinRateBreaksTowardMoreGamesPlayed()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid(); // 100% over 3 games
        var p3 = Guid.NewGuid(); // 100% over 4 games - same rate, more games => favorite
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 3), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 5), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 6), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 7), p1, p3, 3, 0),
        };

        var favorite = StatsService.GetFavoriteOpponent(matches, p1);

        Assert.Equal(p3, favorite!.OpponentId);
    }

    [Fact]
    public void GetOpponentWinRates_ReturnsEmptyForPlayerWithoutMatches()
    {
        Assert.Empty(StatsService.GetOpponentWinRates(new List<Match>(), Guid.NewGuid()));
    }

    [Fact]
    public void GetMostFrequentOpponents_OrdersByGamesPlayedDescending()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid(); // 3 games
        var p3 = Guid.NewGuid(); // 1 game
        var p4 = Guid.NewGuid(); // 2 games
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 2), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 3), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 4), p1, p3, 3, 0),
            M(new DateTime(2026, 1, 5), p1, p4, 3, 0),
            M(new DateTime(2026, 1, 6), p1, p4, 3, 0),
        };

        var result = StatsService.GetMostFrequentOpponents(matches, p1, topN: 3);

        Assert.Equal(new[] { p2, p4, p3 }, result.Select(o => o.OpponentId));
    }

    [Fact]
    public void GetMostFrequentOpponents_RespectsTopNLimit()
    {
        var p1 = Guid.NewGuid();
        var opponents = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var matches = opponents.Select((opp, i) => M(new DateTime(2026, 1, 1).AddDays(i), p1, opp, 3, 0)).ToList();

        var result = StatsService.GetMostFrequentOpponents(matches, p1, topN: 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetMostFrequentOpponents_ReturnsEmptyForPlayerWithoutMatches()
    {
        Assert.Empty(StatsService.GetMostFrequentOpponents(new List<Match>(), Guid.NewGuid()));
    }
}
