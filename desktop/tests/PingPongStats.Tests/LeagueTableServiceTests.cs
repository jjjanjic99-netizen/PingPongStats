using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class LeagueTableServiceTests
{
    private static Season MakeSeason(DateTime start, DateTime end) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test-Saison",
        StartDate = start,
        EndDate = end,
        IsActive = true,
    };

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
    public void BuildSinglesTable_AwardsThreePointsPerWinZeroPerLoss()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 5), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 6), p2, p1, 3, 0),
        };

        var table = LeagueTableService.BuildSinglesTable(matches, season);

        var row1 = table.Single(r => r.PlayerId == p1);
        Assert.Equal(1, row1.Wins);
        Assert.Equal(1, row1.Losses);
        Assert.Equal(3, row1.Points);
    }

    [Fact]
    public void BuildSinglesTable_ExcludesMatchesOutsideSeasonWindow()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 15), p1, p2, 3, 0), // inside
            M(new DateTime(2026, 2, 1), p1, p2, 3, 0), // outside (after end)
            M(new DateTime(2025, 12, 31), p1, p2, 3, 0), // outside (before start)
        };

        var table = LeagueTableService.BuildSinglesTable(matches, season);

        var row1 = table.Single(r => r.PlayerId == p1);
        Assert.Equal(1, row1.Played);
    }

    [Fact]
    public void BuildSinglesTable_TiebreaksBySetDifference()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var matches = new List<Match>
        {
            // p1: 1 win, set diff = 3-0 = +3
            M(new DateTime(2026, 1, 1), p1, p3, 3, 0),
            // p2: 1 win, set diff = 3-2 = +1
            M(new DateTime(2026, 1, 2), p2, p3, 3, 2),
        };

        var table = LeagueTableService.BuildSinglesTable(matches, season);

        Assert.Equal(p1, table[0].PlayerId); // same points (3), better set diff
        Assert.Equal(p2, table[1].PlayerId);
    }

    [Fact]
    public void BuildSinglesTable_HeadToHeadBreaksExactTieOnPointsAndSetDifference()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var filler1 = Guid.NewGuid();
        var filler2 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var matches = new List<Match>
        {
            // p1 beats p2 head-to-head, but each also loses/wins elsewhere so their
            // overall points (3) and set difference (0) end up perfectly tied.
            M(new DateTime(2026, 1, 1), p1, p2, 3, 1), // p1 win, diff +2/-2
            M(new DateTime(2026, 1, 2), filler1, p1, 3, 1), // p1 loss, diff -2
            M(new DateTime(2026, 1, 3), p2, filler2, 3, 1), // p2 win, diff +2
        };

        var table = LeagueTableService.BuildSinglesTable(matches, season);

        var p1Row = table.Single(r => r.PlayerId == p1);
        var p2Row = table.Single(r => r.PlayerId == p2);
        Assert.Equal(p1Row.Points, p2Row.Points);
        Assert.Equal(p1Row.SetDifference, p2Row.SetDifference);

        var p1Index = table.FindIndex(r => r.PlayerId == p1);
        var p2Index = table.FindIndex(r => r.PlayerId == p2);
        Assert.True(p1Index < p2Index); // p1 ranks above p2 on head-to-head
    }

    [Fact]
    public void BuildDoublesTable_CreditsBothTeamMembers()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        var matches = new List<DoubleMatch> { MD(new DateTime(2026, 1, 1), a1, a2, b1, b2, 3, 0) };

        var table = LeagueTableService.BuildDoublesTable(matches, season);

        Assert.Equal(4, table.Count);
        Assert.Equal(3, table.Single(r => r.PlayerId == a1).Points);
        Assert.Equal(3, table.Single(r => r.PlayerId == a2).Points);
        Assert.Equal(0, table.Single(r => r.PlayerId == b1).Points);
        Assert.Equal(0, table.Single(r => r.PlayerId == b2).Points);
    }

    [Fact]
    public void BuildDoublesTable_ExcludesMatchesOutsideSeasonWindow()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var matches = new List<DoubleMatch>
        {
            MD(new DateTime(2026, 2, 1), a1, a2, b1, b2, 3, 0), // outside season
        };

        var table = LeagueTableService.BuildDoublesTable(matches, season);

        Assert.Empty(table);
    }

    [Fact]
    public void BuildSinglesTable_ReturnsEmptyForSeasonWithNoMatches()
    {
        var season = MakeSeason(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        Assert.Empty(LeagueTableService.BuildSinglesTable(new List<Match>(), season));
    }
}
