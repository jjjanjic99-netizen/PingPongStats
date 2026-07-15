using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class PlayerResultDetailServiceTests
{
    private static Player Player(Guid id, string name) => new() { Id = id, DisplayName = name };

    [Fact]
    public void GetRecentResults_ReturnsOnlyWinsForPlayer_SortedDescendingByDate()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var players = new List<Player> { Player(p1, "Marco"), Player(p2, "Sven") };
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // p1 win
            M(new DateTime(2026, 1, 3), p1, p2, 3, 1), // p1 win (latest)
            M(new DateTime(2026, 1, 2), p2, p1, 3, 2), // p1 loss
        };

        var result = PlayerResultDetailService.GetRecentResults(players, matches, new List<DoubleMatch>(), p1, winsOnly: true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0, result.OverflowCount);
        Assert.Equal(new DateTime(2026, 1, 3), result.Rows[0].PlayedAt);
        Assert.Equal(new DateTime(2026, 1, 1), result.Rows[1].PlayedAt);
        Assert.All(result.Rows, r => Assert.Equal("Sven", r.OpponentLabel));
        Assert.Equal("3:1", result.Rows[0].ScoreLabel);
    }

    [Fact]
    public void GetRecentResults_ReturnsOnlyLossesForPlayer()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var players = new List<Player> { Player(p1, "Marco"), Player(p2, "Sven") };
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0), // p1 win
            M(new DateTime(2026, 1, 2), p2, p1, 3, 2), // p1 loss
        };

        var result = PlayerResultDetailService.GetRecentResults(players, matches, new List<DoubleMatch>(), p1, winsOnly: false);

        Assert.Single(result.Rows);
        Assert.Equal("2:3", result.Rows[0].ScoreLabel);
        Assert.False(result.Rows[0].IsDoubles);
    }

    [Fact]
    public void GetRecentResults_TruncatesToMaxRowsButKeepsTotalCount()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var players = new List<Player> { Player(p1, "Marco"), Player(p2, "Sven") };
        var matches = Enumerable.Range(0, 12)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1, p2, 3, 0))
            .ToList();

        var result = PlayerResultDetailService.GetRecentResults(players, matches, new List<DoubleMatch>(), p1, winsOnly: true, maxRows: 5);

        Assert.Equal(5, result.Rows.Count);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(7, result.OverflowCount);
        Assert.Equal(new DateTime(2026, 1, 12), result.Rows[0].PlayedAt);
    }

    [Fact]
    public void GetRecentResults_HandlesDoublesWithTeamLabelsFromEitherSide()
    {
        var p1 = Guid.NewGuid();
        var partner = Guid.NewGuid();
        var opp1 = Guid.NewGuid();
        var opp2 = Guid.NewGuid();
        var players = new List<Player>
        {
            Player(p1, "Marco"), Player(partner, "Sven"), Player(opp1, "Anna"), Player(opp2, "Lea"),
        };
        var doubles = new List<DoubleMatch>
        {
            DM(new DateTime(2026, 1, 1), p1, partner, opp1, opp2, 3, 1), // team A wins, p1 on team A
        };

        var result = PlayerResultDetailService.GetRecentResults(players, new List<Match>(), doubles, p1, winsOnly: true);

        Assert.Single(result.Rows);
        var row = result.Rows[0];
        Assert.True(row.IsDoubles);
        Assert.Equal("Anna & Lea", row.OpponentLabel);
        Assert.Equal("3:1", row.ScoreLabel);
    }

    [Fact]
    public void GetRecentResults_ReturnsEmptyForPlayerWithNoMatches()
    {
        var p1 = Guid.NewGuid();
        var players = new List<Player> { Player(p1, "Marco") };

        var result = PlayerResultDetailService.GetRecentResults(players, new List<Match>(), new List<DoubleMatch>(), p1, winsOnly: true);

        Assert.Empty(result.Rows);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.OverflowCount);
    }
}
