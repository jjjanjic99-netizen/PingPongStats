using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class PlayersViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;
    private List<PlayerRow> _allRows = new();
    private Guid? _editingPlayerId;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool showInactive = true;
    [ObservableProperty] private bool isEditFormOpen;
    [ObservableProperty] private string editDisplayName = string.Empty;
    [ObservableProperty] private string editFirstName = string.Empty;
    [ObservableProperty] private string editLastName = string.Empty;
    [ObservableProperty] private string editEmail = string.Empty;
    [ObservableProperty] private string editErrorMessage = string.Empty;
    [ObservableProperty] private string editFormTitle = string.Empty;

    public ObservableCollection<PlayerRow> Players { get; } = new();

    public IRelayCommand NewPlayerCommand { get; }
    public IRelayCommand<PlayerRow> EditPlayerCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IRelayCommand<PlayerRow> ToggleActiveCommand { get; }
    public IRelayCommand<PlayerRow> DeleteCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public PlayersViewModel(PingPongDataService dataService, NotificationService notifications)
    {
        _dataService = dataService;
        _notifications = notifications;

        NewPlayerCommand = new RelayCommand(BeginCreate);
        EditPlayerCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) BeginEdit(row); });
        SaveCommand = new RelayCommand(Save);
        CancelEditCommand = new RelayCommand(() => IsEditFormOpen = false);
        ToggleActiveCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) ToggleActive(row); });
        DeleteCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) Delete(row); });
        RefreshCommand = new RelayCommand(Load);

        Load();
    }

    public void Load()
    {
        var players = _dataService.Players;
        var matches = _dataService.Matches.ToList();
        var eloRatings = EloService.ComputeRatings(matches, players.Select(p => p.Id));

        _allRows = players
            .Select(p => new PlayerRow
            {
                Player = p,
                Stats = BuildSummary(p, matches, eloRatings),
            })
            .OrderBy(r => r.DisplayName)
            .ToList();

        ApplyFilter();
    }

    private static PlayerStatsSummary BuildSummary(Player player, List<Match> matches, Dictionary<Guid, double> eloRatings)
    {
        var overall = StatsService.GetOverallRecord(matches, player.Id);
        var last30d = StatsService.GetWinRateLastNDays(matches, player.Id);

        return new PlayerStatsSummary(
            player.Id, player.DisplayName, player.IsActive,
            overall.Played, overall.Wins, overall.Losses, overall.WinRatePct, last30d.WinRatePct,
            StatsService.GetCurrentStreak(matches, player.Id),
            StatsService.GetLongestWinStreak(matches, player.Id),
            StatsService.GetAverageSetsWonPerMatch(matches, player.Id),
            eloRatings.GetValueOrDefault(player.Id, EloService.DefaultInitialRating),
            StatsService.GetRecentForm(matches, player.Id, 5),
            StatsService.IsLowSampleSize(overall.Played));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnShowInactiveChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = _allRows.AsEnumerable();
        if (!ShowInactive) query = query.Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(r => r.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Players.Clear();
        foreach (var row in query) Players.Add(row);
    }

    private void BeginCreate()
    {
        _editingPlayerId = null;
        EditFormTitle = "Neuer Spieler";
        EditDisplayName = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditEmail = string.Empty;
        EditErrorMessage = string.Empty;
        IsEditFormOpen = true;
    }

    private void BeginEdit(PlayerRow row)
    {
        _editingPlayerId = row.Id;
        EditFormTitle = "Spieler bearbeiten";
        EditDisplayName = row.Player.DisplayName;
        EditFirstName = row.Player.FirstName;
        EditLastName = row.Player.LastName;
        EditEmail = row.Player.Email;
        EditErrorMessage = string.Empty;
        IsEditFormOpen = true;
    }

    private void Save()
    {
        try
        {
            if (_editingPlayerId is Guid id)
            {
                _dataService.UpdatePlayer(id, EditDisplayName, EditFirstName, EditLastName, EditEmail);
                _notifications.NotifySuccess("Spieler wurde aktualisiert.");
            }
            else
            {
                _dataService.CreatePlayer(EditDisplayName, EditFirstName, EditLastName, EditEmail);
                _notifications.NotifySuccess("Spieler wurde angelegt.");
            }

            IsEditFormOpen = false;
            Load();
        }
        catch (ValidationException ex)
        {
            EditErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            EditErrorMessage = "Unerwarteter Fehler: " + ex.Message;
            Logger.Error("PlayersViewModel.Save failed", ex);
        }
    }

    private void ToggleActive(PlayerRow row)
    {
        try
        {
            _dataService.SetPlayerActive(row.Id, !row.IsActive);
            _notifications.NotifySuccess(row.IsActive ? "Spieler wurde archiviert." : "Spieler wurde reaktiviert.");
            Load();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("PlayersViewModel.ToggleActive failed", ex);
        }
    }

    private void Delete(PlayerRow row)
    {
        try
        {
            _dataService.DeletePlayer(row.Id);
            _notifications.NotifySuccess("Spieler wurde gelöscht.");
            Load();
        }
        catch (ValidationException ex)
        {
            _notifications.NotifyError(ex.Message);
        }
        catch (Exception ex)
        {
            _notifications.NotifyError("Unerwarteter Fehler: " + ex.Message);
            Logger.Error("PlayersViewModel.Delete failed", ex);
        }
    }
}
