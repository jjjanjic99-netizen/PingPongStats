using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;

    [ObservableProperty] private int totalMatches;
    [ObservableProperty] private int activePlayerCount;
    [ObservableProperty] private string mostWinsLabel = "–";
    [ObservableProperty] private string highestOverallWinRateLabel = "–";
    [ObservableProperty] private string highestWinRateLastMonthLabel = "–";
    [ObservableProperty] private string bestCurrentStreakLabel = "–";
    [ObservableProperty] private bool hasData;

    /// <summary>Ranked (by Elo) players with every per-player stat the dashboard
    /// requires (win rate, 30d win rate, streaks, recent form, Elo). This single
    /// table covers most required dashboard figures without needing extra charts.</summary>
    public ObservableCollection<PlayerRankingRow> Ranking { get; } = new();

    public ObservableCollection<ChartBarItem> WinsChart { get; } = new();
    public ObservableCollection<ChartBarItem> MatchesPerWeekChart { get; } = new();

    public IRelayCommand RefreshCommand { get; }

    public DashboardViewModel(PingPongDataService dataService)
    {
        _dataService = dataService;
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    public void Load()
    {
        var snapshot = DashboardService.BuildDashboard(_dataService.Players.ToList(), _dataService.Matches.ToList());

        TotalMatches = snapshot.TotalMatches;
        ActivePlayerCount = snapshot.ActivePlayerCount;
        HasData = snapshot.TotalMatches > 0;

        MostWinsLabel = snapshot.MostWins is null
            ? "–"
            : $"{snapshot.MostWins.DisplayName} ({snapshot.MostWins.Wins})";
        HighestOverallWinRateLabel = snapshot.HighestOverallWinRate is null
            ? "–"
            : $"{snapshot.HighestOverallWinRate.DisplayName} ({snapshot.HighestOverallWinRate.WinRatePct:F0}%)";
        HighestWinRateLastMonthLabel = snapshot.HighestWinRateLastMonth is null
            ? "–"
            : $"{snapshot.HighestWinRateLastMonth.DisplayName} ({snapshot.HighestWinRateLastMonth.WinRateLast30dPct:F0}%)";
        BestCurrentStreakLabel = snapshot.BestCurrentStreak is null
            ? "–"
            : $"{snapshot.BestCurrentStreak.DisplayName} ({snapshot.BestCurrentStreak.CurrentStreak.Length}x)";

        var rankedPlayers = snapshot.Players
            .Where(p => p.Played > 0)
            .OrderByDescending(p => p.EloRating)
            .ToList();

        Ranking.Clear();
        foreach (var p in rankedPlayers) Ranking.Add(new PlayerRankingRow { Stats = p });

        var maxWins = rankedPlayers.Count == 0 ? 0 : rankedPlayers.Max(p => p.Wins);
        WinsChart.Clear();
        foreach (var p in rankedPlayers)
        {
            var normalized = maxWins == 0 ? 0 : p.Wins / (double)maxWins;
            WinsChart.Add(new ChartBarItem(p.DisplayName, p.Wins, p.Wins.ToString(), normalized));
        }

        var weekBuckets = snapshot.MatchesPerWeek.TakeLast(12).ToList();
        var maxWeekCount = weekBuckets.Count == 0 ? 0 : weekBuckets.Max(b => b.Count);
        MatchesPerWeekChart.Clear();
        foreach (var bucket in weekBuckets)
        {
            var normalized = maxWeekCount == 0 ? 0 : bucket.Count / (double)maxWeekCount;
            MatchesPerWeekChart.Add(new ChartBarItem(bucket.Label, bucket.Count, bucket.Count.ToString(), normalized));
        }
    }
}
