using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class ValidationServiceTests
{
    [Fact]
    public void ComputeWinnerId_PlayerAWinsWithMoreSets()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var winner = ValidationService.ComputeWinnerId(a, b, 3, 1);
        Assert.Equal(a, winner);
    }

    [Fact]
    public void ComputeWinnerId_PlayerBWinsWithMoreSets()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var winner = ValidationService.ComputeWinnerId(a, b, 0, 1);
        Assert.Equal(b, winner);
    }

    [Fact]
    public void ComputeWinnerId_RejectsIdenticalPlayers()
    {
        var a = Guid.NewGuid();
        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinnerId(a, a, 3, 1));
    }

    [Fact]
    public void ComputeWinnerId_RejectsTie()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinnerId(a, b, 2, 2));
    }

    [Fact]
    public void ComputeWinnerId_RejectsNegativeSets()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinnerId(a, b, -1, 2));
    }

    [Fact]
    public void ComputeWinnerId_RejectsEmptyPlayerIds()
    {
        var a = Guid.NewGuid();
        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinnerId(Guid.Empty, a, 3, 1));
    }

    [Fact]
    public void ComputeWinningTeam_TeamAWinsWithMoreSets()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();

        var winner = ValidationService.ComputeWinningTeam(a1, a2, b1, b2, 3, 1);

        Assert.Equal("A", winner);
    }

    [Fact]
    public void ComputeWinningTeam_TeamBWinsWithMoreSets()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();

        var winner = ValidationService.ComputeWinningTeam(a1, a2, b1, b2, 1, 3);

        Assert.Equal("B", winner);
    }

    [Fact]
    public void ComputeWinningTeam_RejectsDuplicatePlayerAcrossTeams()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();

        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinningTeam(a1, a2, b1, a1, 3, 1));
    }

    [Fact]
    public void ComputeWinningTeam_RejectsTie()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();

        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinningTeam(a1, a2, b1, b2, 2, 2));
    }

    [Fact]
    public void ComputeWinningTeam_RejectsNegativeSets()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();

        Assert.Throws<ValidationException>(() => ValidationService.ComputeWinningTeam(a1, a2, b1, b2, -1, 3));
    }
}
