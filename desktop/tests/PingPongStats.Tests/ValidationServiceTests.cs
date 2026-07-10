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
}
