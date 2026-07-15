using System.Text;
using System.Xml;
using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface IPlayerRepository
{
    List<Player> GetAll();
    void Update(Func<List<Player>, List<Player>> mutate);
    void EnsureFileExists();
    string FilePath { get; }
}

[XmlRoot("Players")]
public class PlayerListXml
{
    [XmlElement("Player")]
    public List<Player> Players { get; set; } = new();
}

/// <summary>Persists players.xml under the configured data path.</summary>
public class PlayerXmlRepository : IPlayerRepository
{
    private readonly AtomicXmlFileStore<Player> _store;

    public PlayerXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "players.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Player>(
            filePath,
            backupDirectory,
            "players",
            deserialize: stream => XmlSerialization.Deserialize<PlayerListXml>(stream).Players,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new PlayerListXml { Players = items }));
    }

    public string FilePath => _store.FilePath;

    public List<Player> GetAll() => _store.GetAll();

    public void Update(Func<List<Player>, List<Player>> mutate) => _store.Update(mutate);

    public void EnsureFileExists() => _store.EnsureFileExists();
}

/// <summary>Shared XmlSerializer helpers producing clean, UTF-8-without-BOM,
/// indented XML for all repositories.</summary>
internal static class XmlSerialization
{
    public static TWrapper Deserialize<TWrapper>(Stream stream) where TWrapper : new()
    {
        var serializer = new XmlSerializer(typeof(TWrapper));
        return (TWrapper?)serializer.Deserialize(stream) ?? new TWrapper();
    }

    public static void Serialize<TWrapper>(Stream stream, TWrapper wrapper)
    {
        var serializer = new XmlSerializer(typeof(TWrapper));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            OmitXmlDeclaration = false,
        };

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add(string.Empty, string.Empty); // suppress default xmlns/xsi attributes

        using var writer = XmlWriter.Create(stream, settings);
        serializer.Serialize(writer, wrapper, namespaces);
    }
}
