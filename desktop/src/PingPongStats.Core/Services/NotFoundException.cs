namespace PingPongStats.Core.Services;

/// <summary>Thrown when a requested Player/Match id no longer exists (e.g. deleted
/// by another user in the meantime). Message is safe to show directly.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
