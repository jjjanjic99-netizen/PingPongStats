using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>"Mein Profil": personal stats for the currently logged-in player.</summary>
public partial class ProfileViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;
    private Guid _playerId;

    public string DataPath => _settingsRepository.Load().DataPath;

    [ObservableProperty] private Player player = new();
    [ObservableProperty] private int played;
    [ObservableProperty] private int wins;
    [ObservableProperty] private int losses;
    [ObservableProperty] private string winRateLabel = "–";
    [ObservableProperty] private string eloLabel = "–";
    [ObservableProperty] private string longestWinStreakLabel = "–";
    [ObservableProperty] private string longestLossStreakLabel = "–";
    [ObservableProperty] private string setDifferenceLabel = "–";
    [ObservableProperty] private bool hasEloHistory;

    public ObservableCollection<EloChartPoint> EloHistoryChart { get; } = new();

    public IRelayCommand RefreshCommand { get; }

    public ProfileViewModel(PingPongDataService dataService, ISettingsRepository settingsRepository, Guid playerId)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        _playerId = playerId;
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    /// <summary>Re-targets this ViewModel at a different logged-in player (reused
    /// across logins instead of re-constructing) and reloads.</summary>
    public void SetPlayer(Guid playerId)
    {
        _playerId = playerId;
        Load();
    }

    public void Load()
    {
        var found = _dataService.Players.FirstOrDefault(p => p.Id == _playerId);
        if (found is null) return;
        Player = found;

        var matches = _dataService.Matches.ToList();
        var record = StatsService.GetOverallRecord(matches, _playerId);
        Played = record.Played;
        Wins = record.Wins;
        Losses = record.Losses;
        WinRateLabel = record.Played == 0 ? "–" : $"{record.WinRatePct:F1}%";

        var eloRatings = EloService.ComputeRatings(matches, _dataService.Players.Select(p => p.Id));
        EloLabel = eloRatings.GetValueOrDefault(_playerId, EloService.DefaultInitialRating).ToString("F0");

        LongestWinStreakLabel = StatsService.GetLongestWinStreak(matches, _playerId).ToString();
        LongestLossStreakLabel = StatsService.GetLongestLossStreak(matches, _playerId).ToString();
        var setDiff = StatsService.GetSetDifference(matches, _playerId);
        SetDifferenceLabel = setDiff > 0 ? $"+{setDiff}" : setDiff.ToString();

        var history = EloService.GetRatingHistory(matches, _playerId);
        EloHistoryChart.Clear();
        if (history.Count > 0)
        {
            var minRating = history.Min(h => h.Rating);
            var maxRating = history.Max(h => h.Rating);
            var range = maxRating - minRating;
            for (var i = 0; i < history.Count; i++)
            {
                var normalizedX = history.Count == 1 ? 0.5 : i / (double)(history.Count - 1);
                var normalizedY = range < 0.0001 ? 0.5 : (history[i].Rating - minRating) / range;
                EloHistoryChart.Add(new EloChartPoint(history[i].PlayedAt, history[i].Rating, normalizedX, normalizedY));
            }
        }

        HasEloHistory = EloHistoryChart.Count > 0;
    }
}
