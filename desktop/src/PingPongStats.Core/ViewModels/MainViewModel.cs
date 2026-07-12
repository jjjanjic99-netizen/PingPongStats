using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>
/// Composition root for the screen ViewModels and the simple view-model-first
/// navigation (CurrentViewModel is swapped via DataTemplates in the WPF App
/// project). Also owns the status banner shown after actions (success/error).
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly DataPathService _dataPathService;
    private readonly NotificationService _notifications;
    private readonly IFolderPickerService _folderPicker;
    private readonly IShellService _shell;
    private readonly IFilePickerService _filePicker;
    private readonly IAvatarImageService _avatarImageService;
    private readonly SynchronizationContext? _uiContext;
    private System.Threading.Timer? _statusClearTimer;
    private System.Threading.Timer? _winAnimationTimer;

    [ObservableProperty] private ObservableObject? currentViewModel;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isErrorStatus;
    [ObservableProperty] private string activeSection = "Dashboard";
    [ObservableProperty] private Player? currentPlayer;

    /// <summary>Phase 6 win/confetti overlay state, shown for ~3s after a
    /// successful match save (singles or doubles) when enabled in Settings.</summary>
    [ObservableProperty] private bool isWinAnimationVisible;
    [ObservableProperty] private bool winAnimationIsDoubles;
    [ObservableProperty] private Player? winAnimationPlayer1;
    [ObservableProperty] private Player? winAnimationPlayer2;
    [ObservableProperty] private string winAnimationScoreLabel = string.Empty;
    [ObservableProperty] private bool winAnimationIsComeback;
    [ObservableProperty] private string? winAnimationQuote;

    /// <summary>Phase 12: true when the just-saved match completed a tournament,
    /// so the win overlay shows the bigger trophy/"Turniersieger" treatment.</summary>
    [ObservableProperty] private bool winAnimationIsTournamentFinal;

    public DashboardRangeFilter RangeFilter { get; private set; } = null!;
    public DashboardViewModel Dashboard { get; }
    public PlayersViewModel Players { get; }
    public MatchesViewModel Matches { get; }
    public DoublesViewModel Doubles { get; }
    public HeadToHeadViewModel HeadToHead { get; }
    public SettingsViewModel Settings { get; }
    public LoginViewModel Login { get; }
    public ProfileViewModel? Profile { get; private set; }
    public LeagueViewModel League { get; }
    public TournamentViewModel Tournament { get; }

    public bool IsLoggedIn => CurrentPlayer is not null;

    public IRelayCommand ShowDashboardCommand { get; }
    public IRelayCommand ShowPlayersCommand { get; }
    public IRelayCommand ShowMatchesCommand { get; }
    public IRelayCommand ShowNewMatchCommand { get; }
    public IRelayCommand ShowDoublesCommand { get; }
    public IRelayCommand ShowHeadToHeadCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }
    public IRelayCommand ShowLoginCommand { get; }
    public IRelayCommand ShowProfileCommand { get; }
    public IRelayCommand LogoutCommand { get; }
    public IRelayCommand DismissWinAnimationCommand { get; }
    public IRelayCommand ShowLeagueCommand { get; }
    public IRelayCommand ShowTournamentCommand { get; }

    public MainViewModel(
        PingPongDataService dataService,
        ISettingsRepository settingsRepository,
        DataPathService dataPathService,
        IFolderPickerService folderPicker,
        IShellService shell,
        IFilePickerService filePicker,
        IAvatarImageService avatarImageService)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        _dataPathService = dataPathService;
        _folderPicker = folderPicker;
        _shell = shell;
        _filePicker = filePicker;
        _avatarImageService = avatarImageService;
        _uiContext = SynchronizationContext.Current;
        _notifications = new NotificationService();
        _notifications.Notified += OnNotified;

        RangeFilter = new DashboardRangeFilter();
        Dashboard = new DashboardViewModel(_dataService, RangeFilter, _settingsRepository);
        Players = new PlayersViewModel(_dataService, _notifications, _settingsRepository, _filePicker, _avatarImageService);
        Matches = new MatchesViewModel(_dataService, _notifications);
        Matches.EditRequested += OnEditMatchRequested;
        Doubles = new DoublesViewModel(_dataService, _notifications, RangeFilter);
        Doubles.MatchSaved += OnMatchSaved;
        HeadToHead = new HeadToHeadViewModel(_dataService);
        Settings = new SettingsViewModel(
            _dataService, _settingsRepository, _dataPathService, _notifications,
            _folderPicker, _shell, OnDataPathChanged);
        Login = new LoginViewModel(_dataService, _settingsRepository);
        Login.LoggedIn += OnLoggedIn;
        League = new LeagueViewModel(_dataService, _settingsRepository);
        Settings.SeasonsChanged += () => League.Load();
        Tournament = new TournamentViewModel(_dataService, _notifications, _settingsRepository);
        Tournament.PlayRequested += OnTournamentPlayRequested;
        Doubles.TournamentMatchFinished += () => Navigate("Turnier", Tournament, () => Tournament.Load());

        ShowDashboardCommand = new RelayCommand(() => Navigate("Dashboard", Dashboard, () => Dashboard.Load()));
        ShowPlayersCommand = new RelayCommand(() => Navigate("Spieler", Players, () => Players.Load()));
        ShowMatchesCommand = new RelayCommand(() => Navigate("Spiele", Matches, () => Matches.Load()));
        ShowNewMatchCommand = new RelayCommand(ShowNewMatch);
        ShowDoublesCommand = new RelayCommand(() => Navigate("Doppel", Doubles, () => Doubles.Load()));
        ShowHeadToHeadCommand = new RelayCommand(() => Navigate("Head-to-Head", HeadToHead, () => HeadToHead.Load()));
        ShowSettingsCommand = new RelayCommand(() => Navigate("Einstellungen", Settings, null));
        ShowLoginCommand = new RelayCommand(() => Navigate("Anmeldung", Login, () => Login.Load()));
        ShowProfileCommand = new RelayCommand(() =>
        {
            if (Profile is not null) Navigate("Mein Profil", Profile, () => Profile.Load());
        });
        LogoutCommand = new RelayCommand(Logout);
        DismissWinAnimationCommand = new RelayCommand(DismissWinAnimation);
        ShowLeagueCommand = new RelayCommand(() => Navigate("Liga", League, () => League.Load()));
        ShowTournamentCommand = new RelayCommand(() => Navigate("Turnier", Tournament, () => Tournament.Load()));

        CurrentViewModel = Dashboard;
    }

    private void Navigate(string section, ObservableObject viewModel, Action? refresh)
    {
        refresh?.Invoke();
        ActiveSection = section;
        CurrentViewModel = viewModel;
    }

    private void ShowNewMatch()
    {
        var editVm = new MatchEditViewModel(_dataService, _notifications);
        editVm.Finished += () => Navigate("Spiele", Matches, () => Matches.Load());
        editVm.MatchSaved += OnMatchSaved;
        ActiveSection = "Neues Spiel";
        CurrentViewModel = editVm;
    }

    private void OnEditMatchRequested(Guid matchId)
    {
        var editVm = new MatchEditViewModel(_dataService, _notifications, matchId);
        editVm.Finished += () => Navigate("Spiele", Matches, () => Matches.Load());
        editVm.MatchSaved += OnMatchSaved;
        ActiveSection = "Spiel bearbeiten";
        CurrentViewModel = editVm;
    }

    /// <summary>Phase 12: the user clicked a playable bracket slot on the
    /// "Turnier" page - open the normal (locked, pre-filled) match/doubles entry
    /// form for the two entrants assigned to that slot.</summary>
    private void OnTournamentPlayRequested(TournamentPlayRequest request)
    {
        var tournament = _dataService.Tournaments.FirstOrDefault(t => t.Id == request.TournamentId);
        if (tournament is null) return;

        var entrantA = tournament.Entrants.FirstOrDefault(e => e.Id == request.Slot.EntrantAId);
        var entrantB = tournament.Entrants.FirstOrDefault(e => e.Id == request.Slot.EntrantBId);
        if (entrantA is null || entrantB is null) return;

        if (request.Mode == TournamentMode.Doubles)
        {
            if (entrantA.Player2Id is not Guid entrantAPlayer2 || entrantB.Player2Id is not Guid entrantBPlayer2) return;

            var context = new TournamentDoublesMatchContext(
                request.TournamentId, request.Slot.Id,
                entrantA.Player1Id, entrantAPlayer2, entrantB.Player1Id, entrantBPlayer2);

            Doubles.BeginTournamentMatch(context);
            ActiveSection = "Doppel";
            CurrentViewModel = Doubles;
        }
        else
        {
            var context = new TournamentMatchContext(request.TournamentId, request.Slot.Id, entrantA.Player1Id, entrantB.Player1Id);
            var editVm = new MatchEditViewModel(_dataService, _notifications, null, context);
            editVm.Finished += () => Navigate("Turnier", Tournament, () => Tournament.Load());
            editVm.MatchSaved += OnMatchSaved;
            ActiveSection = "Turnier-Partie";
            CurrentViewModel = editVm;
        }
    }

    private void OnMatchSaved(MatchSavedInfo info)
    {
        Tournament.Load();

        if (!_settingsRepository.Load().ShowWinAnimation) return;

        WinAnimationIsDoubles = info.IsDoubles;
        WinAnimationIsTournamentFinal = info.IsTournamentFinal;
        WinAnimationPlayer1 = _dataService.Players.FirstOrDefault(p => p.Id == info.WinnerId1);
        WinAnimationPlayer2 = info.WinnerId2 is Guid id2 ? _dataService.Players.FirstOrDefault(p => p.Id == id2) : null;
        WinAnimationScoreLabel = info.ScoreLabel;
        WinAnimationIsComeback = info.IsComeback;
        WinAnimationQuote = QuoteService.PickRandomQuote(_dataService.Quotes, info.QuoteCategory);
        IsWinAnimationVisible = true;

        _winAnimationTimer?.Dispose();
        _winAnimationTimer = new System.Threading.Timer(_ =>
        {
            if (_uiContext is not null) _uiContext.Post(_ => IsWinAnimationVisible = false, null);
            else IsWinAnimationVisible = false;
        }, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
    }

    private void DismissWinAnimation()
    {
        _winAnimationTimer?.Dispose();
        IsWinAnimationVisible = false;
    }

    partial void OnCurrentPlayerChanged(Player? value) => OnPropertyChanged(nameof(IsLoggedIn));

    private void OnLoggedIn(Guid playerId)
    {
        var player = _dataService.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null) return;

        CurrentPlayer = player;
        if (Profile is null) Profile = new ProfileViewModel(_dataService, _settingsRepository, playerId);
        else Profile.SetPlayer(playerId);

        Navigate("Mein Profil", Profile, null);
    }

    private void Logout()
    {
        CurrentPlayer = null;
        Profile = null;
        Navigate("Dashboard", Dashboard, () => Dashboard.Load());
    }

    private void OnDataPathChanged()
    {
        _dataService.Reload();
        Dashboard.Load();
        Players.Load();
        Matches.Load();
        Doubles.Load();
        HeadToHead.Load();
        League.Load();
        Tournament.Load();
        CurrentPlayer = null;
        Profile = null;
        Login.Load();
    }

    private void OnNotified(string message, bool isError)
    {
        StatusMessage = message;
        IsErrorStatus = isError;

        _statusClearTimer?.Dispose();
        _statusClearTimer = new System.Threading.Timer(_ =>
        {
            if (_uiContext is not null) _uiContext.Post(_ => StatusMessage = string.Empty, null);
            else StatusMessage = string.Empty;
        }, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }
}
