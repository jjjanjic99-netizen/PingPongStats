using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class ComebackServiceTests
{
    private static Match WithSets(Match match, params (int PointsA, int PointsB)[] sets)
    {
        match.SetResults = sets
            .Select((s, i) => new SetResult { SetNumber = i + 1, PointsA = s.PointsA, PointsB = s.PointsB })
            .ToList();
        return match;
    }

    [Fact]
    public void FindBestComeback_ReturnsNullWhenNoMatchHasSetData()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Match> { M(new DateTime(2026, 1, 1), p1, p2, 3, 0) }; // no SetResults

        Assert.Null(ComebackService.FindBestComeback(matches));
    }

    [Fact]
    public void FindBestComeback_ReturnsNullWhenNoQualifyingDeficit()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        // p1 wins 3:1, but was never down by 2+ sets (lost only set 2, deficit maxes at 1)
        var match = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 3, 1),
            (11, 5), (5, 11), (11, 5), (11, 5));

        Assert.Null(ComebackService.FindBestComeback(new List<Match> { match }));
    }

    [Fact]
    public void FindBestComeback_DetectsComebackFromTwoSetDeficit()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        // p1 down 0:2, wins the next three sets => overcame a 2-set deficit
        var match = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 3, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5));

        var result = ComebackService.FindBestComeback(new List<Match> { match });

        Assert.NotNull(result);
        Assert.Equal(p1, result!.WinnerId);
        Assert.Equal(2, result.ComebackValue);
    }

    [Fact]
    public void FindBestComeback_ComebackValueIsMaximumDeficitEverReached()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        // p1 falls behind 0:3 (deficit 3) before winning 4 in a row => comeback value 3
        var match = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 4, 3),
            (5, 11), (5, 11), (5, 11), (11, 5), (11, 5), (11, 5), (11, 5));

        var result = ComebackService.FindBestComeback(new List<Match> { match });

        Assert.NotNull(result);
        Assert.Equal(3, result!.ComebackValue);
    }

    [Fact]
    public void FindBestComeback_PicksHighestComebackValueAmongCandidates()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        var smallComeback = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 3, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5)); // deficit 2

        var bigComeback = WithSets(
            M(new DateTime(2026, 1, 2), p3, p4, 4, 3),
            (5, 11), (5, 11), (5, 11), (11, 5), (11, 5), (11, 5), (11, 5)); // deficit 3

        var result = ComebackService.FindBestComeback(new List<Match> { smallComeback, bigComeback });

        Assert.NotNull(result);
        Assert.Equal(p3, result!.WinnerId);
        Assert.Equal(3, result.ComebackValue);
    }

    [Fact]
    public void FindBestComeback_TiebreaksByCloserFinalScore()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        // Both have comeback value 2, but the second ends 3:2 (closer) vs 4:2
        var wideFinal = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 4, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5), (11, 5));

        var closeFinal = WithSets(
            M(new DateTime(2026, 1, 2), p3, p4, 3, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5));

        var result = ComebackService.FindBestComeback(new List<Match> { wideFinal, closeFinal });

        Assert.NotNull(result);
        Assert.Equal(p3, result!.WinnerId);
    }

    [Fact]
    public void FindBestComeback_IgnoresMatchesWithoutSetDataEvenWhenOthersQualify()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var p4 = Guid.NewGuid();

        var withoutSets = M(new DateTime(2026, 1, 1), p1, p2, 3, 0);
        var withSets = WithSets(
            M(new DateTime(2026, 1, 2), p3, p4, 3, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5));

        var result = ComebackService.FindBestComeback(new List<Match> { withoutSets, withSets });

        Assert.NotNull(result);
        Assert.Equal(p3, result!.WinnerId);
    }

    [Fact]
    public void FindBestComeback_ReturnsNullForEmptyMatchList()
    {
        Assert.Null(ComebackService.FindBestComeback(new List<Match>()));
    }

    [Fact]
    public void IsComeback_TrueWhenDeficitReachesTwo()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var match = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 3, 2),
            (5, 11), (5, 11), (11, 5), (11, 5), (11, 5));

        Assert.True(ComebackService.IsComeback(match));
    }

    [Fact]
    public void IsComeback_FalseWhenDeficitNeverReachesTwo()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var match = WithSets(
            M(new DateTime(2026, 1, 1), p1, p2, 3, 1),
            (11, 5), (5, 11), (11, 5), (11, 5));

        Assert.False(ComebackService.IsComeback(match));
    }

    [Fact]
    public void IsComeback_FalseWithoutSetData()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var match = M(new DateTime(2026, 1, 1), p1, p2, 3, 0);

        Assert.False(ComebackService.IsComeback(match));
    }

    [Fact]
    public void IsComebackDoubles_TrueWhenDeficitReachesTwo()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var match = new DoubleMatch
        {
            Id = Guid.NewGuid(),
            PlayedAt = new DateTime(2026, 1, 1),
            TeamAPlayer1Id = a1,
            TeamAPlayer2Id = a2,
            TeamBPlayer1Id = b1,
            TeamBPlayer2Id = b2,
            TeamASets = 3,
            TeamBSets = 2,
            WinningTeam = "A",
            SetResults = new List<SetResult>
            {
                new() { SetNumber = 1, PointsA = 5, PointsB = 11 },
                new() { SetNumber = 2, PointsA = 5, PointsB = 11 },
                new() { SetNumber = 3, PointsA = 11, PointsB = 5 },
                new() { SetNumber = 4, PointsA = 11, PointsB = 5 },
                new() { SetNumber = 5, PointsA = 11, PointsB = 5 },
            },
        };

        Assert.True(ComebackService.IsComebackDoubles(match));
    }

    [Fact]
    public void IsComebackDoubles_FalseWithoutSetData()
    {
        var match = new DoubleMatch
        {
            Id = Guid.NewGuid(),
            PlayedAt = new DateTime(2026, 1, 1),
            TeamAPlayer1Id = Guid.NewGuid(),
            TeamAPlayer2Id = Guid.NewGuid(),
            TeamBPlayer1Id = Guid.NewGuid(),
            TeamBPlayer2Id = Guid.NewGuid(),
            TeamASets = 3,
            TeamBSets = 0,
            WinningTeam = "A",
        };

        Assert.False(ComebackService.IsComebackDoubles(match));
    }
}
