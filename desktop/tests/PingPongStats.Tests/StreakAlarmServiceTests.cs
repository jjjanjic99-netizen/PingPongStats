using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class StreakAlarmServiceTests
{
    [Fact]
    public void GetCurrentCombinedWinStreak_ReturnsZeroForPlayerWithNoGames()
    {
        var playerId = Guid.NewGuid();
        var streak = StreakAlarmService.GetCurrentCombinedWinStreak(new List<Match>(), new List<DoubleMatch>(), playerId);
        Assert.Equal(0, streak);
    }

    [Fact]
    public void GetCurrentCombinedWinStreak_CountsExactlyThreeConsecutiveWins()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-3), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-1), p1, p2, 3, 0),
        };

        var streak = StreakAlarmService.GetCurrentCombinedWinStreak(matches, new List<DoubleMatch>(), p1);

        Assert.Equal(3, streak);
    }

    [Fact]
    public void GetCurrentCombinedWinStreak_BrokenByADoublesLoss()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);

        // Oldest to newest: singles loss, doubles loss, singles win, singles win.
        // The current streak should only count the last two wins - the doubles
        // loss right before them ends anything further back.
        var matches = new List<Match>
        {
            M(now.AddDays(-10), p1, p2, 0, 3),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-1), p1, p2, 3, 0),
        };
        var doubleMatches = new List<DoubleMatch>
        {
            DM(now.AddDays(-3), p1, p3, p2, p4, 0, 3), // p1 on losing team
        };

        var streak = StreakAlarmService.GetCurrentCombinedWinStreak(matches, doubleMatches, p1);

        Assert.Equal(2, streak);
    }

    [Fact]
    public void GetCurrentCombinedWinStreak_EndedByTheMostRecentLoss()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(now.AddDays(-3), p1, p2, 3, 0),
            M(now.AddDays(-2), p1, p2, 3, 0),
            M(now.AddDays(-1), p1, p2, 0, 3), // most recent game is a loss
        };

        var streak = StreakAlarmService.GetCurrentCombinedWinStreak(matches, new List<DoubleMatch>(), p1);

        Assert.Equal(0, streak);
    }

    [Fact]
    public void GetPlayersOnStreak_ExcludesPlayersBelowThreshold_AndSortsDescending()
    {
        var players = new List<Player>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "OnFire" },
            new() { Id = Guid.NewGuid(), DisplayName = "JustQualifies" },
            new() { Id = Guid.NewGuid(), DisplayName = "TooShort" },
        };
        var onFire = players[0].Id;
        var justQualifies = players[1].Id;
        var tooShort = players[2].Id;
        var opponent = Guid.NewGuid();
        var now = new DateTime(2026, 7, 15);

        var matches = new List<Match>
        {
            M(now.AddDays(-5), onFire, opponent, 3, 0),
            M(now.AddDays(-4), onFire, opponent, 3, 0),
            M(now.AddDays(-3), onFire, opponent, 3, 0),
            M(now.AddDays(-2), onFire, opponent, 3, 0),
            M(now.AddDays(-1), onFire, opponent, 3, 0),

            M(now.AddDays(-3), justQualifies, opponent, 3, 0),
            M(now.AddDays(-2), justQualifies, opponent, 3, 0),
            M(now.AddDays(-1), justQualifies, opponent, 3, 0),

            M(now.AddDays(-2), tooShort, opponent, 3, 0),
            M(now.AddDays(-1), tooShort, opponent, 3, 0),
        };

        var result = StreakAlarmService.GetPlayersOnStreak(players, matches, new List<DoubleMatch>());

        Assert.Equal(2, result.Count);
        Assert.Equal(onFire, result[0].PlayerId);
        Assert.Equal(5, result[0].StreakLength);
        Assert.True(result[0].StreakLength >= StreakAlarmService.FireStreakThreshold);
        Assert.Equal(justQualifies, result[1].PlayerId);
        Assert.Equal(3, result[1].StreakLength);
        Assert.DoesNotContain(result, e => e.PlayerId == tooShort);
    }
}
