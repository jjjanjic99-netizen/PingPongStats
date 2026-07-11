using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>
/// Combined "Doppel" screen: dashboard-style team-pairing analysis (which
/// constellation of two players performs best together), a chronological
/// list of recorded doubles matches, and an inline form to record a new one -
/// mirroring the PlayersViewModel's list+inline-form pattern.
/// </summary>
public partial class DoublesViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;

    public DashboardRangeFilter RangeFilter { get; }

    public DashboardRangeOption[] RangeOptions => DashboardRangeFilter.Options;

    public IReadOnlyList<QuickResultOption> QuickResults => MatchEditViewModel.QuickResultOptions;

    [ObservableProperty] private int totalDoubleMatches;
    [ObservableProperty] private string bestPairingLabel = "–";
    [ObservableProperty] private bool hasData;

    public ObservableCollection<TeamPairingRow> Rankings { get; } = new();
    public ObservableCollection<DoubleMatchRow> Matches { get; } = new();
    public ObservableCollection<ChartBarItem> PairingWinRateChart { get; } = new();
    public ObservableCollection<Player> AvailablePlayers { get; } = new();

    /// <summary>Optional set-by-set score entry. Empty = no set detail recorded.</summary>
    public ObservableCollection<SetResultEntryViewModel> SetEntries { get; } = new();

    [ObservableProperty] private bool isFormOpen;
    [ObservableProperty] private DateTime playedAtDate = DateTime.Today;
    [ObservableProperty] private string playedAtTime = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private Player? teamAPlayer1;
    [ObservableProperty] private Player? teamAPlayer2;
    [ObservableProperty] private Player? teamBPlayer1;
    [ObservableProperty] private Player? teamBPlayer2;
    [ObservableProperty] private int teamASets;
    [ObservableProperty] private int teamBSets;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string winnerSide = "A";
    [ObservableProperty] private string formErrorMessage = string.Empty;

    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand NewMatchCommand { get; }
    public IRelayCommand CancelFormCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand<QuickResultOption> ApplyQuickResultCommand { get; }
    public IRelayCommand<DoubleMatchRow> DeleteCommand { get; }
    public IRelayCommand AddSetCommand { get; }
    public IRelayCommand<SetResultEntryViewModel> RemoveSetCommand { get; }

    public DoublesViewModel(
        PingPongDataService dataService, NotificationService notifications, DashboardRangeFilter rangeFilter)
    {
        _dataService = dataService;
        _notifications = notifications;
        RangeFilter = rangeFilter;
        RangeFilter.Changed += Load;

        RefreshCommand = new RelayCommand(Load);
        NewMatchCommand = new RelayCommand(BeginCreate);
        CancelFormCommand = new RelayCommand(() => IsFormOpen = false);
        SaveCommand = new RelayCommand(Save);
        ApplyQuickResultCommand = new RelayCommand<QuickResultOption>(option => { if (option is not null) ApplyQuickResult(option); });
        DeleteCommand = new RelayCommand<DoubleMatchRow>(row => { if (row is not null) Delete(row); });
        AddSetCommand = new RelayCommand(() => SetEntries.Add(new SetResultEntryViewModel { SetNumber = SetEntries.Count + 1 }));
        RemoveSetCommand = new RelayCommand<SetResultEntryViewModel>(entry => { if (entry is not null) RemoveSet(entry); });

        Load();
    }

    private void RemoveSet(SetResultEntryViewModel entry)
    {
        SetEntries.Remove(entry);
        for (var i = 0; i < SetEntries.Count; i++)
        {
            SetEntries[i].SetNumber = i + 1;
        }
    }

    public void Load()
    {
        AvailablePlayers.Clear();
        foreach (var p in _dataService.Players.Where(p => p.IsActive).OrderBy(p => p.DisplayName))
        {
            AvailablePlayers.Add(p);
        }

        var now = DateTime.Now;
        var from = RangeFilter.GetFromDate(now);
        var matchesInRange = from is null
            ? _dataService.DoubleMatches.ToList()
            : _dataService.DoubleMatches.Where(m => m.PlayedAt >= from.Value).ToList();

        TotalDoubleMatches = matchesInRange.Count;
        HasData = matchesInRange.Count > 0;

        var playersById = _dataService.Players.ToDictionary(p => p.Id);

        var rankings = DoublesStatsService.GetPairingRankings(matchesInRange);
        Rankings.Clear();
        foreach (var r in rankings)
        {
            Rankings.Add(new TeamPairingRow
            {
                Stats = r,
                Player1Name = playersById.GetValueOrDefault(r.Player1Id)?.DisplayName ?? "?",
                Player2Name = playersById.GetValueOrDefault(r.Player2Id)?.DisplayName ?? "?",
            });
        }

        BestPairingLabel = Rankings.Count == 0 ? "–" : $"{Rankings[0].PairLabel} ({Rankings[0].WinRateLabel})";

        PairingWinRateChart.Clear();
        foreach (var r in Rankings.Take(10))
        {
            var normalized = r.Stats.WinRatePct / 100.0;
            PairingWinRateChart.Add(new ChartBarItem(r.PairLabel, r.Stats.WinRatePct, r.WinRateLabel, normalized));
        }

        Matches.Clear();
        foreach (var m in matchesInRange.OrderByDescending(m => m.PlayedAt))
        {
            var teamALabel = TeamLabel(m.TeamAPlayer1Id, m.TeamAPlayer2Id, playersById);
            var teamBLabel = TeamLabel(m.TeamBPlayer1Id, m.TeamBPlayer2Id, playersById);
            Matches.Add(new DoubleMatchRow
            {
                Match = m,
                TeamALabel = teamALabel,
                TeamBLabel = teamBLabel,
                WinnerLabel = m.WinningTeam == "A" ? teamALabel : teamBLabel,
            });
        }
    }

    private static string TeamLabel(Guid player1Id, Guid player2Id, Dictionary<Guid, Player> playersById)
    {
        var name1 = playersById.GetValueOrDefault(player1Id)?.DisplayName ?? "?";
        var name2 = playersById.GetValueOrDefault(player2Id)?.DisplayName ?? "?";
        return $"{name1} & {name2}";
    }

    private void BeginCreate()
    {
        PlayedAtDate = DateTime.Today;
        PlayedAtTime = DateTime.Now.ToString("HH:mm");
        TeamAPlayer1 = null;
        TeamAPlayer2 = null;
        TeamBPlayer1 = null;
        TeamBPlayer2 = null;
        TeamASets = 0;
        TeamBSets = 0;
        Notes = string.Empty;
        WinnerSide = "A";
        FormErrorMessage = string.Empty;
        SetEntries.Clear();
        IsFormOpen = true;
    }

    private void ApplyQuickResult(QuickResultOption option)
    {
        if (WinnerSide == "A")
        {
            TeamASets = option.WinnerSets;
            TeamBSets = option.LoserSets;
        }
        else
        {
            TeamBSets = option.WinnerSets;
            TeamASets = option.LoserSets;
        }
    }

    private void Save()
    {
        FormErrorMessage = string.Empty;

        if (TeamAPlayer1 is null || TeamAPlayer2 is null || TeamBPlayer1 is null || TeamBPlayer2 is null)
        {
            FormErrorMessage = "Bitte alle vier Spieler auswählen.";
            return;
        }

        if (!TimeSpan.TryParse(PlayedAtTime, out var timeOfDay))
        {
            FormErrorMessage = "Ungültige Uhrzeit. Format: HH:mm.";
            return;
        }

        var playedAt = DateTime.SpecifyKind(PlayedAtDate.Date + timeOfDay, DateTimeKind.Unspecified);
        var setResults = SetEntries.Count == 0 ? null : SetEntries.Select(e => e.ToModel()).ToList();

        try
        {
            _dataService.CreateDoubleMatch(
                playedAt, TeamAPlayer1.Id, TeamAPlayer2.Id, TeamBPlayer1.Id, TeamBPlayer2.Id,
                TeamASets, TeamBSets, Notes, setResults);

            _notifications.NotifySuccess("Doppel-Spiel wurde erfasst.");
            IsFormOpen = false;
            Load();
        }
        catch (ValidationException ex)
        {
            FormErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            FormErrorMessage = "Unerwarteter Fehler: " + ex.Message;
            Logger.Error("DoublesViewModel.Save failed", ex);
        }
    }

    private void Delete(DoubleMatchRow row)
    {
        try
        {
            _dataService.DeleteDoubleMatch(row.Id);
            _notifications.NotifySuccess("Doppel-Spiel wurde gelöscht.");
            Load();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("DoublesViewModel.Delete failed", ex);
        }
    }
}
