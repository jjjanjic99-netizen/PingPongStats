using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using Xunit;

namespace PingPongStats.Tests;

public class XmlRepositoryTests : IDisposable
{
    private readonly string _tempDir;

    public XmlRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
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
    public void GetAll_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        var players = repo.GetAll();
        Assert.Empty(players);
    }

    [Fact]
    public void EnsureFileExists_CreatesEmptyValidFile()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        repo.EnsureFileExists();

        Assert.True(File.Exists(Path.Combine(_tempDir, "players.xml")));
        Assert.Empty(repo.GetAll());
    }

    [Fact]
    public void Update_SavesAndReloadsRoundTrip()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        var player = new Player
        {
            DisplayName = "Anna Berger",
            FirstName = "Anna",
            LastName = "Berger",
            Email = "anna@example.com",
            IsActive = true,
            CreatedAt = new DateTime(2026, 7, 10, 12, 0, 0),
            UpdatedAt = new DateTime(2026, 7, 10, 12, 0, 0),
        };

        repo.Update(players =>
        {
            players.Add(player);
            return players;
        });

        var reloaded = repo.GetAll();
        Assert.Single(reloaded);
        Assert.Equal("Anna Berger", reloaded[0].DisplayName);
        Assert.Equal(player.Id, reloaded[0].Id);
        Assert.Equal(new DateTime(2026, 7, 10, 12, 0, 0), reloaded[0].CreatedAt);
    }

    [Fact]
    public void PlayerRepository_RoundTripsAvatarAndPinFields()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        var player = new Player
        {
            DisplayName = "Ben Hofer",
            AvatarFileName = $"{Guid.NewGuid()}.png",
            PinHash = "aGFzaA==",
            PinSalt = "c2FsdA==",
        };

        repo.Update(players =>
        {
            players.Add(player);
            return players;
        });

        var reloaded = repo.GetAll();
        Assert.Single(reloaded);
        Assert.Equal(player.AvatarFileName, reloaded[0].AvatarFileName);
        Assert.Equal("aGFzaA==", reloaded[0].PinHash);
        Assert.Equal("c2FsdA==", reloaded[0].PinSalt);
    }

    [Fact]
    public void MatchRepository_RoundTripsSetResults()
    {
        var repo = new MatchXmlRepository(_tempDir);
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var match = new Match
        {
            PlayedAt = new DateTime(2026, 7, 10, 15, 30, 0),
            PlayerAId = playerA,
            PlayerBId = playerB,
            PlayerASets = 2,
            PlayerBSets = 1,
            WinnerId = playerA,
            SetResults = new List<SetResult>
            {
                new() { SetNumber = 1, PointsA = 11, PointsB = 7 },
                new() { SetNumber = 2, PointsA = 9, PointsB = 11 },
                new() { SetNumber = 3, PointsA = 11, PointsB = 8 },
            },
        };

        repo.Update(matches =>
        {
            matches.Add(match);
            return matches;
        });

        var reloaded = repo.GetAll();
        Assert.Single(reloaded);
        Assert.Equal(3, reloaded[0].SetResults.Count);
        Assert.Equal(11, reloaded[0].SetResults[0].PointsA);
        Assert.Equal(8, reloaded[0].SetResults[2].PointsB);
    }

    [Fact]
    public void Update_WritesFileWithoutByteOrderMark()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        repo.Update(players =>
        {
            players.Add(new Player { DisplayName = "Test" });
            return players;
        });

        var bytes = File.ReadAllBytes(Path.Combine(_tempDir, "players.xml"));
        var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
        Assert.False(bytes.Length >= 3 && bytes[0] == utf8Bom[0] && bytes[1] == utf8Bom[1] && bytes[2] == utf8Bom[2]);
    }

    [Fact]
    public void Update_CreatesBackupOfPreviousVersion()
    {
        var repo = new PlayerXmlRepository(_tempDir);
        repo.Update(players =>
        {
            players.Add(new Player { DisplayName = "First" });
            return players;
        });

        repo.Update(players =>
        {
            players.Add(new Player { DisplayName = "Second" });
            return players;
        });

        var backupDir = Path.Combine(_tempDir, "backups");
        Assert.True(Directory.Exists(backupDir));
        var backups = Directory.GetFiles(backupDir, "players_*.xml");
        Assert.Single(backups);
    }

    [Fact]
    public void GetAll_ThrowsClearException_WhenFileIsCorrupted()
    {
        var filePath = Path.Combine(_tempDir, "players.xml");
        File.WriteAllText(filePath, "<Players><Player><Id>not-well-formed");

        var repo = new PlayerXmlRepository(_tempDir);

        var ex = Assert.Throws<XmlDataCorruptException>(() => repo.GetAll());
        Assert.Contains("players.xml", ex.Message);
    }

    [Fact]
    public void MatchRepository_RoundTripsAllFields()
    {
        var repo = new MatchXmlRepository(_tempDir);
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var match = new Match
        {
            PlayedAt = new DateTime(2026, 7, 10, 15, 30, 0),
            PlayerAId = playerA,
            PlayerBId = playerB,
            PlayerASets = 3,
            PlayerBSets = 1,
            WinnerId = playerA,
            Notes = "Gutes Spiel",
            CreatedAt = new DateTime(2026, 7, 10, 15, 30, 0),
            UpdatedAt = new DateTime(2026, 7, 10, 15, 30, 0),
        };

        repo.Update(matches =>
        {
            matches.Add(match);
            return matches;
        });

        var reloaded = repo.GetAll();
        Assert.Single(reloaded);
        Assert.Equal(match.PlayerASets, reloaded[0].PlayerASets);
        Assert.Equal(match.WinnerId, reloaded[0].WinnerId);
        Assert.Equal("Gutes Spiel", reloaded[0].Notes);
    }

    [Fact]
    public void PlayerRepository_LoadsOldFormatFile_WithoutNewOptionalFields()
    {
        // Exact pre-existing schema: no AvatarFileName/PinHash/PinSalt elements at all.
        var xml =
            "<Players>\n" +
            "  <Player>\n" +
            "    <Id>3fa85f64-5717-4562-b3fc-2c963f66afa6</Id>\n" +
            "    <DisplayName>Markus</DisplayName>\n" +
            "    <FirstName>Markus</FirstName>\n" +
            "    <LastName>Schlegel</LastName>\n" +
            "    <Email></Email>\n" +
            "    <IsActive>true</IsActive>\n" +
            "    <CreatedAt>2026-07-10T12:00:00</CreatedAt>\n" +
            "    <UpdatedAt>2026-07-10T12:00:00</UpdatedAt>\n" +
            "  </Player>\n" +
            "</Players>\n";
        File.WriteAllText(Path.Combine(_tempDir, "players.xml"), xml);

        var repo = new PlayerXmlRepository(_tempDir);
        var players = repo.GetAll();

        Assert.Single(players);
        Assert.Equal("Markus", players[0].DisplayName);
        Assert.Equal(string.Empty, players[0].AvatarFileName);
        Assert.Equal(string.Empty, players[0].PinHash);
        Assert.Equal(string.Empty, players[0].PinSalt);
    }

    [Fact]
    public void MatchRepository_LoadsOldFormatFile_WithoutSetResults()
    {
        // Exact pre-existing schema: no SetResults element at all.
        var xml =
            "<Matches>\n" +
            "  <Match>\n" +
            "    <Id>3fa85f64-5717-4562-b3fc-2c963f66afa6</Id>\n" +
            "    <PlayedAt>2026-07-10T12:00:00</PlayedAt>\n" +
            "    <PlayerAId>3fa85f64-5717-4562-b3fc-2c963f66afa7</PlayerAId>\n" +
            "    <PlayerBId>3fa85f64-5717-4562-b3fc-2c963f66afa8</PlayerBId>\n" +
            "    <PlayerASets>3</PlayerASets>\n" +
            "    <PlayerBSets>1</PlayerBSets>\n" +
            "    <WinnerId>3fa85f64-5717-4562-b3fc-2c963f66afa7</WinnerId>\n" +
            "    <Notes>Gutes Spiel</Notes>\n" +
            "    <CreatedAt>2026-07-10T12:00:00</CreatedAt>\n" +
            "    <UpdatedAt>2026-07-10T12:00:00</UpdatedAt>\n" +
            "  </Match>\n" +
            "</Matches>\n";
        File.WriteAllText(Path.Combine(_tempDir, "matches.xml"), xml);

        var repo = new MatchXmlRepository(_tempDir);
        var matches = repo.GetAll();

        Assert.Single(matches);
        Assert.Equal(3, matches[0].PlayerASets);
        Assert.Empty(matches[0].SetResults);
    }

    [Fact]
    public void Update_ThrowsLockTimeout_WhenFileIsHeldByAnotherProcess()
    {
        var filePath = Path.Combine(_tempDir, "players.xml");
        var store = new AtomicXmlFileStore<Player>(
            filePath,
            Path.Combine(_tempDir, "backups"),
            "players",
            deserialize: _ => new List<Player>(),
            serialize: (_, _) => { },
            lockTimeout: TimeSpan.FromMilliseconds(500));

        var lockPath = filePath + ".lock";
        using var lockStream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        Assert.Throws<DataFileLockTimeoutException>(() => store.Update(players => players));
    }
}
