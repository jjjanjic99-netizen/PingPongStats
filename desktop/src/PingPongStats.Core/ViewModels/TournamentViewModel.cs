using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>Raised when the user clicks a playable bracket slot, so MainViewModel
/// can open the (locked, pre-filled) match/doubles entry form for it.</summary>
public record TournamentPlayRequest(Guid TournamentId, TournamentMatchSlot Slot, TournamentMode Mode);

/// <summary>"Turnier" page: setup (mode, participants, teams) before a tournament
/// starts, the live bracket while one is in progress, and a browsable history of
/// completed/aborted tournaments afterward.</summary>
public partial class TournamentViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;
    private readonly ISettingsRepository _settingsRepository;

    public string DataPath => _settingsRepository.Load().DataPath;

    // ----- Setup (only relevant while no tournament is in progress) --------
    [ObservableProperty] private string newTournamentName = string.Empty;
    [ObservableProperty] private bool isDoublesMode;
    [ObservableProperty] private string setupErrorMessage = string.Empty;

    public ObservableCollection<TournamentParticipantOption> AvailableParticipants { get; } = new();
    public ObservableCollection<TournamentTeamSlot> Teams { get; } = new();

    // ----- Active tournament / bracket display ------------------------------
    [ObservableProperty] private bool hasActiveTournament;
    [ObservableProperty] private Tournament? activeTournamentModel;
    [ObservableProperty] private Tournament? viewingTournament;
    [ObservableProperty] private string viewingTournamentLabel = string.Empty;

    public ObservableCollection<TournamentRoundGroup> BracketRounds { get; } = new();
    public ObservableCollection<TournamentSummaryRow> CompletedTournaments { get; } = new();

    public IRelayCommand StartTournamentCommand { get; }
    public IRelayCommand GenerateTeamsCommand { get; }
    public IRelayCommand DrawRandomTeamsCommand { get; }
    public IRelayCommand<TournamentSlotRow> PlaySlotCommand { get; }
    public IRelayCommand<TournamentSlotRow> RequestBetCommand { get; }
    public IRelayCommand AbortTournamentCommand { get; }
    public IRelayCommand<Tournament> ViewTournamentCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    /// <summary>Raised when the user clicks a playable bracket slot.</summary>
    public event Action<TournamentPlayRequest>? PlayRequested;

    /// <summary>Raised after "Tippen" ensures a PendingMatch exists for a
    /// playable bracket slot (Phase 15), so MainViewModel can navigate to the
    /// "Tippspiel" page to actually place the tip.</summary>
    public event Action? BetRequested;

    public TournamentViewModel(PingPongDataService dataService, NotificationService notifications, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _notifications = notifications;
        _settingsRepository = settingsRepository;

        StartTournamentCommand = new RelayCommand(StartTournament);
        GenerateTeamsCommand = new RelayCommand(() => BuildTeamsFromSelection(shuffle: false));
        DrawRandomTeamsCommand = new RelayCommand(() => BuildTeamsFromSelection(shuffle: true));
        PlaySlotCommand = new RelayCommand<TournamentSlotRow>(row => { if (row is not null) RequestPlaySlot(row); });
        RequestBetCommand = new RelayCommand<TournamentSlotRow>(row => { if (row is not null) RequestBet(row); });
        AbortTournamentCommand = new RelayCommand(AbortActiveTournament);
        ViewTournamentCommand = new RelayCommand<Tournament>(t => { if (t is not null) ViewingTournament = t; });
        RefreshCommand = new RelayCommand(Load);

        Load();
    }

    partial void OnViewingTournamentChanged(Tournament? value) => RebuildBracketDisplay();

    public void Load()
    {
        var previouslySelected = AvailableParticipants.Where(o => o.IsSelected).Select(o => o.Player.Id).ToHashSet();
        AvailableParticipants.Clear();
        foreach (var player in _dataService.Players.Where(p => p.IsActive).OrderBy(p => p.DisplayName))
        {
            AvailableParticipants.Add(new TournamentParticipantOption { Player = player, IsSelected = previouslySelected.Contains(player.Id) });
        }

        var active = _dataService.ActiveTournament;
        HasActiveTournament = active is not null;
        ActiveTournamentModel = active;

        CompletedTournaments.Clear();
        foreach (var t in _dataService.Tournaments.Where(t => t.Status != TournamentStatus.InProgress).OrderByDescending(t => t.CreatedAt))
        {
            CompletedTournaments.Add(new TournamentSummaryRow { Tournament = t, WinnerLabel = BuildWinnerLabel(t) });
        }

        if (active is not null)
        {
            ViewingTournament = active;
        }
        else
        {
            // Reload() always deserializes fresh Tournament instances, so re-resolve by
            // Id rather than holding onto the (now stale) previous reference.
            var previousId = ViewingTournament?.Id;
            var stillExists = previousId is Guid id ? _dataService.Tournaments.FirstOrDefault(t => t.Id == id) : null;
            ViewingTournament = stillExists ?? (CompletedTournaments.Count > 0 ? CompletedTournaments[0].Tournament : null);
        }
    }

    private string BuildWinnerLabel(Tournament tournament)
    {
        if (tournament.Status != TournamentStatus.Completed || tournament.WinnerEntrantId is null) return string.Empty;
        var winnerEntrant = tournament.Entrants.FirstOrDefault(e => e.Id == tournament.WinnerEntrantId);
        if (winnerEntrant is null) return string.Empty;

        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        var names = new List<string>();
        if (playersById.TryGetValue(winnerEntrant.Player1Id, out var p1)) names.Add(p1.DisplayName);
        if (winnerEntrant.Player2Id is Guid p2Id && playersById.TryGetValue(p2Id, out var p2)) names.Add(p2.DisplayName);
        return string.Join(" & ", names);
    }

    private void RebuildBracketDisplay()
    {
        BracketRounds.Clear();
        var tournament = ViewingTournament;
        ViewingTournamentLabel = tournament is null ? string.Empty : $"{tournament.Name} ({(tournament.Mode == TournamentMode.Doubles ? "Doppel" : "Einzel")})";
        if (tournament is null) return;

        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        var entrantsById = tournament.Entrants.ToDictionary(e => e.Id);
        var totalRounds = tournament.Bracket.Count == 0 ? 0 : tournament.Bracket.Max(s => s.Round);

        TournamentEntrantDisplay Resolve(Guid? entrantId, bool isBye)
        {
            if (isBye) return new TournamentEntrantDisplay { IsBye = true };
            if (entrantId is not Guid id || !entrantsById.TryGetValue(id, out var entrant))
            {
                return new TournamentEntrantDisplay();
            }

            var players = new List<Player>();
            if (playersById.TryGetValue(entrant.Player1Id, out var p1)) players.Add(p1);
            if (entrant.Player2Id is Guid p2Id && playersById.TryGetValue(p2Id, out var p2)) players.Add(p2);
            return new TournamentEntrantDisplay { EntrantId = id, Players = players };
        }

        foreach (var roundGroup in tournament.Bracket.GroupBy(s => s.Round).OrderBy(g => g.Key))
        {
            var slots = roundGroup.OrderBy(s => s.PositionInRound).Select(slot =>
            {
                var entrantA = Resolve(slot.EntrantAId, slot.EntrantAIsBye);
                var entrantB = Resolve(slot.EntrantBId, slot.EntrantBIsBye);
                var winnerLabel = slot.WinnerEntrantId is null
                    ? string.Empty
                    : slot.WinnerEntrantId == entrantA.EntrantId
                        ? entrantA.Label
                        : slot.WinnerEntrantId == entrantB.EntrantId ? entrantB.Label : string.Empty;

                return new TournamentSlotRow { Slot = slot, EntrantA = entrantA, EntrantB = entrantB, WinnerLabel = winnerLabel };
            }).ToList();

            BracketRounds.Add(new TournamentRoundGroup { Round = roundGroup.Key, RoundLabel = RoundLabel(roundGroup.Key, totalRounds), Slots = slots });
        }
    }

    private static string RoundLabel(int round, int totalRounds) => (totalRounds - round) switch
    {
        0 => "Finale",
        1 => "Halbfinale",
        2 => "Viertelfinale",
        _ => $"Runde {round}",
    };

    private void BuildTeamsFromSelection(bool shuffle)
    {
        SetupErrorMessage = string.Empty;
        var selected = AvailableParticipants.Where(o => o.IsSelected).Select(o => o.Player).ToList();
        if (selected.Count < 4 || selected.Count % 2 != 0)
        {
            SetupErrorMessage = "Für ein Doppel-Turnier wird eine gerade Anzahl Spieler (mind. 4) benötigt.";
            return;
        }

        var pairs = shuffle
            ? TournamentService.DrawRandomTeams(selected.Select(p => p.Id).ToList())
            : SequentialPairs(selected);

        var playersById = selected.ToDictionary(p => p.Id);
        Teams.Clear();
        foreach (var (player1Id, player2Id) in pairs)
        {
            Teams.Add(new TournamentTeamSlot { Player1 = playersById[player1Id], Player2 = playersById[player2Id] });
        }
    }

    private static List<(Guid Player1Id, Guid Player2Id)> SequentialPairs(List<Player> players)
    {
        var pairs = new List<(Guid, Guid)>();
        for (var i = 0; i < players.Count; i += 2)
        {
            pairs.Add((players[i].Id, players[i + 1].Id));
        }

        return pairs;
    }

    private void StartTournament()
    {
        SetupErrorMessage = string.Empty;
        try
        {
            if (IsDoublesMode)
            {
                if (Teams.Count < 2)
                {
                    SetupErrorMessage = "Mindestens 2 Teams nötig - zuerst Teams erzeugen oder auslosen.";
                    return;
                }

                var teamPairs = new List<(Guid, Guid)>();
                var usedPlayerIds = new HashSet<Guid>();
                foreach (var team in Teams)
                {
                    if (team.Player1 is null || team.Player2 is null)
                    {
                        SetupErrorMessage = "Jedes Team benötigt zwei Spieler.";
                        return;
                    }

                    if (team.Player1.Id == team.Player2.Id || !usedPlayerIds.Add(team.Player1.Id) || !usedPlayerIds.Add(team.Player2.Id))
                    {
                        SetupErrorMessage = "Jeder Spieler darf nur in einem Team vorkommen.";
                        return;
                    }

                    teamPairs.Add((team.Player1.Id, team.Player2.Id));
                }

                _dataService.CreateDoublesTournament(NewTournamentName, teamPairs);
            }
            else
            {
                var selectedIds = AvailableParticipants.Where(o => o.IsSelected).Select(o => o.Player.Id).ToList();
                if (selectedIds.Count < BracketService.MinEntrants)
                {
                    SetupErrorMessage = $"Mindestens {BracketService.MinEntrants} Teilnehmer nötig.";
                    return;
                }

                _dataService.CreateSinglesTournament(NewTournamentName, selectedIds);
            }

            _notifications.NotifySuccess("Turnier wurde gestartet.");
            NewTournamentName = string.Empty;
            Teams.Clear();
            Load();
        }
        catch (ValidationException ex)
        {
            SetupErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            SetupErrorMessage = "Unerwarteter Fehler: " + ex.Message;
            Logger.Error("TournamentViewModel.StartTournament failed", ex);
        }
    }

    private void RequestPlaySlot(TournamentSlotRow row)
    {
        if (!row.IsPlayable || ActiveTournamentModel is null) return;
        PlayRequested?.Invoke(new TournamentPlayRequest(ActiveTournamentModel.Id, row.Slot, ActiveTournamentModel.Mode));
    }

    private void RequestBet(TournamentSlotRow row)
    {
        if (!row.IsPlayable || ActiveTournamentModel is null) return;

        try
        {
            _dataService.GetOrCreatePendingMatchForSlot(ActiveTournamentModel.Id, row.Slot.Id);
            BetRequested?.Invoke();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("TournamentViewModel.RequestBet failed", ex);
        }
    }

    private void AbortActiveTournament()
    {
        if (ActiveTournamentModel is null) return;

        try
        {
            _dataService.AbortTournament(ActiveTournamentModel.Id);
            _notifications.NotifySuccess("Turnier wurde abgebrochen.");
            Load();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("TournamentViewModel.AbortActiveTournament failed", ex);
        }
    }
}
