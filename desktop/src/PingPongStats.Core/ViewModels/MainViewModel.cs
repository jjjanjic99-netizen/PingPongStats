using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty] private ObservableObject? currentViewModel;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isErrorStatus;
    [ObservableProperty] private string activeSection = "Dashboard";

    public DashboardRangeFilter RangeFilter { get; private set; } = null!;
    public DashboardViewModel Dashboard { get; }
    public PlayersViewModel Players { get; }
    public MatchesViewModel Matches { get; }
    public DoublesViewModel Doubles { get; }
    public HeadToHeadViewModel HeadToHead { get; }
    public SettingsViewModel Settings { get; }

    public IRelayCommand ShowDashboardCommand { get; }
    public IRelayCommand ShowPlayersCommand { get; }
    public IRelayCommand ShowMatchesCommand { get; }
    public IRelayCommand ShowNewMatchCommand { get; }
    public IRelayCommand ShowDoublesCommand { get; }
    public IRelayCommand ShowHeadToHeadCommand { get; }
    public IRelayCommand ShowSettingsCommand { get; }

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
        HeadToHead = new HeadToHeadViewModel(_dataService);
        Settings = new SettingsViewModel(
            _dataService, _settingsRepository, _dataPathService, _notifications,
            _folderPicker, _shell, OnDataPathChanged);

        ShowDashboardCommand = new RelayCommand(() => Navigate("Dashboard", Dashboard, () => Dashboard.Load()));
        ShowPlayersCommand = new RelayCommand(() => Navigate("Spieler", Players, () => Players.Load()));
        ShowMatchesCommand = new RelayCommand(() => Navigate("Spiele", Matches, () => Matches.Load()));
        ShowNewMatchCommand = new RelayCommand(ShowNewMatch);
        ShowDoublesCommand = new RelayCommand(() => Navigate("Doppel", Doubles, () => Doubles.Load()));
        ShowHeadToHeadCommand = new RelayCommand(() => Navigate("Head-to-Head", HeadToHead, () => HeadToHead.Load()));
        ShowSettingsCommand = new RelayCommand(() => Navigate("Einstellungen", Settings, null));

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
        ActiveSection = "Neues Spiel";
        CurrentViewModel = editVm;
    }

    private void OnEditMatchRequested(Guid matchId)
    {
        var editVm = new MatchEditViewModel(_dataService, _notifications, matchId);
        editVm.Finished += () => Navigate("Spiele", Matches, () => Matches.Load());
        ActiveSection = "Spiel bearbeiten";
        CurrentViewModel = editVm;
    }

    private void OnDataPathChanged()
    {
        _dataService.Reload();
        Dashboard.Load();
        Players.Load();
        Matches.Load();
        Doubles.Load();
        HeadToHead.Load();
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
