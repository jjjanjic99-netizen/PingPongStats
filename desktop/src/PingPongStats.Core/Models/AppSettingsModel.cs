namespace PingPongStats.Core.Models;

/// <summary>
/// Per-user local configuration, persisted as JSON under
/// %AppData%\PingPongStats\appsettings.json. Distinct from the shared XML
/// data files, which live at <see cref="DataPath"/> and may be a network share.
/// </summary>
public class AppSettingsModel
{
    public string DataPath { get; set; } = string.Empty;
    public bool DarkMode { get; set; }

    /// <summary>One of "Small", "Medium", "Large". Controls the app-wide UI scale
    /// (see PingPongStats.App.UiScaleManager). Defaults to "Medium".</summary>
    public string UiScale { get; set; } = "Medium";

    /// <summary>Whether to show the fullscreen confetti/win overlay after saving a
    /// match. Defaults to on.</summary>
    public bool ShowWinAnimation { get; set; } = true;
}
