using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>"Liga" page: league table for the currently active Season, singles or
/// doubles (evaluated completely separately - see LeagueTableService).</summary>
public partial class LeagueViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;

    public string DataPath => _settingsRepository.Load().DataPath;

    [ObservableProperty] private bool isDoublesMode;
    [ObservableProperty] private bool hasActiveSeason;
    [ObservableProperty] private string activeSeasonLabel = string.Empty;

    public ObservableCollection<LeagueTableDisplayRow> Table { get; } = new();
    public ObservableCollection<LeagueTableDisplayRow> Podium { get; } = new();

    public IRelayCommand RefreshCommand { get; }

    /// <summary>Sets IsDoublesMode directly (mockup's Einzel/Doppel segmented
    /// control, Phase D4) - a thin wrapper so the two toggle buttons don't need
    /// a two-way CheckBox binding.</summary>
    public IRelayCommand<bool> SetDoublesModeCommand { get; }

    public LeagueViewModel(PingPongDataService dataService, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        RefreshCommand = new RelayCommand(Load);
        SetDoublesModeCommand = new RelayCommand<bool>(value => IsDoublesMode = value);
        Load();
    }

    partial void OnIsDoublesModeChanged(bool value) => Load();

    public void Load()
    {
        var activeSeason = _dataService.ActiveSeason;
        HasActiveSeason = activeSeason is not null;
        ActiveSeasonLabel = activeSeason is null
            ? string.Empty
            : $"{activeSeason.Name} ({activeSeason.StartDate:dd.MM.yyyy} - {activeSeason.EndDate:dd.MM.yyyy})";

        Table.Clear();
        Podium.Clear();
        if (activeSeason is null) return;

        var rows = IsDoublesMode
            ? LeagueTableService.BuildDoublesTable(_dataService.DoubleMatches, activeSeason)
            : LeagueTableService.BuildSinglesTable(_dataService.Matches, activeSeason);

        var playersById = _dataService.Players.ToDictionary(p => p.Id);

        var rank = 1;
        foreach (var row in rows)
        {
            var displayRow = new LeagueTableDisplayRow
            {
                Row = row,
                Player = playersById.GetValueOrDefault(row.PlayerId),
                Rank = rank,
            };

            Table.Add(displayRow);
            if (rank <= 3) Podium.Add(displayRow);
            rank++;
        }
    }
}
