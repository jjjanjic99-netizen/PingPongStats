using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

public partial class HeadToHeadViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;

    [ObservableProperty] private Player? playerA;
    [ObservableProperty] private Player? playerB;
    [ObservableProperty] private bool hasResult;
    [ObservableProperty] private int totalGames;
    [ObservableProperty] private int playerAWins;
    [ObservableProperty] private int playerBWins;
    [ObservableProperty] private double playerAWinRatePct;
    [ObservableProperty] private double playerBWinRatePct;
    [ObservableProperty] private string errorMessage = string.Empty;

    public ObservableCollection<Player> AvailablePlayers { get; } = new();
    public ObservableCollection<MatchRow> RecentMatches { get; } = new();

    public HeadToHeadViewModel(PingPongDataService dataService)
    {
        _dataService = dataService;
        Load();
    }

    public void Load()
    {
        AvailablePlayers.Clear();
        foreach (var p in _dataService.Players.OrderBy(p => p.DisplayName)) AvailablePlayers.Add(p);

        PlayerA ??= AvailablePlayers.ElementAtOrDefault(0);
        PlayerB ??= AvailablePlayers.ElementAtOrDefault(1);

        Recalculate();
    }

    partial void OnPlayerAChanged(Player? value) => Recalculate();

    partial void OnPlayerBChanged(Player? value) => Recalculate();

    private void Recalculate()
    {
        RecentMatches.Clear();

        if (PlayerA is null || PlayerB is null || PlayerA.Id == PlayerB.Id)
        {
            HasResult = false;
            ErrorMessage = PlayerA is not null && PlayerB is not null && PlayerA.Id == PlayerB.Id
                ? "Bitte zwei unterschiedliche Spieler auswählen."
                : string.Empty;
            return;
        }

        ErrorMessage = string.Empty;
        var matches = _dataService.Matches.ToList();
        var stats = StatsService.GetHeadToHead(matches, PlayerA.Id, PlayerB.Id);

        TotalGames = stats.TotalGames;
        PlayerAWins = stats.PlayerAWins;
        PlayerBWins = stats.PlayerBWins;
        PlayerAWinRatePct = stats.PlayerAWinRatePct;
        PlayerBWinRatePct = stats.PlayerBWinRatePct;
        HasResult = true;

        var relevant = matches
            .Where(m =>
                (m.PlayerAId == PlayerA.Id && m.PlayerBId == PlayerB.Id) ||
                (m.PlayerAId == PlayerB.Id && m.PlayerBId == PlayerA.Id))
            .OrderByDescending(m => m.PlayedAt);

        foreach (var m in relevant)
        {
            var aSets = m.PlayerAId == PlayerA.Id ? m.PlayerASets : m.PlayerBSets;
            var bSets = m.PlayerAId == PlayerA.Id ? m.PlayerBSets : m.PlayerASets;
            var winnerName = m.WinnerId == PlayerA.Id ? PlayerA.DisplayName : PlayerB.DisplayName;

            // Re-orient the display so "PlayerA"/"PlayerB" in the row always match
            // the two selected head-to-head players, regardless of the original
            // match's stored side.
            var displayMatch = new Match
            {
                Id = m.Id,
                PlayedAt = m.PlayedAt,
                PlayerAId = PlayerA.Id,
                PlayerBId = PlayerB.Id,
                PlayerASets = aSets,
                PlayerBSets = bSets,
                WinnerId = m.WinnerId,
                Notes = m.Notes,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
            };

            RecentMatches.Add(new MatchRow
            {
                Match = displayMatch,
                PlayerAName = PlayerA.DisplayName,
                PlayerBName = PlayerB.DisplayName,
                WinnerName = winnerName,
            });
        }
    }
}
