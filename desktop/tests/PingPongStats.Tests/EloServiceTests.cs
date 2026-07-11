using PingPongStats.Core.Services;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class EloServiceTests
{
    [Fact]
    public void ComputeRatings_KeepsInitialRatingForPlayersWithoutMatches()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var ratings = EloService.ComputeRatings(new List<Core.Models.Match>(), new[] { p1, p2 });

        Assert.Equal(EloService.DefaultInitialRating, ratings[p1]);
        Assert.Equal(EloService.DefaultInitialRating, ratings[p2]);
    }

    [Fact]
    public void ComputeRatings_WinnerGainsLoserLosesAndTotalIsZeroSum()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var matches = new List<Core.Models.Match> { M(new DateTime(2026, 1, 1), p1, p2, 3, 0) };

        var ratings = EloService.ComputeRatings(matches, new[] { p1, p2 });

        Assert.True(ratings[p1] > EloService.DefaultInitialRating);
        Assert.True(ratings[p2] < EloService.DefaultInitialRating);
        Assert.Equal(EloService.DefaultInitialRating * 2, ratings[p1] + ratings[p2], precision: 6);
    }

    [Fact]
    public void ComputeRatings_ProcessesMatchesChronologicallyRegardlessOfInputOrder()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var m1 = M(new DateTime(2026, 1, 1), p1, p2, 3, 0);
        var m2 = M(new DateTime(2026, 1, 2), p1, p2, 3, 0);

        var chronological = EloService.ComputeRatings(new List<Core.Models.Match> { m1, m2 }, new[] { p1, p2 });
        var reversed = EloService.ComputeRatings(new List<Core.Models.Match> { m2, m1 }, new[] { p1, p2 });

        Assert.Equal(chronological[p1], reversed[p1], precision: 6);
        Assert.Equal(chronological[p2], reversed[p2], precision: 6);
    }

    [Fact]
    public void GetRatingHistory_LastPointMatchesComputeRatingsFinalValue()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var matches = new List<Core.Models.Match>
        {
            M(new DateTime(2026, 1, 1), p1, p2, 3, 0),
            M(new DateTime(2026, 1, 2), p2, p3, 3, 1),
            M(new DateTime(2026, 1, 3), p1, p2, 0, 3),
            M(new DateTime(2026, 1, 4), p1, p3, 3, 2),
        };

        var finalRatings = EloService.ComputeRatings(matches, new[] { p1, p2, p3 });
        var history = EloService.GetRatingHistory(matches, p1);

        Assert.Equal(3, history.Count); // only p1's 3 matches, not the p2-vs-p3 one
        Assert.Equal(finalRatings[p1], history[^1].Rating, precision: 6);
        Assert.Equal(matches[3].PlayedAt, history[^1].PlayedAt);
    }

    [Fact]
    public void GetRatingHistory_ReturnsEmptyForPlayerWithoutMatches()
    {
        Assert.Empty(EloService.GetRatingHistory(new List<Core.Models.Match>(), Guid.NewGuid()));
    }
}
