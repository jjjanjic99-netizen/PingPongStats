namespace PingPongStats.Core.Helpers;

/// <summary>
/// Central source of "now" timestamps. Always returns DateTimeKind.Unspecified
/// so XmlSerializer emits a clean "yyyy-MM-ddTHH:mm:ss" without a UTC/local
/// offset suffix, matching the documented XML sample format.
/// </summary>
public static class Clock
{
    public static DateTime Now() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
}
