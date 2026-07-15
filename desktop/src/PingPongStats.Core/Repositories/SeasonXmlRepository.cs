using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface ISeasonRepository
{
    List<Season> GetAll();
    void Update(Func<List<Season>, List<Season>> mutate);
    void EnsureFileExists();
}

[XmlRoot("Seasons")]
public class SeasonListXml
{
    [XmlElement("Season")]
    public List<Season> Seasons { get; set; } = new();
}

/// <summary>Manually-created league seasons persisted as seasons.xml.</summary>
public class SeasonXmlRepository : ISeasonRepository
{
    private readonly AtomicXmlFileStore<Season> _store;

    public SeasonXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "seasons.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Season>(
            filePath,
            backupDirectory,
            "seasons",
            deserialize: stream => XmlSerialization.Deserialize<SeasonListXml>(stream).Seasons,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new SeasonListXml { Seasons = items }));
    }

    public List<Season> GetAll() => _store.GetAll();

    public void Update(Func<List<Season>, List<Season>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}
