using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class HallOfFameServiceTests
{
    private static Player P(string name) => new() { Id = Guid.NewGuid(), DisplayName = name, IsActive = true };

    [Fact]
    public void GetCompletedSeasons_OnlyReturnsSeasonsWhoseEndDateHasPassed()
    {
        var now = new DateTime(2026, 7, 15);
        var past = new Season { Id = Guid.NewGuid(), Name = "Past", StartDate = now.AddMonths(-3), EndDate = now.AddDays(-1) };
        var future = new Season { Id = Guid.NewGuid(), Name = "Future", StartDate = now, EndDate = now.AddMonths(3) };

        var completed = HallOfFameService.GetCompletedSeasons(new[] { past, future }, now);

        Assert.Single(completed);
        Assert.Equal(past.Id, completed[0].Id);
    }

    [Fact]
    public void BuildRecordBoard_ReturnsAllNullsForEmptyDataBase()
    {
        var board = HallOfFameService.BuildRecordBoard(new List<Player>(), new List<Match>(), new List<DoubleMatch>());

        Assert.Null(board.LongestWinStreak);
        Assert.Null(board.HighestElo);
        Assert.Null(board.MostGamesInOneDay);
        Assert.Null(board.BiggestComeback);
        Assert.Null(board.BiggestUpset);
        Assert.Null(board.MostTotalGames);
        Assert.Null(board.BestWinRate);
    }

    [Fact]
    public void LongestWinStreak_TiedLengthGoesToTheEarlierAchievingPlayer()
    {
        var earlier = P("Earlier");
        var later = P("Later");
        var opponent = P("Opponent");
        var now = new DateTime(2026, 7, 15);

        // Earlier reaches a 3-win streak first (finishing 2026-01-03).
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), earlier.Id, opponent.Id, 3, 0),
            M(new DateTime(2026, 1, 2), earlier.Id, opponent.Id, 3, 0),
            M(new DateTime(2026, 1, 3), earlier.Id, opponent.Id, 3, 0),

            // Later reaches the same length (3) but finishes later, 2026-02-03.
            M(new DateTime(2026, 2, 1), later.Id, opponent.Id, 3, 0),
            M(new DateTime(2026, 2, 2), later.Id, opponent.Id, 3, 0),
            M(new DateTime(2026, 2, 3), later.Id, opponent.Id, 3, 0),
        };

        var board = HallOfFameService.BuildRecordBoard(new[] { earlier, later, opponent }, matches, new List<DoubleMatch>());

        Assert.NotNull(board.LongestWinStreak);
        Assert.Equal(earlier.Id, board.LongestWinStreak!.PlayerId);
        Assert.Equal(new DateTime(2026, 1, 3), board.LongestWinStreak.AchievedAt);
    }

    [Fact]
    public void HighestElo_PicksThePlayerWithTheHigherPeakRating()
    {
        var strong = P("Strong");
        var weak = P("Weak");
        var now = new DateTime(2026, 7, 15);

        // Strong beats Weak repeatedly, driving Strong's Elo well above 1000.
        var matches = new List<Match>
        {
            M(now.AddDays(-4), strong.Id, weak.Id, 3, 0),
            M(now.AddDays(-3), strong.Id, weak.Id, 3, 0),
            M(now.AddDays(-2), strong.Id, weak.Id, 3, 0),
        };

        var board = HallOfFameService.BuildRecordBoard(new[] { strong, weak }, matches, new List<DoubleMatch>());

        Assert.NotNull(board.HighestElo);
        Assert.Equal(strong.Id, board.HighestElo!.PlayerId);
    }

    [Fact]
    public void MostGamesInOneDay_CountsSinglesAndDoublesTogetherForTheSamePlayer()
    {
        var p1 = P("P1");
        var p2 = P("P2");
        var p3 = P("P3");
        var p4 = P("P4");
        var busyDay = new DateTime(2026, 5, 1);

        var matches = new List<Match>
        {
            M(busyDay, p1.Id, p2.Id, 3, 0),
            M(busyDay, p1.Id, p3.Id, 3, 0), // extra singles game only for p1, so it uniquely leads
        };
        var doubles = new List<DoubleMatch>
        {
            DM(busyDay, p1.Id, p3.Id, p2.Id, p4.Id, 3, 0),
            DM(busyDay, p1.Id, p3.Id, p2.Id, p4.Id, 3, 0),
        };

        var board = HallOfFameService.BuildRecordBoard(new[] { p1, p2, p3, p4 }, matches, doubles);

        Assert.NotNull(board.MostGamesInOneDay);
        Assert.Equal(p1.Id, board.MostGamesInOneDay!.PlayerId); // 2 singles + 2 doubles = 4 games that day
        Assert.Equal(busyDay, board.MostGamesInOneDay.AchievedAt);
    }

    [Fact]
    public void BiggestComeback_ReusesComebackServiceUnchanged()
    {
        var winner = P("Winner");
        var loser = P("Loser");
        var match = new Match
        {
            Id = Guid.NewGuid(),
            PlayedAt = new DateTime(2026, 4, 1),
            PlayerAId = winner.Id,
            PlayerBId = loser.Id,
            PlayerASets = 3,
            PlayerBSets = 2,
            WinnerId = winner.Id,
            SetResults = new List<SetResult>
            {
                new() { SetNumber = 1, PointsA = 5, PointsB = 11 },
                new() { SetNumber = 2, PointsA = 5, PointsB = 11 },
                new() { SetNumber = 3, PointsA = 11, PointsB = 5 },
                new() { SetNumber = 4, PointsA = 11, PointsB = 5 },
                new() { SetNumber = 5, PointsA = 11, PointsB = 5 },
            },
        };

        var board = HallOfFameService.BuildRecordBoard(new[] { winner, loser }, new List<Match> { match }, new List<DoubleMatch>());

        Assert.NotNull(board.BiggestComeback);
        Assert.Equal(winner.Id, board.BiggestComeback!.PlayerId);
        Assert.Equal(new DateTime(2026, 4, 1), board.BiggestComeback.AchievedAt);
    }

    [Fact]
    public void BiggestUpset_PicksTheWinnerWithTheLowestPreMatchWinProbability()
    {
        var strong = P("Strong");
        var weak = P("Weak");
        var now = new DateTime(2026, 7, 1);

        var matches = new List<Match>
        {
            // Strong builds up a big Elo lead over Weak first...
            M(now.AddDays(-10), strong.Id, weak.Id, 3, 0),
            M(now.AddDays(-9), strong.Id, weak.Id, 3, 0),
            M(now.AddDays(-8), strong.Id, weak.Id, 3, 0),
            M(now.AddDays(-7), strong.Id, weak.Id, 3, 0),
            // ...then the underdog Weak pulls off an upset win.
            M(now.AddDays(-1), weak.Id, strong.Id, 3, 0),
        };

        var board = HallOfFameService.BuildRecordBoard(new[] { strong, weak }, matches, new List<DoubleMatch>());

        Assert.NotNull(board.BiggestUpset);
        Assert.Equal(weak.Id, board.BiggestUpset!.PlayerId);
        Assert.Equal(now.AddDays(-1), board.BiggestUpset.AchievedAt);
    }

    [Fact]
    public void MostTotalGames_CombinesSinglesAndDoublesCounts()
    {
        var p1 = P("P1");
        var p2 = P("P2");
        var p3 = P("P3");
        var p4 = P("P4");
        var now = new DateTime(2026, 6, 1);

        var matches = new List<Match>
        {
            M(now.AddDays(-4), p1.Id, p3.Id, 3, 0), // extra singles game only for p1
            M(now.AddDays(-3), p1.Id, p2.Id, 3, 0),
            M(now.AddDays(-2), p1.Id, p2.Id, 3, 0),
        };
        var doubles = new List<DoubleMatch> { DM(now.AddDays(-1), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0) };

        var board = HallOfFameService.BuildRecordBoard(new[] { p1, p2, p3, p4 }, matches, doubles);

        Assert.NotNull(board.MostTotalGames);
        Assert.Equal(p1.Id, board.MostTotalGames!.PlayerId); // 3 singles + 1 doubles = 4, more than p2/p3/p4
    }

    [Fact]
    public void BestWinRate_RequiresAtLeastTwentyGames()
    {
        var qualifies = P("Qualifies");
        var tooFew = P("TooFew");
        var opponent = P("Opponent");
        var now = new DateTime(2026, 1, 1);

        var matches = new List<Match>();
        for (var i = 0; i < 20; i++)
        {
            matches.Add(M(now.AddDays(i), qualifies.Id, opponent.Id, 3, 0));
        }
        matches.Add(M(now.AddDays(100), tooFew.Id, opponent.Id, 3, 0)); // only 1 game - below the 20-game minimum

        var board = HallOfFameService.BuildRecordBoard(new[] { qualifies, tooFew, opponent }, matches, new List<DoubleMatch>());

        Assert.NotNull(board.BestWinRate);
        Assert.Equal(qualifies.Id, board.BestWinRate!.PlayerId);
    }

    [Fact]
    public void BuildSeasonSummary_TotalGamesCombinesSinglesAndDoublesWithinTheSeasonWindow()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var season = new Season { Id = Guid.NewGuid(), Name = "S1", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31) };

        var matches = new List<Match>
        {
            M(new DateTime(2026, 2, 1), p1, p2, 3, 0),
            M(new DateTime(2026, 5, 1), p1, p2, 3, 0), // outside the season window
        };
        var doubles = new List<DoubleMatch> { DM(new DateTime(2026, 2, 15), p1, p3, p2, p4, 3, 0) };

        var summary = HallOfFameService.BuildSeasonSummary(season, matches, doubles);

        Assert.Equal(2, summary.TotalGames);
        Assert.Equal(p1, summary.SinglesTable[0].PlayerId);
    }
}
