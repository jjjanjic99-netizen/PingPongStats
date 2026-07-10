using System.Xml.Serialization;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Repositories;

public interface IMatchRepository
{
    List<Match> GetAll();
    void Update(Func<List<Match>, List<Match>> mutate);
    void EnsureFileExists();
    string FilePath { get; }
}

[XmlRoot("Matches")]
public class MatchListXml
{
    [XmlElement("Match")]
    public List<Match> Matches { get; set; } = new();
}

/// <summary>Persists matches.xml under the configured data path.</summary>
public class MatchXmlRepository : IMatchRepository
{
    private readonly AtomicXmlFileStore<Match> _store;

    public MatchXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "matches.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Match>(
            filePath,
            backupDirectory,
            "matches",
            deserialize: stream => XmlSerialization.Deserialize<MatchListXml>(stream).Matches,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new MatchListXml { Matches = items }));
    }

    public string FilePath => _store.FilePath;

    public List<Match> GetAll() => _store.GetAll();

    public void Update(Func<List<Match>, List<Match>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
