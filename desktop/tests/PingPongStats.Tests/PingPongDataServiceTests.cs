using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class PingPongDataServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PingPongDataService _service;

    public PingPongDataServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new PingPongDataService(
            new PlayerXmlRepository(_tempDir), new MatchXmlRepository(_tempDir), new DoubleMatchXmlRepository(_tempDir));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }

    [Fact]
    public void CreatePlayer_RequiresDisplayName()
    {
        Assert.Throws<ValidationException>(() => _service.CreatePlayer("", "", "", ""));
    }

    [Fact]
    public void CreateMatch_PersistsAndComputesWinner()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");

        var match = _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 1, "Testspiel");

        Assert.Equal(a.Id, match.WinnerId);
        Assert.Single(_service.Matches);
    }

    [Fact]
    public void CreateMatch_RejectsUnknownPlayer()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        Assert.Throws<ValidationException>(() => _service.CreateMatch(DateTime.Now, a.Id, Guid.NewGuid(), 3, 0, ""));
    }

    [Fact]
    public void DeletePlayer_BlockedWhenMatchesExist()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");
        _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 0, "");

        Assert.Throws<ValidationException>(() => _service.DeletePlayer(a.Id));
    }

    [Fact]
    public void DeletePlayer_AllowedWhenNoMatchesExist()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        _service.DeletePlayer(a.Id);
        Assert.Empty(_service.Players);
    }

    [Fact]
    public void SetPlayerActive_ArchivesWithoutRemovingHistory()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");
        _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 0, "");

        _service.SetPlayerActive(a.Id, false);

        Assert.False(_service.Players.Single(p => p.Id == a.Id).IsActive);
        Assert.Single(_service.Matches);
    }

    [Fact]
    public void CreateDoubleMatch_PersistsAndComputesWinningTeam()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");
        var b2 = _service.CreatePlayer("David", "", "", "");

        var match = _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, b2.Id, 3, 1, "Doppel-Testspiel");

        Assert.Equal("A", match.WinningTeam);
        Assert.Single(_service.DoubleMatches);
    }

    [Fact]
    public void CreateDoubleMatch_RejectsDuplicatePlayerAcrossTeams()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");

        Assert.Throws<ValidationException>(() =>
            _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, a1.Id, 3, 1, ""));
    }

    [Fact]
    public void DeleteDoubleMatch_RemovesEntry()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");
        var b2 = _service.CreatePlayer("David", "", "", "");
        var match = _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, b2.Id, 3, 0, "");

        _service.DeleteDoubleMatch(match.Id);

        Assert.Empty(_service.DoubleMatches);
    }
}
