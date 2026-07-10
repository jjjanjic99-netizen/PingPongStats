using CommunityToolkit.Mvvm.ComponentModel;

namespace PingPongStats.Core.ViewModels;

public enum DashboardRangePreset
{
    Last7Days,
    Last30Days,
    Last90Days,
    AllTime,
}

public record DashboardRangeOption(DashboardRangePreset Preset, string Label);

/// <summary>
/// A single shared time-range selection used by every dashboard section
/// (singles dashboard and doubles dashboard). One instance is created in
/// MainViewModel and passed to both, so changing the range on either page
/// keeps both in sync.
/// </summary>
public partial class DashboardRangeFilter : ObservableObject
{
    public static readonly DashboardRangeOption[] Options =
    {
        new(DashboardRangePreset.Last7Days, "Letzte 7 Tage"),
        new(DashboardRangePreset.Last30Days, "Letzte 30 Tage"),
        new(DashboardRangePreset.Last90Days, "Letzte 90 Tage"),
        new(DashboardRangePreset.AllTime, "Gesamter Zeitraum"),
    };

    [ObservableProperty] private DashboardRangePreset selectedPreset = DashboardRangePreset.AllTime;

    /// <summary>Raised whenever the selection changes, so any dashboard ViewModel
    /// currently alive can reload its data with the new range.</summary>
    public event Action? Changed;

    partial void OnSelectedPresetChanged(DashboardRangePreset value) => Changed?.Invoke();

    /// <summary>Inclusive start date for the current selection, or null for "all time".</summary>
    public DateTime? GetFromDate(DateTime referenceNow) => SelectedPreset switch
    {
        DashboardRangePreset.Last7Days => referenceNow.Date.AddDays(-6),
        DashboardRangePreset.Last30Days => referenceNow.Date.AddDays(-29),
        DashboardRangePreset.Last90Days => referenceNow.Date.AddDays(-89),
        _ => null,
    };
}
