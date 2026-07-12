using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>"Tippspiel" page (Phase 15): announce upcoming pairings, let
/// logged-in players tip the winner for points (never real money), and browse
/// the tip leaderboard. Betting is points-only - see the README for the
/// explicit "no real money" note.</summary>
public partial class BettingViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;
    private readonly ISettingsRepository _settingsRepository;
    private Guid? _currentPlayerId;

    public string DataPath => _settingsRepository.Load().DataPath;

    [ObservableProperty] private bool isLoggedIn;
    [ObservableProperty] private bool isDoublesMode;
    [ObservableProperty] private bool hasOpenPendingMatches;
    [ObservableProperty] private Player? newPlayerA;
    [ObservableProperty] private Player? newPlayerB;
    [ObservableProperty] private Player? newTeamAPlayer1;
    [ObservableProperty] private Player? newTeamAPlayer2;
    [ObservableProperty] private Player? newTeamBPlayer1;
    [ObservableProperty] private Player? newTeamBPlayer2;
    [ObservableProperty] private string setupErrorMessage = string.Empty;

    public ObservableCollection<Player> AvailablePlayers { get; } = new();
    public ObservableCollection<PendingMatchRow> OpenPendingMatches { get; } = new();
    public ObservableCollection<BettingLeaderboardDisplayRow> Leaderboard { get; } = new();

    public IRelayCommand CreatePendingMatchCommand { get; }
    public IRelayCommand<BetPickOption> PlaceBetCommand { get; }
    public IRelayCommand<PendingMatchRow> RecordResultCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    /// <summary>Raised when the user clicks "Ergebnis erfassen" on a standalone
    /// (non-tournament) pending match, so MainViewModel can open the (locked,
    /// pre-filled) match/doubles entry form for it.</summary>
    public event Action<PendingMatch>? RecordResultRequested;

    public BettingViewModel(PingPongDataService dataService, NotificationService notifications, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _notifications = notifications;
        _settingsRepository = settingsRepository;

        CreatePendingMatchCommand = new RelayCommand(CreatePendingMatch);
        PlaceBetCommand = new RelayCommand<BetPickOption>(option => { if (option is not null) PlaceBet(option); });
        RecordResultCommand = new RelayCommand<PendingMatchRow>(row => { if (row is not null) RecordResultRequested?.Invoke(row.PendingMatch); });
        RefreshCommand = new RelayCommand(Load);

        Load();
    }

    /// <summary>Re-targets this ViewModel at the currently logged-in player (or
    /// null after logout) so it knows who is placing a tip and can block
    /// self-bets in the UI, mirroring ProfileViewModel.SetPlayer.</summary>
    public void SetCurrentPlayer(Guid? playerId)
    {
        _currentPlayerId = playerId;
        IsLoggedIn = playerId is not null;
        Load();
    }

    public void Load()
    {
        AvailablePlayers.Clear();
        foreach (var p in _dataService.Players.Where(p => p.IsActive).OrderBy(p => p.DisplayName))
        {
            AvailablePlayers.Add(p);
        }

        var playersById = _dataService.Players.ToDictionary(p => p.Id);

        OpenPendingMatches.Clear();
        foreach (var pendingMatch in _dataService.PendingMatches.Where(pm => !pm.IsResolved).OrderBy(pm => pm.CreatedAt))
        {
            OpenPendingMatches.Add(BuildRow(pendingMatch, playersById));
        }

        HasOpenPendingMatches = OpenPendingMatches.Count > 0;

        Leaderboard.Clear();
        var rank = 1;
        foreach (var row in BettingService.GetLeaderboard(_dataService.Bets))
        {
            Leaderboard.Add(new BettingLeaderboardDisplayRow { Row = row, Player = playersById.GetValueOrDefault(row.PlayerId), Rank = rank });
            rank++;
        }
    }

    private PendingMatchRow BuildRow(PendingMatch pendingMatch, Dictionary<Guid, Player> playersById)
    {
        string Name(Guid id) => playersById.GetValueOrDefault(id)?.DisplayName ?? "?";

        var currentBet = _currentPlayerId is Guid bettorId
            ? _dataService.Bets.FirstOrDefault(b => b.PendingMatchId == pendingMatch.Id && b.BettorPlayerId == bettorId)
            : null;

        string label;
        List<BetPickOption> options;

        if (pendingMatch.Mode == TournamentMode.Doubles)
        {
            var teamALabel = $"{Name(pendingMatch.TeamAPlayer1Id!.Value)} & {Name(pendingMatch.TeamAPlayer2Id!.Value)}";
            var teamBLabel = $"{Name(pendingMatch.TeamBPlayer1Id!.Value)} & {Name(pendingMatch.TeamBPlayer2Id!.Value)}";
            label = $"{teamALabel} vs {teamBLabel}";
            options = new List<BetPickOption>
            {
                new()
                {
                    PendingMatchId = pendingMatch.Id, PredictedWinningTeam = "A", Label = teamALabel,
                    IsCurrentPick = currentBet?.PredictedWinningTeam == "A",
                },
                new()
                {
                    PendingMatchId = pendingMatch.Id, PredictedWinningTeam = "B", Label = teamBLabel,
                    IsCurrentPick = currentBet?.PredictedWinningTeam == "B",
                },
            };
        }
        else
        {
            var nameA = Name(pendingMatch.PlayerAId!.Value);
            var nameB = Name(pendingMatch.PlayerBId!.Value);
            label = $"{nameA} vs {nameB}";
            options = new List<BetPickOption>
            {
                new()
                {
                    PendingMatchId = pendingMatch.Id, PredictedWinnerId = pendingMatch.PlayerAId, Label = nameA,
                    IsCurrentPick = currentBet?.PredictedWinnerId == pendingMatch.PlayerAId,
                },
                new()
                {
                    PendingMatchId = pendingMatch.Id, PredictedWinnerId = pendingMatch.PlayerBId, Label = nameB,
                    IsCurrentPick = currentBet?.PredictedWinnerId == pendingMatch.PlayerBId,
                },
            };
        }

        var isParticipant = _currentPlayerId is Guid pid && BettingService.GetParticipantIds(pendingMatch).Contains(pid);

        return new PendingMatchRow
        {
            PendingMatch = pendingMatch, Label = label, PickOptions = options, CurrentPlayerIsParticipant = isParticipant,
        };
    }

    private void CreatePendingMatch()
    {
        SetupErrorMessage = string.Empty;
        try
        {
            if (IsDoublesMode)
            {
                if (NewTeamAPlayer1 is null || NewTeamAPlayer2 is null || NewTeamBPlayer1 is null || NewTeamBPlayer2 is null)
                {
                    SetupErrorMessage = "Bitte alle vier Spieler auswählen.";
                    return;
                }

                _dataService.CreateDoublesPendingMatch(NewTeamAPlayer1.Id, NewTeamAPlayer2.Id, NewTeamBPlayer1.Id, NewTeamBPlayer2.Id);
            }
            else
            {
                if (NewPlayerA is null || NewPlayerB is null)
                {
                    SetupErrorMessage = "Bitte beide Spieler auswählen.";
                    return;
                }

                _dataService.CreateSinglesPendingMatch(NewPlayerA.Id, NewPlayerB.Id);
            }

            NewPlayerA = null;
            NewPlayerB = null;
            NewTeamAPlayer1 = null;
            NewTeamAPlayer2 = null;
            NewTeamBPlayer1 = null;
            NewTeamBPlayer2 = null;
            _notifications.NotifySuccess("Partie wurde angelegt - Tipps sind jetzt möglich.");
            Load();
        }
        catch (ValidationException ex)
        {
            SetupErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            SetupErrorMessage = "Unerwarteter Fehler: " + ex.Message;
            Logger.Error("BettingViewModel.CreatePendingMatch failed", ex);
        }
    }

    private void PlaceBet(BetPickOption option)
    {
        if (_currentPlayerId is not Guid bettorId)
        {
            _notifications.NotifyError("Bitte zuerst anmelden, um zu tippen.");
            return;
        }

        try
        {
            _dataService.PlaceOrUpdateBet(
                option.PendingMatchId, bettorId, option.PredictedWinnerId,
                string.IsNullOrEmpty(option.PredictedWinningTeam) ? null : option.PredictedWinningTeam);
            _notifications.NotifySuccess("Tipp wurde gespeichert.");
            Load();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("BettingViewModel.PlaceBet failed", ex);
        }
    }
}
