using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly DataPathService _dataPathService;
    private readonly NotificationService _notifications;
    private readonly IFolderPickerService _folderPicker;
    private readonly IShellService _shell;
    private readonly Action _onDataPathChanged;

    // Instance property (not static) so plain {Binding UiScaleOptions} resolves
    // correctly from the DataContext instance in XAML.
    public UiScaleOption[] UiScaleOptions { get; } =
    {
        new("Small", "Klein"),
        new("Medium", "Mittel"),
        new("Large", "Gross"),
    };

    [ObservableProperty] private string currentDataPath = string.Empty;
    [ObservableProperty] private bool darkMode;
    [ObservableProperty] private string selectedUiScale = "Medium";
    [ObservableProperty] private bool showWinAnimation = true;

    [ObservableProperty] private string newSeasonName = string.Empty;
    [ObservableProperty] private DateTime newSeasonStartDate = DateTime.Today;
    [ObservableProperty] private DateTime newSeasonEndDate = DateTime.Today.AddMonths(3);
    [ObservableProperty] private bool newSeasonIsActive = true;
    [ObservableProperty] private string seasonErrorMessage = string.Empty;

    public ObservableCollection<SeasonRow> Seasons { get; } = new();

    public IRelayCommand ChangeDataPathCommand { get; }
    public IRelayCommand ReloadCommand { get; }
    public IRelayCommand OpenBackupFolderCommand { get; }
    public IRelayCommand OpenDataFolderCommand { get; }
    public IRelayCommand ExportPlayersCommand { get; }
    public IRelayCommand ExportMatchesCommand { get; }
    public IRelayCommand ExportStatisticsCommand { get; }
    public IRelayCommand SeedDataCommand { get; }
    public IRelayCommand CreateSeasonCommand { get; }
    public IRelayCommand<SeasonRow> ToggleSeasonActiveCommand { get; }

    /// <summary>Raised after a season is created or (de)activated, so MainViewModel
    /// can refresh the Liga page without requiring a manual navigation round-trip.</summary>
    public event Action? SeasonsChanged;

    /// <summary>True only in Debug builds, so the seed-data button is hidden entirely
    /// in a Release/published EXE - it must never be reachable in production use.</summary>
#if DEBUG
    public bool IsSeedDataAvailable => true;
#else
    public bool IsSeedDataAvailable => false;
#endif

    public SettingsViewModel(
        PingPongDataService dataService,
        ISettingsRepository settingsRepository,
        DataPathService dataPathService,
        NotificationService notifications,
        IFolderPickerService folderPicker,
        IShellService shell,
        Action onDataPathChanged)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        _dataPathService = dataPathService;
        _notifications = notifications;
        _folderPicker = folderPicker;
        _shell = shell;
        _onDataPathChanged = onDataPathChanged;

        var settings = _settingsRepository.Load();
        CurrentDataPath = settings.DataPath;
        DarkMode = settings.DarkMode;
        SelectedUiScale = string.IsNullOrWhiteSpace(settings.UiScale) ? "Medium" : settings.UiScale;
        ShowWinAnimation = settings.ShowWinAnimation;

        ChangeDataPathCommand = new RelayCommand(ChangeDataPath);
        ReloadCommand = new RelayCommand(Reload);
        OpenBackupFolderCommand = new RelayCommand(() => _shell.OpenFolderInExplorer(Path.Combine(CurrentDataPath, "backups")));
        OpenDataFolderCommand = new RelayCommand(() => _shell.OpenFolderInExplorer(CurrentDataPath));
        ExportPlayersCommand = new RelayCommand(ExportPlayers);
        ExportMatchesCommand = new RelayCommand(ExportMatches);
        ExportStatisticsCommand = new RelayCommand(ExportStatistics);
        SeedDataCommand = new RelayCommand(SeedData);
        CreateSeasonCommand = new RelayCommand(CreateSeason);
        ToggleSeasonActiveCommand = new RelayCommand<SeasonRow>(row => { if (row is not null) ToggleSeasonActive(row); });

        LoadSeasons();
    }

    private void LoadSeasons()
    {
        Seasons.Clear();
        foreach (var season in _dataService.Seasons.OrderByDescending(s => s.StartDate))
        {
            Seasons.Add(new SeasonRow { Season = season });
        }
    }

    private void CreateSeason()
    {
        SeasonErrorMessage = string.Empty;
        try
        {
            _dataService.CreateSeason(NewSeasonName, NewSeasonStartDate, NewSeasonEndDate, NewSeasonIsActive);
            _notifications.NotifySuccess("Saison wurde angelegt.");
            NewSeasonName = string.Empty;
            LoadSeasons();
            SeasonsChanged?.Invoke();
        }
        catch (ValidationException ex)
        {
            SeasonErrorMessage = ex.Message;
        }
    }

    private void ToggleSeasonActive(SeasonRow row)
    {
        try
        {
            _dataService.SetSeasonActive(row.Id, !row.IsActive);
            LoadSeasons();
            SeasonsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("ToggleSeasonActive failed", ex);
        }
    }

    partial void OnDarkModeChanged(bool value)
    {
        var settings = _settingsRepository.Load();
        settings.DarkMode = value;
        _settingsRepository.Save(settings);
    }

    partial void OnSelectedUiScaleChanged(string value)
    {
        var settings = _settingsRepository.Load();
        settings.UiScale = value;
        _settingsRepository.Save(settings);
    }

    partial void OnShowWinAnimationChanged(bool value)
    {
        var settings = _settingsRepository.Load();
        settings.ShowWinAnimation = value;
        _settingsRepository.Save(settings);
    }

    private void ChangeDataPath()
    {
        var selected = _folderPicker.PickFolder("Datenordner auswählen", CurrentDataPath);
        if (string.IsNullOrWhiteSpace(selected)) return;

        try
        {
            _dataPathService.ValidateAndPrepare(selected);

            var settings = _settingsRepository.Load();
            settings.DataPath = selected;
            _settingsRepository.Save(settings);

            CurrentDataPath = selected;
            _onDataPathChanged();
            _notifications.NotifySuccess("Datenpfad wurde geändert und Daten neu geladen.");
        }
        catch (DataPathUnavailableException ex)
        {
            _notifications.NotifyError(ex.Message);
            Logger.Error("ChangeDataPath failed", ex);
        }
    }

    private void Reload()
    {
        try
        {
            _dataService.Reload();
            _notifications.NotifySuccess("XML-Dateien wurden neu geladen.");
        }
        catch (XmlDataCorruptException ex)
        {
            _notifications.NotifyError(ex.Message);
        }
    }

    private void ExportPlayers()
    {
        var path = SaveCsv("players-export.csv", CsvExportService.ExportPlayers(_dataService.Players));
        if (path is not null) _notifications.NotifySuccess($"Spieler exportiert nach {path}");
    }

    private void ExportMatches()
    {
        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        var path = SaveCsv("matches-export.csv", CsvExportService.ExportMatches(_dataService.Matches, playersById));
        if (path is not null) _notifications.NotifySuccess($"Spiele exportiert nach {path}");
    }

    private void ExportStatistics()
    {
        var snapshot = DashboardService.BuildDashboard(_dataService.Players.ToList(), _dataService.Matches.ToList());
        var path = SaveCsv("statistics-export.csv", CsvExportService.ExportStatistics(snapshot.Players));
        if (path is not null) _notifications.NotifySuccess($"Statistiken exportiert nach {path}");
    }

    private string? SaveCsv(string fileName, string content)
    {
        try
        {
            var exportDir = Path.Combine(CurrentDataPath, "exports");
            Directory.CreateDirectory(exportDir);
            var path = Path.Combine(exportDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
            File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
            return path;
        }
        catch (Exception ex)
        {
            _notifications.NotifyError($"Export fehlgeschlagen: {ex.Message}");
            Logger.Error("CSV export failed", ex);
            return null;
        }
    }

    private void SeedData()
    {
        try
        {
            var (players, matches, doubleMatches) = SeedDataService.Generate();
            _dataService.ReplaceAllData(players, matches, doubleMatches);
            _notifications.NotifySuccess(
                $"Testdaten erzeugt: {players.Count} Spieler, {matches.Count} Spiele, {doubleMatches.Count} Doppel-Spiele.");
        }
        catch (Exception ex)
        {
            _notifications.NotifyError($"Testdaten konnten nicht erzeugt werden: {ex.Message}");
            Logger.Error("SeedData failed", ex);
        }
    }
}
