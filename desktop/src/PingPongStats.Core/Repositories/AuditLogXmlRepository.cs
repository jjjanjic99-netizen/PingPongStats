using System.Xml.Serialization;
using PingPongStats.Core.Models;
using System.IO;

namespace PingPongStats.Core.Repositories;

public interface IAuditLogRepository
{
    List<AuditLogEntry> GetAll();
    void Append(AuditLogEntry entry);
    void EnsureFileExists();
}

[XmlRoot("AuditLog")]
public class AuditLogListXml
{
    [XmlElement("Entry")]
    public List<AuditLogEntry> Entries { get; set; } = new();
}

/// <summary>Optional append-only audit trail persisted as audit-log.xml.</summary>
public class AuditLogXmlRepository : IAuditLogRepository
{
    private readonly AtomicXmlFileStore<AuditLogEntry> _store;

    public AuditLogXmlRepository(string dataPath)
    {
        var filePath = Path.Combine(dataPath, "audit-log.xml");
        var backupDirectory = Path.Combine(dataPath, "backups");
        _store = new AtomicXmlFileStore<AuditLogEntry>(
            filePath,
            backupDirectory,
            "audit-log",
            deserialize: stream => XmlSerialization.Deserialize<AuditLogListXml>(stream).Entries,
            serialize: (stream, items) => XmlSerialization.Serialize(stream, new AuditLogListXml { Entries = items }));
    }

    public List<AuditLogEntry> GetAll() => _store.GetAll();

    public void Append(AuditLogEntry entry)
    {
        _store.Update(entries =>
        {
            entries.Add(entry);
            return entries;
        });
    }

    public void EnsureFileExists() => _store.EnsureFileExists();
}
