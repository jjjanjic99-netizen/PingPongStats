namespace PingPongStats.Core.Services;

/// <summary>User-facing validation failure (invalid match data, unknown player, etc.).
/// The message is always safe to show directly in a dialog.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
