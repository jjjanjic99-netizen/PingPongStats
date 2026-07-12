using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class PingPongDataServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PingPongDataService _service;

    public PingPongDataServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PingPongStatsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new PingPongDataService(
            new PlayerXmlRepository(_tempDir), new MatchXmlRepository(_tempDir), new DoubleMatchXmlRepository(_tempDir),
            auditLogRepository: null, quoteRepository: null, seasonRepository: new SeasonXmlRepository(_tempDir),
            tournamentRepository: new TournamentXmlRepository(_tempDir),
            pendingMatchRepository: new PendingMatchXmlRepository(_tempDir), betRepository: new BetXmlRepository(_tempDir));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best effort cleanup
        }
    }

    [Fact]
    public void CreatePlayer_RequiresDisplayName()
    {
        Assert.Throws<ValidationException>(() => _service.CreatePlayer("", "", "", ""));
    }

    [Fact]
    public void CreateMatch_PersistsAndComputesWinner()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");

        var match = _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 1, "Testspiel");

        Assert.Equal(a.Id, match.WinnerId);
        Assert.Single(_service.Matches);
    }

    [Fact]
    public void CreateMatch_RejectsUnknownPlayer()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        Assert.Throws<ValidationException>(() => _service.CreateMatch(DateTime.Now, a.Id, Guid.NewGuid(), 3, 0, ""));
    }

    [Fact]
    public void DeletePlayer_BlockedWhenMatchesExist()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");
        _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 0, "");

        Assert.Throws<ValidationException>(() => _service.DeletePlayer(a.Id));
    }

    [Fact]
    public void DeletePlayer_AllowedWhenNoMatchesExist()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        _service.DeletePlayer(a.Id);
        Assert.Empty(_service.Players);
    }

    [Fact]
    public void SetPlayerActive_ArchivesWithoutRemovingHistory()
    {
        var a = _service.CreatePlayer("Anna", "", "", "");
        var b = _service.CreatePlayer("Ben", "", "", "");
        _service.CreateMatch(DateTime.Now, a.Id, b.Id, 3, 0, "");

        _service.SetPlayerActive(a.Id, false);

        Assert.False(_service.Players.Single(p => p.Id == a.Id).IsActive);
        Assert.Single(_service.Matches);
    }

    [Fact]
    public void CreateDoubleMatch_PersistsAndComputesWinningTeam()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");
        var b2 = _service.CreatePlayer("David", "", "", "");

        var match = _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, b2.Id, 3, 1, "Doppel-Testspiel");

        Assert.Equal("A", match.WinningTeam);
        Assert.Single(_service.DoubleMatches);
    }

    [Fact]
    public void CreateDoubleMatch_RejectsDuplicatePlayerAcrossTeams()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");

        Assert.Throws<ValidationException>(() =>
            _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, a1.Id, 3, 1, ""));
    }

    [Fact]
    public void DeleteDoubleMatch_RemovesEntry()
    {
        var a1 = _service.CreatePlayer("Anna", "", "", "");
        var a2 = _service.CreatePlayer("Ben", "", "", "");
        var b1 = _service.CreatePlayer("Clara", "", "", "");
        var b2 = _service.CreatePlayer("David", "", "", "");
        var match = _service.CreateDoubleMatch(DateTime.Now, a1.Id, a2.Id, b1.Id, b2.Id, 3, 0, "");

        _service.DeleteDoubleMatch(match.Id);

        Assert.Empty(_service.DoubleMatches);
    }

    [Fact]
    public void SetPlayerAvatar_UpdatesAvatarFileName()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");

        _service.SetPlayerAvatar(player.Id, "avatar.png");

        Assert.Equal("avatar.png", _service.Players.Single(p => p.Id == player.Id).AvatarFileName);
    }

    [Fact]
    public void SetPlayerAvatar_EmptyStringClearsAvatar()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");
        _service.SetPlayerAvatar(player.Id, "avatar.png");

        _service.SetPlayerAvatar(player.Id, "");

        Assert.Equal(string.Empty, _service.Players.Single(p => p.Id == player.Id).AvatarFileName);
    }

    [Fact]
    public void SetPlayerPin_RejectsNonFourDigitPin()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");

        Assert.Throws<ValidationException>(() => _service.SetPlayerPin(player.Id, "12"));
        Assert.False(_service.PlayerHasPin(player.Id));
    }

    [Fact]
    public void SetPlayerPin_ThenVerifyPlayerPin_AcceptsCorrectPinOnly()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");

        _service.SetPlayerPin(player.Id, "1234");

        Assert.True(_service.PlayerHasPin(player.Id));
        Assert.True(_service.VerifyPlayerPin(player.Id, "1234"));
        Assert.False(_service.VerifyPlayerPin(player.Id, "0000"));
    }

    [Fact]
    public void SetPlayerPin_NullClearsExistingPin()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");
        _service.SetPlayerPin(player.Id, "1234");

        _service.SetPlayerPin(player.Id, null);

        Assert.False(_service.PlayerHasPin(player.Id));
    }

    [Fact]
    public void PlayerHasPin_FalseByDefault()
    {
        var player = _service.CreatePlayer("Anna", "", "", "");
        Assert.False(_service.PlayerHasPin(player.Id));
    }

    [Fact]
    public void CreateSeason_RequiresName()
    {
        Assert.Throws<ValidationException>(() =>
            _service.CreateSeason("", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), isActive: true));
    }

    [Fact]
    public void CreateSeason_RejectsEndDateBeforeStartDate()
    {
        Assert.Throws<ValidationException>(() =>
            _service.CreateSeason("Saison 2026", new DateTime(2026, 6, 1), new DateTime(2026, 1, 1), isActive: true));
    }

    [Fact]
    public void CreateSeason_PersistsAndActivates()
    {
        var season = _service.CreateSeason("Saison 2026", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), isActive: true);

        Assert.Single(_service.Seasons);
        Assert.True(_service.Seasons.Single(s => s.Id == season.Id).IsActive);
        Assert.Equal(season.Id, _service.ActiveSeason?.Id);
    }

    [Fact]
    public void CreateSeason_ActivatingNewSeasonDeactivatesPreviousOne()
    {
        var first = _service.CreateSeason("Saison 1", new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), isActive: true);
        var second = _service.CreateSeason("Saison 2", new DateTime(2026, 7, 1), new DateTime(2026, 12, 31), isActive: true);

        Assert.False(_service.Seasons.Single(s => s.Id == first.Id).IsActive);
        Assert.True(_service.Seasons.Single(s => s.Id == second.Id).IsActive);
        Assert.Equal(second.Id, _service.ActiveSeason?.Id);
    }

    [Fact]
    public void SetSeasonActive_SwitchesActiveSeason()
    {
        var first = _service.CreateSeason("Saison 1", new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), isActive: true);
        var second = _service.CreateSeason("Saison 2", new DateTime(2026, 7, 1), new DateTime(2026, 12, 31), isActive: false);

        _service.SetSeasonActive(second.Id, true);

        Assert.False(_service.Seasons.Single(s => s.Id == first.Id).IsActive);
        Assert.True(_service.Seasons.Single(s => s.Id == second.Id).IsActive);
    }

    [Fact]
    public void ActiveSeason_NullWhenNoSeasonIsActive()
    {
        _service.CreateSeason("Saison 1", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), isActive: false);
        Assert.Null(_service.ActiveSeason);
    }

    [Fact]
    public void CreateSinglesTournament_BuildsSeededBracketAndSetsActiveTournament()
    {
        var players = Enumerable.Range(0, 4).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();

        var tournament = _service.CreateSinglesTournament("Turnier 1", players.Select(p => p.Id).ToList());

        Assert.Single(_service.Tournaments);
        Assert.Equal(tournament.Id, _service.ActiveTournament?.Id);
        Assert.Equal(4, tournament.Entrants.Count);
        Assert.Equal(2, tournament.Bracket.Count(s => s.Round == 1));
    }

    [Fact]
    public void CreateSinglesTournament_ThrowsWhenAnotherTournamentIsInProgress()
    {
        var players = Enumerable.Range(0, 4).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();
        _service.CreateSinglesTournament("Turnier 1", players.Select(p => p.Id).ToList());

        Assert.Throws<ValidationException>(() =>
            _service.CreateSinglesTournament("Turnier 2", players.Select(p => p.Id).ToList()));
    }

    [Fact]
    public void RecordTournamentSinglesResult_CreatesTaggedMatchAndAdvancesBracket()
    {
        var players = Enumerable.Range(0, 2).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();
        var tournament = _service.CreateSinglesTournament("Turnier", players.Select(p => p.Id).ToList());
        var slot = tournament.Bracket.Single();

        var match = _service.RecordTournamentSinglesResult(
            tournament.Id, slot.Id, DateTime.Now, players[0].Id, players[1].Id, 3, 0, "");

        Assert.Equal(tournament.Id, match.TournamentId);
        Assert.Single(_service.Matches);

        var updatedTournament = _service.Tournaments.Single(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Completed, updatedTournament.Status);
        Assert.NotNull(updatedTournament.WinnerEntrantId);
        Assert.Null(_service.ActiveTournament);
    }

    [Fact]
    public void RecordTournamentDoublesResult_CreatesTaggedMatchAndAdvancesBracket()
    {
        var a1 = _service.CreatePlayer("A1", "", "", "");
        var a2 = _service.CreatePlayer("A2", "", "", "");
        var b1 = _service.CreatePlayer("B1", "", "", "");
        var b2 = _service.CreatePlayer("B2", "", "", "");
        var teams = new List<(Guid, Guid)> { (a1.Id, a2.Id), (b1.Id, b2.Id) };
        var tournament = _service.CreateDoublesTournament("Doppel-Turnier", teams);
        var slot = tournament.Bracket.Single();

        var match = _service.RecordTournamentDoublesResult(
            tournament.Id, slot.Id, DateTime.Now, a1.Id, a2.Id, b1.Id, b2.Id, 3, 0, "");

        Assert.Equal(tournament.Id, match.TournamentId);

        var updatedTournament = _service.Tournaments.Single(t => t.Id == tournament.Id);
        Assert.Equal(TournamentStatus.Completed, updatedTournament.Status);
    }

    [Fact]
    public void AbortTournament_MarksAbortedAndAllowsNewTournamentToStart()
    {
        var players = Enumerable.Range(0, 4).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();
        var tournament = _service.CreateSinglesTournament("Turnier 1", players.Select(p => p.Id).ToList());

        _service.AbortTournament(tournament.Id);

        Assert.Equal(TournamentStatus.Aborted, _service.Tournaments.Single().Status);
        Assert.Null(_service.ActiveTournament);

        // A new tournament can now be started.
        var second = _service.CreateSinglesTournament("Turnier 2", players.Select(p => p.Id).ToList());
        Assert.Equal(second.Id, _service.ActiveTournament?.Id);
    }

    [Fact]
    public void AbortTournament_KeepsAlreadyPlayedMatchesInStats()
    {
        var players = Enumerable.Range(0, 4).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();
        var tournament = _service.CreateSinglesTournament("Turnier", players.Select(p => p.Id).ToList());
        var firstSlot = tournament.Bracket.First(s => s.Round == 1);

        _service.RecordTournamentSinglesResult(
            tournament.Id, firstSlot.Id, DateTime.Now, players[0].Id, players[1].Id, 3, 0, "");

        _service.AbortTournament(tournament.Id);

        Assert.Single(_service.Matches);
    }

    [Fact]
    public void CreateSinglesPendingMatch_IsAddedToPendingMatches()
    {
        var playerA = _service.CreatePlayer("A", "", "", "");
        var playerB = _service.CreatePlayer("B", "", "", "");

        var pendingMatch = _service.CreateSinglesPendingMatch(playerA.Id, playerB.Id);

        Assert.Single(_service.PendingMatches);
        Assert.Equal(pendingMatch.Id, _service.PendingMatches.Single().Id);
        Assert.False(pendingMatch.IsResolved);
    }

    [Fact]
    public void PlaceOrUpdateBet_BlocksBetFromAParticipant()
    {
        var playerA = _service.CreatePlayer("A", "", "", "");
        var playerB = _service.CreatePlayer("B", "", "", "");
        var pendingMatch = _service.CreateSinglesPendingMatch(playerA.Id, playerB.Id);

        Assert.Throws<ValidationException>(() =>
            _service.PlaceOrUpdateBet(pendingMatch.Id, playerA.Id, playerB.Id, null));
    }

    [Fact]
    public void RecordPendingMatchSinglesResult_ResolvesPendingMatchAndScoresBets()
    {
        var playerA = _service.CreatePlayer("A", "", "", "");
        var playerB = _service.CreatePlayer("B", "", "", "");
        var bettor = _service.CreatePlayer("Bettor", "", "", "");
        var pendingMatch = _service.CreateSinglesPendingMatch(playerA.Id, playerB.Id);

        _service.PlaceOrUpdateBet(pendingMatch.Id, bettor.Id, playerA.Id, null);

        var match = _service.RecordPendingMatchSinglesResult(pendingMatch.Id, DateTime.Now, 3, 0, "");

        Assert.Single(_service.Matches);
        Assert.True(_service.PendingMatches.Single().IsResolved);
        Assert.Equal(match.Id, _service.PendingMatches.Single().ResolvedMatchId);

        var bet = _service.Bets.Single();
        Assert.True(bet.IsResolved);
        Assert.True(bet.Points > 0); // even ratings (1000 vs 1000) -> correct pick, at least the base point
    }

    [Fact]
    public void RecordTournamentSinglesResult_AlsoResolvesALinkedPendingMatchBet()
    {
        var players = Enumerable.Range(0, 2).Select(i => _service.CreatePlayer($"P{i}", "", "", "")).ToList();
        var bettor = _service.CreatePlayer("Bettor", "", "", "");
        var tournament = _service.CreateSinglesTournament("Turnier", players.Select(p => p.Id).ToList());
        var slot = tournament.Bracket.Single();

        var pendingMatch = _service.GetOrCreatePendingMatchForSlot(tournament.Id, slot.Id);
        _service.PlaceOrUpdateBet(pendingMatch.Id, bettor.Id, players[0].Id, null);

        _service.RecordTournamentSinglesResult(tournament.Id, slot.Id, DateTime.Now, players[0].Id, players[1].Id, 3, 0, "");

        Assert.True(_service.PendingMatches.Single().IsResolved);
        Assert.True(_service.Bets.Single().IsResolved);
    }

    [Fact]
    public void RecordPendingMatchSinglesResult_ThrowsWhenAlreadyResolved()
    {
        var playerA = _service.CreatePlayer("A", "", "", "");
        var playerB = _service.CreatePlayer("B", "", "", "");
        var pendingMatch = _service.CreateSinglesPendingMatch(playerA.Id, playerB.Id);
        _service.RecordPendingMatchSinglesResult(pendingMatch.Id, DateTime.Now, 3, 0, "");

        Assert.Throws<ValidationException>(() =>
            _service.RecordPendingMatchSinglesResult(pendingMatch.Id, DateTime.Now, 3, 1, ""));
    }
}
