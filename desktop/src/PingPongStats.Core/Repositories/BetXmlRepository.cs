using System.Xml.Serialization;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Repositories;

public interface IBetRepository
{
    List<Bet> GetAll();
    void Update(Func<List<Bet>, List<Bet>> mutate);
    void EnsureFileExists();
}

[XmlRoot("Bets")]
public class BetListXml
{
    [XmlElement("Bet")]
    public List<Bet> Bets { get; set; } = new();
}

/// <summary>Player tips on pending matches (Phase 15), persisted as bets.xml.</summary>
public class BetXmlRepository : IBetRepository
{
    private readonly AtomicXmlFileStore<Bet> _store;

    public BetXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "bets.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Bet>(
            filePath,
            backupDirectory,
            "bets",
            deserialize: stream => XmlSerialization.Deserialize<BetListXml>(stream).Bets,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new BetListXml { Bets = items }));
    }

    public List<Bet> GetAll() => _store.GetAll();

    public void Update(Func<List<Bet>, List<Bet>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
