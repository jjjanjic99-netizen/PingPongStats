using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class AvatarServiceTests
{
    [Theory]
    [InlineData("Anna Berger", "AB")]
    [InlineData("Anna", "A")]
    [InlineData("  Anna   Berger  ", "AB")]
    [InlineData("anna berger", "AB")]
    [InlineData("", "?")]
    [InlineData("   ", "?")]
    public void GetInitials_ReturnsExpected(string displayName, string expected)
    {
        Assert.Equal(expected, AvatarService.GetInitials(displayName));
    }

    [Fact]
    public void GetInitials_UsesFirstAndLastWord_ForMultiWordNames()
    {
        Assert.Equal("AZ", AvatarService.GetInitials("Anna Middle Zimmer"));
    }

    [Fact]
    public void GetAvatarColorHex_IsDeterministic_ForSamePlayerId()
    {
        var id = Guid.NewGuid();
        var first = AvatarService.GetAvatarColorHex(id);
        var second = AvatarService.GetAvatarColorHex(id);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GetAvatarColorHex_ReturnsValidHexColor()
    {
        var color = AvatarService.GetAvatarColorHex(Guid.NewGuid());
        Assert.Matches("^#[0-9A-Fa-f]{6}$", color);
    }

    [Fact]
    public void GetAvatarFilePath_UsesPlayerIdAsFileName()
    {
        var id = Guid.NewGuid();
        var path = AvatarService.GetAvatarFilePath("C:\\Data", id);
        Assert.EndsWith(Path.Combine("avatars", $"{id}.png"), path);
    }

    [Fact]
    public void TryResolveAvatarPath_ReturnsNull_WhenNoAvatarFileNameSet()
    {
        var player = new Player { AvatarFileName = "" };
        Assert.Null(AvatarService.TryResolveAvatarPath("C:\\Data", player));
    }

    [Fact]
    public void TryResolveAvatarPath_ReturnsNull_WhenFileDoesNotExistOnDisk()
    {
        var player = new Player { AvatarFileName = "whatever.png" };
        var tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
        Assert.Null(AvatarService.TryResolveAvatarPath(tempDir, player));
    }

    [Fact]
    public void TryResolveAvatarPath_ReturnsPath_WhenAvatarFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
        var player = new Player { AvatarFileName = "set.png" };
        var expectedPath = AvatarService.GetAvatarFilePath(tempDir, player.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
        File.WriteAllText(expectedPath, "fake-image-bytes");

        try
        {
            var resolved = AvatarService.TryResolveAvatarPath(tempDir, player);
            Assert.Equal(expectedPath, resolved);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
