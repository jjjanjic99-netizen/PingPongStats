using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface IPendingMatchRepository
{
    List<PendingMatch> GetAll();
    void Update(Func<List<PendingMatch>, List<PendingMatch>> mutate);
    void EnsureFileExists();
}

[XmlRoot("PendingMatches")]
public class PendingMatchListXml
{
    [XmlElement("PendingMatch")]
    public List<PendingMatch> PendingMatches { get; set; } = new();
}

/// <summary>Announced pairings to bet on (Phase 15), persisted as pendingmatches.xml.</summary>
public class PendingMatchXmlRepository : IPendingMatchRepository
{
    private readonly AtomicXmlFileStore<PendingMatch> _store;

    public PendingMatchXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "pendingmatches.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<PendingMatch>(
            filePath,
            backupDirectory,
            "pendingmatches",
            deserialize: stream => XmlSerialization.Deserialize<PendingMatchListXml>(stream).PendingMatches,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new PendingMatchListXml { PendingMatches = items }));
    }

    public List<PendingMatch> GetAll() => _store.GetAll();

    public void Update(Func<List<PendingMatch>, List<PendingMatch>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
