namespace PingPongStats.Core.ViewModels;

/// <summary>
/// Simple pub/sub used by screen ViewModels to surface success/error banners
/// without each of them needing a direct reference to the main window.
/// </summary>
public class NotificationService
{
    public event Action<string, bool>? Notified; // (message, isError)

    public void NotifySuccess(string message) => Notified?.Invoke(message, false);

    public void NotifyError(string message) => Notified?.Invoke(message, true);
}
