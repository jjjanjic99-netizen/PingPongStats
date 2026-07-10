using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class DataPathServiceTests : IDisposable
{
    private readonly string _tempDir;

    public DataPathServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }

    [Fact]
    public void ValidateAndPrepare_CreatesFolderStructureAndDefaultFiles_WhenMissing()
    {
        var nestedPath = Path.Combine(_tempDir, "PingPongStats", "Data");
        new DataPathService().ValidateAndPrepare(nestedPath);

        Assert.True(Directory.Exists(nestedPath));
        Assert.True(Directory.Exists(Path.Combine(nestedPath, "backups")));
        Assert.True(File.Exists(Path.Combine(nestedPath, "players.xml")));
        Assert.True(File.Exists(Path.Combine(nestedPath, "matches.xml")));
        Assert.True(File.Exists(Path.Combine(nestedPath, "audit-log.xml")));
    }

    [Fact]
    public void ValidateAndPrepare_IsIdempotent_WhenCalledAgainOnExistingData()
    {
        var dataPath = Path.Combine(_tempDir, "Data");
        var service = new DataPathService();
        service.ValidateAndPrepare(dataPath);

        var playerRepo = new PlayerXmlRepository(dataPath);
        playerRepo.Update(players =>
        {
            players.Add(new Core.Models.Player { DisplayName = "Existing" });
            return players;
        });

        service.ValidateAndPrepare(dataPath);

        Assert.Single(playerRepo.GetAll());
    }

    [Fact]
    public void ValidateAndPrepare_Throws_WhenPathIsEmpty()
    {
        Assert.Throws<DataPathUnavailableException>(() => new DataPathService().ValidateAndPrepare(""));
    }
}
