using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;
using PingPongStats.Core.Services.Badges;

namespace PingPongStats.Core.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;

    public string DataPath => _settingsRepository.Load().DataPath;

    [ObservableProperty] private int totalMatches;
    [ObservableProperty] private int activePlayerCount;
    [ObservableProperty] private string mostWinsLabel = "–";
    [ObservableProperty] private string highestOverallWinRateLabel = "–";
    [ObservableProperty] private string bestCurrentStreakLabel = "–";
    [ObservableProperty] private bool hasData;

    /// <summary>"Player of the Week": always computed over the fixed last-7-days
    /// window per spec, independent of RangeFilter (which only affects the ranking
    /// table and charts below it).</summary>
    [ObservableProperty] private bool hasPlayerOfTheWeek;
    [ObservableProperty] private Player? playerOfTheWeek;
    [ObservableProperty] private string playerOfTheWeekRecordLabel = string.Empty;
    [ObservableProperty] private string playerOfTheWeekScoreLabel = string.Empty;

    /// <summary>"Bestes Comeback": respects RangeFilter like the ranking table below,
    /// unlike Player of the Week which is always a fixed 7-day window.</summary>
    [ObservableProperty] private bool hasBestComeback;
    [ObservableProperty] private Player? comebackWinner;
    [ObservableProperty] private string comebackOpponentName = string.Empty;
    [ObservableProperty] private string comebackSetProgressionLabel = string.Empty;
    [ObservableProperty] private string comebackDateLabel = string.Empty;
    [ObservableProperty] private string comebackValueLabel = string.Empty;

    /// <summary>"Rivalität des Monats": always a fixed 30-day window (Phase 9),
    /// independent of RangeFilter, like Player of the Week's fixed 7-day window.</summary>
    [ObservableProperty] private bool hasRivalryOfTheMonth;
    [ObservableProperty] private Player? rivalryPlayer1;
    [ObservableProperty] private Player? rivalryPlayer2;
    [ObservableProperty] private string rivalryRecordLabel = string.Empty;

    /// <summary>"Streak-Alarm" (Phase 13): always computed over the player's full
    /// history, independent of RangeFilter - a streak that got truncated by a
    /// time-range filter would be misleading.</summary>
    [ObservableProperty] private bool hasStreakAlarms;
    public ObservableCollection<StreakAlarmRow> StreakAlarms { get; } = new();

    /// <summary>Shared with DoublesViewModel so changing the time range on either
    /// dashboard page keeps both in sync.</summary>
    public DashboardRangeFilter RangeFilter { get; }

    public DashboardRangeOption[] RangeOptions => DashboardRangeFilter.Options;

    /// <summary>Ranked (by Elo) players with every per-player stat the dashboard
    /// requires (win rate, 30d win rate, streaks, recent form, Elo). This single
    /// table covers most required dashboard figures without needing extra charts.</summary>
    public ObservableCollection<PlayerRankingRow> Ranking { get; } = new();

    public ObservableCollection<ChartBarItem> WinsChart { get; } = new();
    public ObservableCollection<ChartBarItem> WinRateChart { get; } = new();
    public ObservableCollection<ChartBarItem> MatchesPerWeekChart { get; } = new();
    public ObservableCollection<ChartBarItem> MatchesPerMonthChart { get; } = new();

    public IRelayCommand RefreshCommand { get; }

    /// <summary>Elo-Rangliste drill-down (R3): clicking a player's S (Siege)
    /// or N (Niederlagen) value raises these for MainViewModel to navigate to
    /// the Matches page pre-filtered to that player + result type.</summary>
    public IRelayCommand<Guid> ShowPlayerWinsCommand { get; }
    public IRelayCommand<Guid> ShowPlayerLossesCommand { get; }
    public event Action<Guid>? PlayerWinsRequested;
    public event Action<Guid>? PlayerLossesRequested;

    public DashboardViewModel(
        PingPongDataService dataService, DashboardRangeFilter rangeFilter, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        RangeFilter = rangeFilter;
        RangeFilter.Changed += Load;
        RefreshCommand = new RelayCommand(Load);
        ShowPlayerWinsCommand = new RelayCommand<Guid>(id => PlayerWinsRequested?.Invoke(id));
        ShowPlayerLossesCommand = new RelayCommand<Guid>(id => PlayerLossesRequested?.Invoke(id));
        Load();
    }

    public void Load()
    {
        var now = DateTime.Now;
        var from = RangeFilter.GetFromDate(now);
        var matchesInRange = from is null
            ? _dataService.Matches.ToList()
            : _dataService.Matches.Where(m => m.PlayedAt >= from.Value).ToList();

        var snapshot = DashboardService.BuildDashboard(_dataService.Players.ToList(), matchesInRange);

        TotalMatches = snapshot.TotalMatches;
        ActivePlayerCount = snapshot.ActivePlayerCount;
        HasData = snapshot.TotalMatches > 0;

        MostWinsLabel = snapshot.MostWins is null
            ? "–"
            : $"{snapshot.MostWins.DisplayName} ({snapshot.MostWins.Wins})";
        HighestOverallWinRateLabel = snapshot.HighestOverallWinRate is null
            ? "–"
            : $"{snapshot.HighestOverallWinRate.DisplayName} ({snapshot.HighestOverallWinRate.WinRatePct:F0}%)";
        BestCurrentStreakLabel = snapshot.BestCurrentStreak is null
            ? "–"
            : $"{snapshot.BestCurrentStreak.DisplayName} ({snapshot.BestCurrentStreak.CurrentStreak.Length}x)";

        var rankedPlayers = snapshot.Players
            .Where(p => p.Played > 0)
            .OrderByDescending(p => p.EloRating)
            .ToList();

        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        var badgeContext = BadgeEngine.BuildContext(_dataService.Players, _dataService.Matches, _dataService.DoubleMatches, _dataService.Tournaments, _dataService.Bets, _dataService.ActiveSeason);

        Ranking.Clear();
        foreach (var p in rankedPlayers)
        {
            Ranking.Add(new PlayerRankingRow
            {
                Stats = p,
                Player = playersById.GetValueOrDefault(p.PlayerId),
                Badges = BadgeEngine.EvaluateForPlayer(p.PlayerId, badgeContext),
                // R4: same matchesInRange the ranking/Stats.Wins-Losses are built
                // from, so the S/N tooltip's row count always matches the
                // displayed number. No doubles here - the Elo-Rangliste (and its
                // Wins/Losses) is singles-only, see ABWEICHUNGEN.md.
                WinsDetail = PlayerResultDetailService.GetRecentResults(
                    _dataService.Players, matchesInRange, Array.Empty<DoubleMatch>(), p.PlayerId, winsOnly: true),
                LossesDetail = PlayerResultDetailService.GetRecentResults(
                    _dataService.Players, matchesInRange, Array.Empty<DoubleMatch>(), p.PlayerId, winsOnly: false),
            });
        }

        var maxWins = rankedPlayers.Count == 0 ? 0 : rankedPlayers.Max(p => p.Wins);
        WinsChart.Clear();
        foreach (var p in rankedPlayers)
        {
            var normalized = maxWins == 0 ? 0 : p.Wins / (double)maxWins;
            WinsChart.Add(new ChartBarItem(p.DisplayName, p.Wins, p.Wins.ToString(), normalized));
        }

        WinRateChart.Clear();
        foreach (var p in rankedPlayers)
        {
            var normalized = p.WinRatePct / 100.0;
            WinRateChart.Add(new ChartBarItem(p.DisplayName, p.WinRatePct, $"{p.WinRatePct:F0}%", normalized));
        }

        var weekBuckets = snapshot.MatchesPerWeek.TakeLast(12).ToList();
        var maxWeekCount = weekBuckets.Count == 0 ? 0 : weekBuckets.Max(b => b.Count);
        MatchesPerWeekChart.Clear();
        foreach (var bucket in weekBuckets)
        {
            var normalized = maxWeekCount == 0 ? 0 : bucket.Count / (double)maxWeekCount;
            MatchesPerWeekChart.Add(new ChartBarItem(bucket.Label, bucket.Count, bucket.Count.ToString(), normalized));
        }

        var monthBuckets = snapshot.MatchesPerMonth.TakeLast(12).ToList();
        var maxMonthCount = monthBuckets.Count == 0 ? 0 : monthBuckets.Max(b => b.Count);
        MatchesPerMonthChart.Clear();
        foreach (var bucket in monthBuckets)
        {
            var normalized = maxMonthCount == 0 ? 0 : bucket.Count / (double)maxMonthCount;
            MatchesPerMonthChart.Add(new ChartBarItem(bucket.Label, bucket.Count, bucket.Count.ToString(), normalized));
        }

        var eloRatings = EloService.ComputeRatings(_dataService.Matches, _dataService.Players.Select(p => p.Id));
        var potw = PlayerOfTheWeekService.Compute(
            _dataService.Matches, _dataService.DoubleMatches, _dataService.Players.Select(p => p.Id), eloRatings);

        HasPlayerOfTheWeek = potw is not null;
        if (potw is not null)
        {
            PlayerOfTheWeek = playersById.GetValueOrDefault(potw.PlayerId);
            PlayerOfTheWeekRecordLabel = $"{potw.Wins}S / {potw.Losses}N ({potw.WinRatePct:F0}%)";
            PlayerOfTheWeekScoreLabel = $"Score: {potw.Score:F1}";
        }
        else
        {
            PlayerOfTheWeek = null;
            PlayerOfTheWeekRecordLabel = string.Empty;
            PlayerOfTheWeekScoreLabel = string.Empty;
        }

        var comeback = ComebackService.FindBestComeback(matchesInRange);
        HasBestComeback = comeback is not null;
        if (comeback is not null)
        {
            ComebackWinner = playersById.GetValueOrDefault(comeback.WinnerId);
            ComebackOpponentName = playersById.GetValueOrDefault(comeback.LoserId)?.DisplayName ?? "?";
            ComebackDateLabel = comeback.PlayedAt.ToString("dd.MM.yyyy");
            ComebackValueLabel = $"Aufgeholter Rückstand: {comeback.ComebackValue} Sätze";
            ComebackSetProgressionLabel = string.Join("  ", comeback.SetResults.Select(s => comeback.WinnerIsPlayerA
                ? $"{s.PointsA}:{s.PointsB}"
                : $"{s.PointsB}:{s.PointsA}"));
        }
        else
        {
            ComebackWinner = null;
            ComebackOpponentName = string.Empty;
            ComebackDateLabel = string.Empty;
            ComebackValueLabel = string.Empty;
            ComebackSetProgressionLabel = string.Empty;
        }

        var rivalry = RivalryService.FindRivalryOfTheMonth(_dataService.Matches);
        HasRivalryOfTheMonth = rivalry is not null;
        if (rivalry is not null)
        {
            RivalryPlayer1 = playersById.GetValueOrDefault(rivalry.Player1Id);
            RivalryPlayer2 = playersById.GetValueOrDefault(rivalry.Player2Id);
            var name1 = RivalryPlayer1?.DisplayName ?? "?";
            var name2 = RivalryPlayer2?.DisplayName ?? "?";
            RivalryRecordLabel = $"{name1} {rivalry.Player1Wins}:{rivalry.Player2Wins} {name2} ({rivalry.TotalGames} Spiele)";
        }
        else
        {
            RivalryPlayer1 = null;
            RivalryPlayer2 = null;
            RivalryRecordLabel = string.Empty;
        }

        var streaks = StreakAlarmService.GetPlayersOnStreak(_dataService.Players, _dataService.Matches, _dataService.DoubleMatches);
        StreakAlarms.Clear();
        foreach (var s in streaks)
        {
            var player = playersById.GetValueOrDefault(s.PlayerId);
            if (player is null) continue;
            StreakAlarms.Add(new StreakAlarmRow { Player = player, StreakLength = s.StreakLength });
        }

        HasStreakAlarms = StreakAlarms.Count > 0;
    }
}
