namespace PingPongStats.Core.Models;

/// <summary>Optional append-only audit trail entry for data-changing actions.</summary>
public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
