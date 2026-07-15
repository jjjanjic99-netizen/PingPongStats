using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface IQuoteRepository
{
    List<Quote> GetAll();
    void EnsureSeeded();
}

[XmlRoot("Quotes")]
public class QuoteListXml
{
    [XmlElement("Quote")]
    public List<Quote> Quotes { get; set; } = new();
}

/// <summary>
/// Trash-talk quotes persisted as quotes.xml. Deliberately user-editable: the
/// file is only ever created once (with the default quote set) if it doesn't
/// exist yet, and the app never overwrites or validates its content beyond
/// what deserialization tolerates - an unknown/misspelled Category just means
/// that quote is never picked, never a crash (see QuoteService).
/// </summary>
public class QuoteXmlRepository : IQuoteRepository
{
    private readonly AtomicXmlFileStore<Quote> _store;

    public QuoteXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "quotes.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<Quote>(
            filePath,
            backupDirectory,
            "quotes",
            deserialize: stream => XmlSerialization.Deserialize<QuoteListXml>(stream).Quotes,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new QuoteListXml { Quotes = items }));
    }

    public List<Quote> GetAll() => _store.GetAll();

    /// <summary>Writes the default quote set if quotes.xml doesn't exist yet. Never
    /// touches an existing file, so user edits are always preserved.</summary>
    public void EnsureSeeded()
    {
        if (File.Exists(_store.FilePath)) return;
        _store.Update(_ => DefaultQuotes.All.Select(q => new Quote { Category = q.Category, Text = q.Text }).ToList());
    }
}
