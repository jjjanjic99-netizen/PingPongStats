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
    private TournamentDoublesMatchContext? _tournamentContext;

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

    /// <summary>Elo-based pre-match win probability (Phase 9): team Elo is the
    /// average of both players' ratings. Shown once all four players are picked.</summary>
    [ObservableProperty] private bool hasPrediction;
    [ObservableProperty] private string predictionTeamALabel = string.Empty;
    [ObservableProperty] private string predictionTeamBLabel = string.Empty;

    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand NewMatchCommand { get; }
    public IRelayCommand CancelFormCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand<QuickResultOption> ApplyQuickResultCommand { get; }
    public IRelayCommand<DoubleMatchRow> DeleteCommand { get; }
    public IRelayCommand AddSetCommand { get; }
    public IRelayCommand<SetResultEntryViewModel> RemoveSetCommand { get; }

    /// <summary>True while the open form is a tournament bracket slot's match:
    /// the four player selectors are locked to the bracket's assigned entrants.</summary>
    public bool IsTournamentMatch => _tournamentContext is not null;
    public bool ArePlayerSelectorsEnabled => !IsTournamentMatch;

    /// <summary>Raised after a successful save, so MainViewModel can show the win
    /// animation overlay.</summary>
    public event Action<MatchSavedInfo>? MatchSaved;

    /// <summary>Raised after a tournament-context save completes, so MainViewModel
    /// can navigate back to the "Turnier" page.</summary>
    public event Action? TournamentMatchFinished;

    public DoublesViewModel(
        PingPongDataService dataService, NotificationService notifications, DashboardRangeFilter rangeFilter)
    {
        _dataService = dataService;
        _notifications = notifications;
        RangeFilter = rangeFilter;
        RangeFilter.Changed += Load;

        RefreshCommand = new RelayCommand(Load);
        NewMatchCommand = new RelayCommand(BeginCreate);
        CancelFormCommand = new RelayCommand(() => { _tournamentContext = null; IsFormOpen = false; });
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

    partial void OnTeamAPlayer1Changed(Player? value) => UpdatePrediction();
    partial void OnTeamAPlayer2Changed(Player? value) => UpdatePrediction();
    partial void OnTeamBPlayer1Changed(Player? value) => UpdatePrediction();
    partial void OnTeamBPlayer2Changed(Player? value) => UpdatePrediction();

    private void UpdatePrediction()
    {
        if (TeamAPlayer1 is null || TeamAPlayer2 is null || TeamBPlayer1 is null || TeamBPlayer2 is null)
        {
            HasPrediction = false;
            PredictionTeamALabel = string.Empty;
            PredictionTeamBLabel = string.Empty;
            return;
        }

        var eloRatings = EloService.ComputeRatings(_dataService.Matches, _dataService.Players.Select(p => p.Id));
        var teamAElo = EloPredictionService.ComputeTeamElo(
            eloRatings.GetValueOrDefault(TeamAPlayer1.Id, EloService.DefaultInitialRating),
            eloRatings.GetValueOrDefault(TeamAPlayer2.Id, EloService.DefaultInitialRating));
        var teamBElo = EloPredictionService.ComputeTeamElo(
            eloRatings.GetValueOrDefault(TeamBPlayer1.Id, EloService.DefaultInitialRating),
            eloRatings.GetValueOrDefault(TeamBPlayer2.Id, EloService.DefaultInitialRating));
        var probabilityAPercent = EloPredictionService.ComputeWinProbability(teamAElo, teamBElo) * 100;

        HasPrediction = true;
        PredictionTeamALabel = $"{TeamAPlayer1.DisplayName} & {TeamAPlayer2.DisplayName}: {probabilityAPercent:F0}%";
        PredictionTeamBLabel = $"{TeamBPlayer1.DisplayName} & {TeamBPlayer2.DisplayName}: {100 - probabilityAPercent:F0}%";
    }

    public void Load()
    {
        // Only active players can be picked for a new doubles match, except the
        // players a locked tournament-context match already assigned (which may
        // since have been archived) so the pre-fill can still succeed.
        var relevantIds = _tournamentContext is null
            ? new HashSet<Guid>()
            : new HashSet<Guid>
            {
                _tournamentContext.TeamAPlayer1Id, _tournamentContext.TeamAPlayer2Id,
                _tournamentContext.TeamBPlayer1Id, _tournamentContext.TeamBPlayer2Id,
            };

        AvailablePlayers.Clear();
        foreach (var p in _dataService.Players.Where(p => p.IsActive || relevantIds.Contains(p.Id)).OrderBy(p => p.DisplayName))
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
        _tournamentContext = null;
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
        OnPropertyChanged(nameof(IsTournamentMatch));
        OnPropertyChanged(nameof(ArePlayerSelectorsEnabled));
    }

    /// <summary>Opens the form pre-filled and locked for a tournament bracket
    /// slot's doubles match (Phase 12).</summary>
    public void BeginTournamentMatch(TournamentDoublesMatchContext context)
    {
        _tournamentContext = context;
        PlayedAtDate = DateTime.Today;
        PlayedAtTime = DateTime.Now.ToString("HH:mm");
        TeamAPlayer1 = AvailablePlayers.FirstOrDefault(p => p.Id == context.TeamAPlayer1Id);
        TeamAPlayer2 = AvailablePlayers.FirstOrDefault(p => p.Id == context.TeamAPlayer2Id);
        TeamBPlayer1 = AvailablePlayers.FirstOrDefault(p => p.Id == context.TeamBPlayer1Id);
        TeamBPlayer2 = AvailablePlayers.FirstOrDefault(p => p.Id == context.TeamBPlayer2Id);
        TeamASets = 0;
        TeamBSets = 0;
        Notes = string.Empty;
        WinnerSide = "A";
        FormErrorMessage = string.Empty;
        SetEntries.Clear();
        IsFormOpen = true;
        OnPropertyChanged(nameof(IsTournamentMatch));
        OnPropertyChanged(nameof(ArePlayerSelectorsEnabled));
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
            DoubleMatch savedMatch;
            var isTournamentFinal = false;

            if (_tournamentContext is not null)
            {
                savedMatch = _dataService.RecordTournamentDoublesResult(
                    _tournamentContext.TournamentId, _tournamentContext.SlotId, playedAt,
                    TeamAPlayer1.Id, TeamAPlayer2.Id, TeamBPlayer1.Id, TeamBPlayer2.Id,
                    TeamASets, TeamBSets, Notes, setResults);
                var tournament = _dataService.Tournaments.FirstOrDefault(t => t.Id == _tournamentContext.TournamentId);
                isTournamentFinal = tournament?.Status == TournamentStatus.Completed;
                _notifications.NotifySuccess("Turnier-Partie wurde erfasst.");
            }
            else
            {
                savedMatch = _dataService.CreateDoubleMatch(
                    playedAt, TeamAPlayer1.Id, TeamAPlayer2.Id, TeamBPlayer1.Id, TeamBPlayer2.Id,
                    TeamASets, TeamBSets, Notes, setResults);
                _notifications.NotifySuccess("Doppel-Spiel wurde erfasst.");
            }

            var teamAWon = savedMatch.WinningTeam == "A";
            var winnerId1 = teamAWon ? savedMatch.TeamAPlayer1Id : savedMatch.TeamBPlayer1Id;
            var winnerId2 = teamAWon ? savedMatch.TeamAPlayer2Id : savedMatch.TeamBPlayer2Id;
            var loserId1 = teamAWon ? savedMatch.TeamBPlayer1Id : savedMatch.TeamAPlayer1Id;
            var loserId2 = teamAWon ? savedMatch.TeamBPlayer2Id : savedMatch.TeamAPlayer2Id;
            var winnerSets = teamAWon ? savedMatch.TeamASets : savedMatch.TeamBSets;
            var loserSets = teamAWon ? savedMatch.TeamBSets : savedMatch.TeamASets;
            var isComeback = ComebackService.IsComebackDoubles(savedMatch);

            // Elo only ever processes singles matches, so this doubles match itself
            // never changed anyone's rating - no need to exclude it like the singles
            // Save() does. Team Elo = average of the two players' (singles) ratings.
            var eloRatings = EloService.ComputeRatings(_dataService.Matches, _dataService.Players.Select(p => p.Id));
            var winnerTeamElo = (eloRatings.GetValueOrDefault(winnerId1, EloService.DefaultInitialRating)
                + eloRatings.GetValueOrDefault(winnerId2, EloService.DefaultInitialRating)) / 2.0;
            var loserTeamElo = (eloRatings.GetValueOrDefault(loserId1, EloService.DefaultInitialRating)
                + eloRatings.GetValueOrDefault(loserId2, EloService.DefaultInitialRating)) / 2.0;
            var isUnderdogWin = winnerTeamElo < loserTeamElo;

            var quoteCategory = QuoteCategorySelector.SelectCategory(
                isDoubles: true, isComeback, isUnderdogWin, winnerSets, loserSets);

            MatchSaved?.Invoke(new MatchSavedInfo(
                IsDoubles: true, winnerId1, winnerId2, $"{winnerSets}:{loserSets}", isComeback, quoteCategory,
                IsTournamentFinal: isTournamentFinal));

            var wasTournamentMatch = _tournamentContext is not null;
            _tournamentContext = null;
            IsFormOpen = false;
            Load();
            OnPropertyChanged(nameof(IsTournamentMatch));
            OnPropertyChanged(nameof(ArePlayerSelectorsEnabled));
            if (wasTournamentMatch) TournamentMatchFinished?.Invoke();
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
