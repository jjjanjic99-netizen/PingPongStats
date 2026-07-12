using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>"Hall of Fame" page (Phase 16): completed seasons and tournaments
/// with their winners, plus the all-time record board.</summary>
public partial class HallOfFameViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;

    public string DataPath => _settingsRepository.Load().DataPath;

    [ObservableProperty] private bool hasCompletedSeasons;

    public ObservableCollection<SeasonHallOfFameRow> CompletedSeasons { get; } = new();
    public ObservableCollection<TournamentSummaryRow> CompletedTournaments { get; } = new();
    public ObservableCollection<RecordBoardRow> RecordBoard { get; } = new();

    public IRelayCommand RefreshCommand { get; }

    public HallOfFameViewModel(PingPongDataService dataService, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;
        RefreshCommand = new RelayCommand(Load);
        Load();
    }

    public void Load()
    {
        var playersById = _dataService.Players.ToDictionary(p => p.Id);
        var now = Clock.Now();

        CompletedSeasons.Clear();
        foreach (var season in HallOfFameService.GetCompletedSeasons(_dataService.Seasons, now))
        {
            var summary = HallOfFameService.BuildSeasonSummary(season, _dataService.Matches, _dataService.DoubleMatches);

            var podium = new List<LeagueTableDisplayRow>();
            var rank = 1;
            foreach (var row in summary.SinglesTable.Take(3))
            {
                podium.Add(new LeagueTableDisplayRow { Row = row, Player = playersById.GetValueOrDefault(row.PlayerId), Rank = rank });
                rank++;
            }

            CompletedSeasons.Add(new SeasonHallOfFameRow
            {
                Season = season,
                TotalGames = summary.TotalGames,
                SinglesWinner = summary.SinglesTable.Count > 0 ? playersById.GetValueOrDefault(summary.SinglesTable[0].PlayerId) : null,
                DoublesWinner = summary.DoublesTable.Count > 0 ? playersById.GetValueOrDefault(summary.DoublesTable[0].PlayerId) : null,
                SinglesPodium = podium,
            });
        }

        HasCompletedSeasons = CompletedSeasons.Count > 0;

        CompletedTournaments.Clear();
        foreach (var t in _dataService.Tournaments.Where(t => t.Status == TournamentStatus.Completed).OrderByDescending(t => t.CompletedAt))
        {
            CompletedTournaments.Add(new TournamentSummaryRow { Tournament = t, WinnerLabel = BuildTournamentWinnerLabel(t, playersById) });
        }

        var board = HallOfFameService.BuildRecordBoard(_dataService.Players, _dataService.Matches, _dataService.DoubleMatches);
        RecordBoard.Clear();
        RecordBoard.Add(BuildRow("Längste Siegserie", board.LongestWinStreak, playersById));
        RecordBoard.Add(BuildRow("Höchstes je erreichtes Elo", board.HighestElo, playersById));
        RecordBoard.Add(BuildRow("Meiste Spiele an einem Tag", board.MostGamesInOneDay, playersById));
        RecordBoard.Add(BuildRow("Grösster Comeback", board.BiggestComeback, playersById));
        RecordBoard.Add(BuildRow("Grösste Elo-Überraschung", board.BiggestUpset, playersById));
        RecordBoard.Add(BuildRow("Meiste Spiele gesamt", board.MostTotalGames, playersById));
        RecordBoard.Add(BuildRow($"Beste Siegquote (min. {HallOfFameService.MinGamesForBestWinRate} Spiele)", board.BestWinRate, playersById));
    }

    private static RecordBoardRow BuildRow(string title, HallOfFameRecord? record, Dictionary<Guid, Player> playersById) =>
        new() { Title = title, Record = record, Player = record is null ? null : playersById.GetValueOrDefault(record.PlayerId) };

    private static string BuildTournamentWinnerLabel(Tournament tournament, Dictionary<Guid, Player> playersById)
    {
        if (tournament.WinnerEntrantId is null) return string.Empty;
        var winnerEntrant = tournament.Entrants.FirstOrDefault(e => e.Id == tournament.WinnerEntrantId);
        if (winnerEntrant is null) return string.Empty;

        var names = new List<string>();
        if (playersById.TryGetValue(winnerEntrant.Player1Id, out var p1)) names.Add(p1.DisplayName);
        if (winnerEntrant.Player2Id is Guid p2Id && playersById.TryGetValue(p2Id, out var p2)) names.Add(p2.DisplayName);
        return string.Join(" & ", names);
    }
}
