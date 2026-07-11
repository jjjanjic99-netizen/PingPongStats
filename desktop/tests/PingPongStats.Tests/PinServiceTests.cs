using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class PinServiceTests
{
    [Theory]
    [InlineData("1234", true)]
    [InlineData("0000", true)]
    [InlineData("123", false)]
    [InlineData("12345", false)]
    [InlineData("12a4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidPinFormat_RequiresExactlyFourDigits(string? pin, bool expected)
    {
        Assert.Equal(expected, PinService.IsValidPinFormat(pin));
    }

    [Fact]
    public void HashPin_ProducesDifferentSaltsForSamePin()
    {
        var (hash1, salt1) = PinService.HashPin("1234");
        var (hash2, salt2) = PinService.HashPin("1234");

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPin_AcceptsCorrectPin()
    {
        var (hash, salt) = PinService.HashPin("4321");

        Assert.True(PinService.VerifyPin("4321", hash, salt));
    }

    [Fact]
    public void VerifyPin_RejectsWrongPin()
    {
        var (hash, salt) = PinService.HashPin("4321");

        Assert.False(PinService.VerifyPin("0000", hash, salt));
    }

    [Fact]
    public void VerifyPin_RejectsWhenNoHashStored()
    {
        Assert.False(PinService.VerifyPin("1234", "", ""));
    }
}
