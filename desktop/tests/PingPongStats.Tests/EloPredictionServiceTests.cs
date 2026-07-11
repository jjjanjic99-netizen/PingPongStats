using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class EloPredictionServiceTests
{
    [Fact]
    public void ComputeWinProbability_ReturnsFiftyPercentForEqualRatings()
    {
        Assert.Equal(0.5, EloPredictionService.ComputeWinProbability(1000, 1000), precision: 6);
    }

    [Fact]
    public void ComputeWinProbability_FavorsHigherRatedPlayer()
    {
        var probability = EloPredictionService.ComputeWinProbability(1200, 1000);
        Assert.True(probability > 0.5);
    }

    [Fact]
    public void ComputeWinProbability_IsSymmetric()
    {
        var probabilityA = EloPredictionService.ComputeWinProbability(1100, 900);
        var probabilityB = EloPredictionService.ComputeWinProbability(900, 1100);
        Assert.Equal(1.0, probabilityA + probabilityB, precision: 6);
    }

    [Fact]
    public void ComputeWinProbability_MatchesStandardEloFormula()
    {
        // Exact formula per spec: 1 / (1 + 10^((eloB - eloA) / 400))
        var expected = 1.0 / (1.0 + Math.Pow(10, (800.0 - 1200.0) / 400.0));
        Assert.Equal(expected, EloPredictionService.ComputeWinProbability(1200, 800), precision: 10);
    }

    [Fact]
    public void ComputeTeamElo_AveragesBothPlayers()
    {
        Assert.Equal(1100, EloPredictionService.ComputeTeamElo(1000, 1200), precision: 6);
    }
}
