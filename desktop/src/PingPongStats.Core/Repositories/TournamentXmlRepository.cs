using System.Xml.Serialization;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Repositories;

public interface ITournamentRepository
{
    List<Tournament> GetAll();
    void Update(Func<List<Tournament>, List<Tournament>> mutate);
    void EnsureFileExists();
}

[XmlRoot("Tournaments")]
public class TournamentListXml
{
    [XmlElement("Tournament")]
    public List<Tournament> Tournaments { get; set; } = new();
}

/// <summary>Tournaments (bracket + progress), persisted as tournaments.xml. The
/// matches themselves live in matches.xml/doubles.xml as usual (tagged with
/// TournamentId); this file only tracks bracket structure and status.</summary>
public class TournamentXmlRepository : ITournamentRepository
{
    private readonly AtomicXmlFileStore<Tournament> _store;

    public TournamentXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "tournaments.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Tournament>(
            filePath,
            backupDirectory,
            "tournaments",
            deserialize: stream => XmlSerialization.Deserialize<TournamentListXml>(stream).Tournaments,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new TournamentListXml { Tournaments = items }));
    }

    public List<Tournament> GetAll() => _store.GetAll();

    public void Update(Func<List<Tournament>, List<Tournament>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
