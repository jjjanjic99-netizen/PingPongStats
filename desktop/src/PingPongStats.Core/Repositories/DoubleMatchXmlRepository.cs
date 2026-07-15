using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface IDoubleMatchRepository
{
    List<DoubleMatch> GetAll();
    void Update(Func<List<DoubleMatch>, List<DoubleMatch>> mutate);
    void EnsureFileExists();
    string FilePath { get; }
}

[XmlRoot("DoubleMatches")]
public class DoubleMatchListXml
{
    [XmlElement("DoubleMatch")]
    public List<DoubleMatch> DoubleMatches { get; set; } = new();
}

/// <summary>Persists doubles.xml under the configured data path.</summary>
public class DoubleMatchXmlRepository : IDoubleMatchRepository
{
    private readonly AtomicXmlFileStore<DoubleMatch> _store;

    public DoubleMatchXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "doubles.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<DoubleMatch>(
            filePath,
            backupDirectory,
            "doubles",
            deserialize: stream => XmlSerialization.Deserialize<DoubleMatchListXml>(stream).DoubleMatches,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new DoubleMatchListXml { DoubleMatches = items }));
    }

    public string FilePath => _store.FilePath;

    public List<DoubleMatch> GetAll() => _store.GetAll();

    public void Update(Func<List<DoubleMatch>, List<DoubleMatch>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
