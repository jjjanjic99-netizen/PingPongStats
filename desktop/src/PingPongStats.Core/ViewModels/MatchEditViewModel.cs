using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>Shared ViewModel for both "Neues Spiel" and "Spiel bearbeiten" - the
/// only difference is whether a matchIdToEdit was supplied.</summary>
public partial class MatchEditViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;
    private readonly Guid? _editingMatchId;

    public static readonly QuickResultOption[] QuickResultOptions =
    {
        new(1, 0), new(2, 0), new(2, 1), new(3, 0), new(3, 1), new(3, 2),
    };

    [ObservableProperty] private DateTime playedAtDate = DateTime.Today;
    [ObservableProperty] private string playedAtTime = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private Player? playerA;
    [ObservableProperty] private Player? playerB;
    [ObservableProperty] private int playerASets;
    [ObservableProperty] private int playerBSets;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string winnerSide = "A";
    [ObservableProperty] private string title = "Neues Spiel erfassen";

    public ObservableCollection<Player> AvailablePlayers { get; } = new();

    /// <summary>Optional set-by-set score entry. Empty = no set detail recorded
    /// (matches remain fully usable without it).</summary>
    public ObservableCollection<SetResultEntryViewModel> SetEntries { get; } = new();

    /// <summary>Instance wrapper around the static option list, for XAML binding convenience.</summary>
    public IReadOnlyList<QuickResultOption> QuickResults => QuickResultOptions;

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand<QuickResultOption> ApplyQuickResultCommand { get; }
    public IRelayCommand AddSetCommand { get; }
    public IRelayCommand<SetResultEntryViewModel> RemoveSetCommand { get; }

    public bool IsEditMode => _editingMatchId is not null;

    /// <summary>Raised after a successful save or when the user cancels, so the
    /// hosting navigation can return to the matches list.</summary>
    public event Action? Finished;

    /// <summary>Raised after a successful save (create or update), so MainViewModel
    /// can show the win animation overlay.</summary>
    public event Action<MatchSavedInfo>? MatchSaved;

    public MatchEditViewModel(PingPongDataService dataService, NotificationService notifications, Guid? matchIdToEdit = null)
    {
        _dataService = dataService;
        _notifications = notifications;
        _editingMatchId = matchIdToEdit;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => Finished?.Invoke());
        ApplyQuickResultCommand = new RelayCommand<QuickResultOption>(option => { if (option is not null) ApplyQuickResult(option); });
        AddSetCommand = new RelayCommand(AddSet);
        RemoveSetCommand = new RelayCommand<SetResultEntryViewModel>(entry => { if (entry is not null) RemoveSet(entry); });

        LoadAvailablePlayers();

        if (matchIdToEdit is Guid id)
        {
            Title = "Spiel bearbeiten";
            var existing = _dataService.Matches.FirstOrDefault(m => m.Id == id);
            if (existing is not null)
            {
                PlayedAtDate = existing.PlayedAt.Date;
                PlayedAtTime = existing.PlayedAt.ToString("HH:mm");
                PlayerA = AvailablePlayers.FirstOrDefault(p => p.Id == existing.PlayerAId);
                PlayerB = AvailablePlayers.FirstOrDefault(p => p.Id == existing.PlayerBId);
                PlayerASets = existing.PlayerASets;
                PlayerBSets = existing.PlayerBSets;
                Notes = existing.Notes;

                if (existing.SetResults is { Count: > 0 })
                {
                    foreach (var set in existing.SetResults)
                    {
                        SetEntries.Add(SetResultEntryViewModel.FromModel(set));
                    }
                }
            }
        }
    }

    private void AddSet()
    {
        SetEntries.Add(new SetResultEntryViewModel { SetNumber = SetEntries.Count + 1 });
    }

    private void RemoveSet(SetResultEntryViewModel entry)
    {
        SetEntries.Remove(entry);
        for (var i = 0; i < SetEntries.Count; i++)
        {
            SetEntries[i].SetNumber = i + 1;
        }
    }

    private void LoadAvailablePlayers()
    {
        AvailablePlayers.Clear();

        // Only active players can be picked for a new/edited match, except the
        // players already on the match being edited (which may since have been
        // archived) so an existing entry can still be shown/edited.
        var relevantIds = new HashSet<Guid>();
        if (_editingMatchId is Guid id)
        {
            var existing = _dataService.Matches.FirstOrDefault(m => m.Id == id);
            if (existing is not null)
            {
                relevantIds.Add(existing.PlayerAId);
                relevantIds.Add(existing.PlayerBId);
            }
        }

        foreach (var p in _dataService.Players
                     .Where(p => p.IsActive || relevantIds.Contains(p.Id))
                     .OrderBy(p => p.DisplayName))
        {
            AvailablePlayers.Add(p);
        }
    }

    private void ApplyQuickResult(QuickResultOption option)
    {
        if (WinnerSide == "A")
        {
            PlayerASets = option.WinnerSets;
            PlayerBSets = option.LoserSets;
        }
        else
        {
            PlayerBSets = option.WinnerSets;
            PlayerASets = option.LoserSets;
        }
    }

    private void Save()
    {
        ErrorMessage = string.Empty;

        if (PlayerA is null || PlayerB is null)
        {
            ErrorMessage = "Bitte beide Spieler auswählen.";
            return;
        }

        if (!TimeSpan.TryParse(PlayedAtTime, out var timeOfDay))
        {
            ErrorMessage = "Ungültige Uhrzeit. Format: HH:mm.";
            return;
        }

        var playedAt = DateTime.SpecifyKind(PlayedAtDate.Date + timeOfDay, DateTimeKind.Unspecified);
        var setResults = SetEntries.Count == 0 ? null : SetEntries.Select(e => e.ToModel()).ToList();

        try
        {
            Match savedMatch;
            if (_editingMatchId is Guid id)
            {
                _dataService.UpdateMatch(id, playedAt, PlayerA.Id, PlayerB.Id, PlayerASets, PlayerBSets, Notes, setResults);
                savedMatch = _dataService.Matches.First(m => m.Id == id);
                _notifications.NotifySuccess("Spiel wurde aktualisiert.");
            }
            else
            {
                savedMatch = _dataService.CreateMatch(playedAt, PlayerA.Id, PlayerB.Id, PlayerASets, PlayerBSets, Notes, setResults);
                _notifications.NotifySuccess("Spiel wurde erfasst.");
            }

            var winnerIsPlayerA = savedMatch.WinnerId == savedMatch.PlayerAId;
            var winnerSets = winnerIsPlayerA ? savedMatch.PlayerASets : savedMatch.PlayerBSets;
            var loserSets = winnerIsPlayerA ? savedMatch.PlayerBSets : savedMatch.PlayerASets;
            var loserId = winnerIsPlayerA ? savedMatch.PlayerBId : savedMatch.PlayerAId;
            var isComeback = ComebackService.IsComeback(savedMatch);

            // Ratings excluding this specific match, so a big upset doesn't already
            // show the winner boosted past the loser when deciding "was this an
            // underdog win".
            var priorMatches = _dataService.Matches.Where(m => m.Id != savedMatch.Id).ToList();
            var priorRatings = EloService.ComputeRatings(priorMatches, _dataService.Players.Select(p => p.Id));
            var isUnderdogWin = priorRatings.GetValueOrDefault(savedMatch.WinnerId, EloService.DefaultInitialRating)
                < priorRatings.GetValueOrDefault(loserId, EloService.DefaultInitialRating);

            var quoteCategory = QuoteCategorySelector.SelectCategory(
                isDoubles: false, isComeback, isUnderdogWin, winnerSets, loserSets);

            MatchSaved?.Invoke(new MatchSavedInfo(
                IsDoubles: false, savedMatch.WinnerId, null, $"{winnerSets}:{loserSets}", isComeback, quoteCategory));

            Finished?.Invoke();
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unerwarteter Fehler: " + ex.Message;
            Logger.Error("MatchEditViewModel.Save failed", ex);
        }
    }
}
