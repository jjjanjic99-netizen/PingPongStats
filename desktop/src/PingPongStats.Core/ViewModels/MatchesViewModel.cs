using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class MatchesViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;

    [ObservableProperty] private Player? filterPlayer;
    [ObservableProperty] private Player? filterWinner;
    [ObservableProperty] private DateTime? filterFrom;
    [ObservableProperty] private DateTime? filterTo;
    [ObservableProperty] private bool sortDescending = true;

    public ObservableCollection<Player> FilterablePlayers { get; } = new();
    public ObservableCollection<MatchRow> Matches { get; } = new();

    public IRelayCommand ApplyFilterCommand { get; }
    public IRelayCommand ClearFilterCommand { get; }
    public IRelayCommand<MatchRow> EditCommand { get; }
    public IRelayCommand<MatchRow> DeleteCommand { get; }
    public IRelayCommand RefreshCommand { get; }

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

    private void ApplyFilter()
    {
        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        IEnumerable<Match> query = _dataService.Matches;

        if (FilterPlayer is not null)
        {
            query = query.Where(m => m.PlayerAId == FilterPlayer.Id || m.PlayerBId == FilterPlayer.Id);
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
        ApplyFilter();
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
