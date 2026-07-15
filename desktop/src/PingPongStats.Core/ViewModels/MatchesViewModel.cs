using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>Dashboard drill-down (R3): restricts the "Spieler"-filtered
/// matches to only that player's wins or only their losses. Independent of
/// <see cref="MatchesViewModel.FilterWinner"/>, which is a separate,
/// pre-existing "any player won" filter with its own UI control.</summary>
public enum MatchResultFilter
{
    All,
    WinsOnly,
    LossesOnly,
}

public partial class MatchesViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;

    [ObservableProperty] private Player? filterPlayer;
    [ObservableProperty] private Player? filterWinner;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private bool sortDescending = true;

    /// <summary>Set together with <see cref="FilterPlayer"/> via
    /// <see cref="SetPlayerResultFilter"/> when navigating here from the
    /// Dashboard ranking's clickable S/N values (R3).</summary>
    [ObservableProperty] private MatchResultFilter resultFilter = MatchResultFilter.All;

    public ObservableCollection<Player> FilterablePlayers { get; } = new();
    public ObservableCollection<MatchRow> Matches { get; } = new();

    public IRelayCommand ApplyFilterCommand { get; }
    public IRelayCommand ClearFilterCommand { get; }
    public IRelayCommand<MatchRow> EditCommand { get; }
    public IRelayCommand<MatchRow> DeleteCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    /// <summary>True while a Dashboard S/N drill-down (R3) is active, so the
    /// Matches page can show a removable filter chip.</summary>
    public bool HasResultFilterChip => FilterPlayer is not null && ResultFilter != MatchResultFilter.All;

    /// <summary>E.g. "Marco · nur Siege" for the removable filter chip (R3).</summary>
    public string ResultFilterChipLabel => HasResultFilterChip
        ? $"{FilterPlayer!.DisplayName} · {(ResultFilter == MatchResultFilter.WinsOnly ? "nur Siege" : "nur Niederlagen")}"
        : string.Empty;

    /// <summary>Raised when the user wants to edit a match; MainViewModel
    /// subscribes to navigate to a MatchEditViewModel for that match.</summary>
    public event Action<Guid>? EditRequested;

    public MatchesViewModel(PingPongDataService dataService, NotificationService notifications)
    {
        _dataService = dataService;
        _notifications = notifications;

        ApplyFilterCommand = new RelayCommand(ApplyFilter);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        EditCommand = new RelayCommand<MatchRow>(row => { if (row is not null) EditRequested?.Invoke(row.Id); });
        DeleteCommand = new RelayCommand<MatchRow>(row => { if (row is not null) Delete(row); });
        RefreshCommand = new RelayCommand(Load);

        Load();
    }

    public void Load()
    {
        FilterablePlayers.Clear();
        foreach (var p in _dataService.Players.OrderBy(p => p.DisplayName)) FilterablePlayers.Add(p);

        ApplyFilter();
    }

    /// <summary>Dashboard drill-down (R3): shows only the given player's
    /// matches, restricted to their wins or their losses.</summary>
    public void SetPlayerResultFilter(Player player, bool winsOnly)
    {
        FilterPlayer = player;
        FilterWinner = null;
        ResultFilter = winsOnly ? MatchResultFilter.WinsOnly : MatchResultFilter.LossesOnly;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        IEnumerable<Match> query = _dataService.Matches;

        if (FilterPlayer is not null)
        {
            query = query.Where(m => m.PlayerAId == FilterPlayer.Id || m.PlayerBId == FilterPlayer.Id);

            if (ResultFilter == MatchResultFilter.WinsOnly)
            {
                query = query.Where(m => m.WinnerId == FilterPlayer.Id);
            }
            else if (ResultFilter == MatchResultFilter.LossesOnly)
            {
                query = query.Where(m => m.WinnerId != FilterPlayer.Id);
            }
        }
        if (FilterWinner is not null)
        {
            query = query.Where(m => m.WinnerId == FilterWinner.Id);
        }
        if (FilterFrom is DateTime from)
        {
            query = query.Where(m => m.PlayedAt >= from.Date);
        }
        if (FilterTo is DateTime to)
        {
            query = query.Where(m => m.PlayedAt <= to.Date.AddDays(1).AddTicks(-1));
        }

        query = SortDescending ? query.OrderByDescending(m => m.PlayedAt) : query.OrderBy(m => m.PlayedAt);

        Matches.Clear();
        foreach (var m in query)
        {
            Matches.Add(new MatchRow
            {
                Match = m,
                PlayerAName = playersById.GetValueOrDefault(m.PlayerAId)?.DisplayName ?? "?",
                PlayerBName = playersById.GetValueOrDefault(m.PlayerBId)?.DisplayName ?? "?",
                WinnerName = playersById.GetValueOrDefault(m.WinnerId)?.DisplayName ?? "?",
            });
        }
    }

    private void ClearFilter()
    {
        FilterPlayer = null;
        FilterWinner = null;
        FilterFrom = null;
        FilterTo = null;
        SortDescending = true;
        ResultFilter = MatchResultFilter.All;
        ApplyFilter();
    }

    partial void OnFilterPlayerChanged(Player? value)
    {
        OnPropertyChanged(nameof(HasResultFilterChip));
        OnPropertyChanged(nameof(ResultFilterChipLabel));
    }

    partial void OnResultFilterChanged(MatchResultFilter value)
    {
        OnPropertyChanged(nameof(HasResultFilterChip));
        OnPropertyChanged(nameof(ResultFilterChipLabel));
    }

    private void Delete(MatchRow row)
    {
        try
        {
            _dataService.DeleteMatch(row.Id);
            _notifications.NotifySuccess("Spiel wurde gelöscht.");
            Load();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("MatchesViewModel.Delete failed", ex);
        }
    }
}
