using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class PlayersViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly NotificationService _notifications;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFilePickerService _filePicker;
    private readonly IAvatarImageService _avatarImageService;
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

    /// <summary>Absolute path of a newly picked (but not yet saved) avatar image, or
    /// null if none picked this edit session. Used by AvatarControl's OverrideImagePath
    /// to preview before Save actually processes and persists it.</summary>
    [ObservableProperty] private string? editAvatarPendingSourcePath;
    [ObservableProperty] private bool editAvatarRemoved;
    [ObservableProperty] private Player editPreviewPlayer = new();

    /// <summary>New 4-digit PIN to set on Save; left blank keeps the existing PIN
    /// unchanged. See PingPongDataService.SetPlayerPin for the security disclaimer.</summary>
    [ObservableProperty] private string editPinInput = string.Empty;
    [ObservableProperty] private bool editRemovePin;
    [ObservableProperty] private bool editPlayerHasPin;

    public string DataPath => _settingsRepository.Load().DataPath;

    public ObservableCollection<PlayerRow> Players { get; } = new();

    public IRelayCommand NewPlayerCommand { get; }
    public IRelayCommand<PlayerRow> EditPlayerCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IRelayCommand<PlayerRow> ToggleActiveCommand { get; }
    public IRelayCommand<PlayerRow> DeleteCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand PickAvatarCommand { get; }
    public IRelayCommand RemoveAvatarCommand { get; }

    public PlayersViewModel(
        PingPongDataService dataService,
        NotificationService notifications,
        ISettingsRepository settingsRepository,
        IFilePickerService filePicker,
        IAvatarImageService avatarImageService)
    {
        _dataService = dataService;
        _notifications = notifications;
        _settingsRepository = settingsRepository;
        _filePicker = filePicker;
        _avatarImageService = avatarImageService;

        NewPlayerCommand = new RelayCommand(BeginCreate);
        EditPlayerCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) BeginEdit(row); });
        SaveCommand = new RelayCommand(Save);
        CancelEditCommand = new RelayCommand(() => IsEditFormOpen = false);
        ToggleActiveCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) ToggleActive(row); });
        DeleteCommand = new RelayCommand<PlayerRow>(row => { if (row is not null) Delete(row); });
        RefreshCommand = new RelayCommand(Load);
        PickAvatarCommand = new RelayCommand(PickAvatar);
        RemoveAvatarCommand = new RelayCommand(RemoveAvatar);

        Load();
    }

    private void PickAvatar()
    {
        var path = _filePicker.PickImageFile("Profilbild auswählen");
        if (string.IsNullOrWhiteSpace(path)) return;

        EditAvatarPendingSourcePath = path;
        EditAvatarRemoved = false;
    }

    private void RemoveAvatar()
    {
        EditAvatarPendingSourcePath = null;
        EditAvatarRemoved = true;
        RefreshPreviewPlayer();
    }

    partial void OnEditDisplayNameChanged(string value) => RefreshPreviewPlayer();

    private void RefreshPreviewPlayer()
    {
        EditPreviewPlayer = new Player
        {
            Id = EditPreviewPlayer.Id,
            DisplayName = EditDisplayName,
            AvatarFileName = EditAvatarRemoved ? string.Empty : EditPreviewPlayer.AvatarFileName,
        };
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
        EditAvatarPendingSourcePath = null;
        EditAvatarRemoved = false;
        EditPreviewPlayer = new Player { DisplayName = string.Empty };
        EditPinInput = string.Empty;
        EditRemovePin = false;
        EditPlayerHasPin = false;
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
        EditAvatarPendingSourcePath = null;
        EditAvatarRemoved = false;
        EditPreviewPlayer = row.Player;
        EditPinInput = string.Empty;
        EditRemovePin = false;
        EditPlayerHasPin = _dataService.PlayerHasPin(row.Id);
        IsEditFormOpen = true;
    }

    private void Save()
    {
        try
        {
            Guid playerId;
            if (_editingPlayerId is Guid id)
            {
                _dataService.UpdatePlayer(id, EditDisplayName, EditFirstName, EditLastName, EditEmail);
                playerId = id;
                _notifications.NotifySuccess("Spieler wurde aktualisiert.");
            }
            else
            {
                var created = _dataService.CreatePlayer(EditDisplayName, EditFirstName, EditLastName, EditEmail);
                playerId = created.Id;
                _notifications.NotifySuccess("Spieler wurde angelegt.");
            }

            ApplyPendingAvatarChange(playerId);
            ApplyPendingPinChange(playerId);

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

    private void ApplyPendingAvatarChange(Guid playerId)
    {
        if (!string.IsNullOrWhiteSpace(EditAvatarPendingSourcePath))
        {
            var dataPath = DataPath;
            var fileName = _avatarImageService.SaveAvatar(EditAvatarPendingSourcePath, dataPath, playerId);
            _dataService.SetPlayerAvatar(playerId, fileName);
        }
        else if (EditAvatarRemoved)
        {
            _dataService.SetPlayerAvatar(playerId, string.Empty);
        }
    }

    private void ApplyPendingPinChange(Guid playerId)
    {
        if (EditRemovePin)
        {
            _dataService.SetPlayerPin(playerId, null);
        }
        else if (!string.IsNullOrWhiteSpace(EditPinInput))
        {
            _dataService.SetPlayerPin(playerId, EditPinInput.Trim());
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
