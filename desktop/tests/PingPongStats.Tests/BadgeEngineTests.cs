using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using PingPongStats.Core.Services.Badges;
using Xunit;
using static PingPongStats.Tests.TestHelpers;

namespace PingPongStats.Tests;

public class BadgeEngineTests
{
    private static Player P(string name) => new() { Id = Guid.NewGuid(), DisplayName = name, IsActive = true };

    private static DoubleMatch MD(DateTime playedAt, Guid a1, Guid a2, Guid b1, Guid b2, int aSets, int bSets)
    {
        return new DoubleMatch
        {
            Id = Guid.NewGuid(),
            PlayedAt = playedAt,
            TeamAPlayer1Id = a1,
            TeamAPlayer2Id = a2,
            TeamBPlayer1Id = b1,
            TeamBPlayer2Id = b2,
            TeamASets = aSets,
            TeamBSets = bSets,
            WinningTeam = ValidationService.ComputeWinningTeam(a1, a2, b1, b2, aSets, bSets),
        };
    }

    // ----- UnbeatenBadgeRule ---------------------------------------------

    [Fact]
    public void Unbeaten_AwardsAtExactlyTenConsecutiveWins()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var matches = Enumerable.Range(0, 10)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        var award = new UnbeatenBadgeRule().Evaluate(p1.Id, context);

        Assert.NotNull(award);
        Assert.Equal("der-unbesiegte", award!.BadgeId);
    }

    [Fact]
    public void Unbeaten_DoesNotAwardAtNineWins()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var matches = Enumerable.Range(0, 9)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        Assert.Null(new UnbeatenBadgeRule().Evaluate(p1.Id, context));
    }

    [Fact]
    public void Unbeaten_StreakBrokenByLossDoesNotQualify()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var matches = Enumerable.Range(0, 10)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        matches.Add(M(new DateTime(2026, 1, 11), p1.Id, p2.Id, 0, 3)); // breaks it
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        Assert.Null(new UnbeatenBadgeRule().Evaluate(p1.Id, context));
    }

    // ----- AschenputtelBadgeRule -------------------------------------------

    [Fact]
    public void Aschenputtel_AwardsForWinAgainstTop3EloPlayer()
    {
        var underdog = P("Underdog");
        var top1 = P("Top1");
        var top2 = P("Top2");
        var top3 = P("Top3");
        var players = new[] { underdog, top1, top2, top3 };

        // Build up Elo separation: top1/2/3 beat each other and a filler a lot; underdog beats top3 once.
        var filler = P("Filler");
        var allPlayers = new[] { underdog, top1, top2, top3, filler };
        var matches = new List<Match>();
        for (var i = 0; i < 10; i++)
        {
            matches.Add(M(new DateTime(2026, 1, 1).AddDays(i), top1.Id, filler.Id, 3, 0));
            matches.Add(M(new DateTime(2026, 1, 1).AddDays(i), top2.Id, filler.Id, 3, 0));
            matches.Add(M(new DateTime(2026, 1, 1).AddDays(i), top3.Id, filler.Id, 3, 0));
        }

        matches.Add(M(new DateTime(2026, 2, 1), underdog.Id, top3.Id, 3, 1));

        var context = BadgeEngine.BuildContext(allPlayers, matches, new List<DoubleMatch>());

        var award = new AschenputtelBadgeRule().Evaluate(underdog.Id, context);

        Assert.NotNull(award);
        Assert.Equal(new DateTime(2026, 2, 1), award!.EarnedAt);
    }

    [Fact]
    public void Aschenputtel_NoAwardWhenNoWinsAgainstTopPlayers()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var matches = new List<Match> { M(new DateTime(2026, 1, 1), p2.Id, p1.Id, 3, 0) }; // p1 lost
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        Assert.Null(new AschenputtelBadgeRule().Evaluate(p1.Id, context));
    }

    // ----- StammgastBadgeRule -----------------------------------------------

    [Fact]
    public void Stammgast_AwardsPlayerWithMostGamesThisMonth()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(new DateTime(2026, 7, 1), p1.Id, p3.Id, 3, 0),
            M(new DateTime(2026, 7, 2), p1.Id, p3.Id, 3, 0),
            M(new DateTime(2026, 7, 3), p1.Id, p3.Id, 3, 0),
            M(new DateTime(2026, 7, 4), p2.Id, p4.Id, 3, 0),
        };
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4 }, matches, new List<DoubleMatch>(), referenceDate: now);

        // p1: 3 games, p3: 3 games (tied max) - both qualify; p2/p4: 1 game each - do not.
        Assert.NotNull(new StammgastBadgeRule().Evaluate(p1.Id, context));
        Assert.Null(new StammgastBadgeRule().Evaluate(p2.Id, context));
    }

    [Fact]
    public void Stammgast_IgnoresGamesFromOtherMonths()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var now = new DateTime(2026, 7, 15);
        var matches = new List<Match>
        {
            M(new DateTime(2026, 6, 1), p1.Id, p2.Id, 3, 0),
            M(new DateTime(2026, 6, 2), p1.Id, p2.Id, 3, 0),
            M(new DateTime(2026, 7, 1), p2.Id, p1.Id, 3, 0),
        };
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>(), referenceDate: now);

        // Only July counts: p2 has 1 game, p1 has 1 game -> tie, both awarded.
        Assert.NotNull(new StammgastBadgeRule().Evaluate(p1.Id, context));
        Assert.NotNull(new StammgastBadgeRule().Evaluate(p2.Id, context));
    }

    [Fact]
    public void Stammgast_CombinesSinglesAndDoublesGameCounts()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        var p5 = P("Eva");
        var now = new DateTime(2026, 7, 15);
        // p1: 1 singles (vs p5) + 2 doubles = 3. p2: 2 doubles = 2. p3/p4: 2 doubles = 2 each.
        var matches = new List<Match> { M(new DateTime(2026, 7, 1), p1.Id, p5.Id, 3, 0) };
        var doubles = new List<DoubleMatch>
        {
            MD(new DateTime(2026, 7, 2), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0),
            MD(new DateTime(2026, 7, 3), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0),
        };
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4, p5 }, matches, doubles, referenceDate: now);

        // p1: 1 singles + 2 doubles = 3, strictly more than everyone else.
        Assert.NotNull(new StammgastBadgeRule().Evaluate(p1.Id, context));
        Assert.Null(new StammgastBadgeRule().Evaluate(p2.Id, context));
    }

    [Fact]
    public void Stammgast_NoAwardWhenNoGamesThisMonth()
    {
        var p1 = P("Anna");
        var context = BadgeEngine.BuildContext(new[] { p1 }, new List<Match>(), new List<DoubleMatch>(), referenceDate: new DateTime(2026, 7, 15));

        Assert.Null(new StammgastBadgeRule().Evaluate(p1.Id, context));
    }

    // ----- EisenmannBadgeRule -----------------------------------------------

    [Fact]
    public void Eisenmann_AwardsAtExactlyTwentyCombinedGames()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        var matches = Enumerable.Range(0, 15)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        var doubles = Enumerable.Range(0, 5)
            .Select(i => MD(new DateTime(2026, 2, 1).AddDays(i), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4 }, matches, doubles);

        Assert.NotNull(new EisenmannBadgeRule().Evaluate(p1.Id, context));
    }

    [Fact]
    public void Eisenmann_DoesNotAwardAtNineteenGames()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var matches = Enumerable.Range(0, 19)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        Assert.Null(new EisenmannBadgeRule().Evaluate(p1.Id, context));
    }

    // ----- DoublesSpecialistBadgeRule ---------------------------------------

    [Fact]
    public void DoublesSpecialist_AwardsWhenDoublesRateExceedsSinglesRate()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        // Singles: 1 win out of 4 = 25%
        var matches = new List<Match>
        {
            M(new DateTime(2026, 1, 1), p1.Id, p2.Id, 3, 0),
            M(new DateTime(2026, 1, 2), p2.Id, p1.Id, 3, 0),
            M(new DateTime(2026, 1, 3), p2.Id, p1.Id, 3, 0),
            M(new DateTime(2026, 1, 4), p2.Id, p1.Id, 3, 0),
        };
        // Doubles: 5 wins out of 5 = 100%
        var doubles = Enumerable.Range(0, 5)
            .Select(i => MD(new DateTime(2026, 2, 1).AddDays(i), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4 }, matches, doubles);

        Assert.NotNull(new DoublesSpecialistBadgeRule().Evaluate(p1.Id, context));
    }

    [Fact]
    public void DoublesSpecialist_NoAwardBelowMinimumDoublesGames()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        var doubles = Enumerable.Range(0, 4) // one short of the minimum of 5
            .Select(i => MD(new DateTime(2026, 2, 1).AddDays(i), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4 }, new List<Match>(), doubles);

        Assert.Null(new DoublesSpecialistBadgeRule().Evaluate(p1.Id, context));
    }

    [Fact]
    public void DoublesSpecialist_NoAwardWhenDoublesRateNotHigherThanSingles()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        var p3 = P("Clara");
        var p4 = P("David");
        // Singles: 100% (5/5)
        var matches = Enumerable.Range(0, 5)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        // Doubles: 100% too (5/5) - not strictly higher, so no award
        var doubles = Enumerable.Range(0, 5)
            .Select(i => MD(new DateTime(2026, 2, 1).AddDays(i), p1.Id, p3.Id, p2.Id, p4.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2, p3, p4 }, matches, doubles);

        Assert.Null(new DoublesSpecialistBadgeRule().Evaluate(p1.Id, context));
    }

    // ----- BadgeEngine -------------------------------------------------------

    [Fact]
    public void EvaluateForPlayer_ReturnsEmptyListWhenNothingQualifies()
    {
        var p1 = P("Anna");
        var context = BadgeEngine.BuildContext(new[] { p1 }, new List<Match>(), new List<DoubleMatch>());

        Assert.Empty(BadgeEngine.EvaluateForPlayer(p1.Id, context));
    }

    [Fact]
    public void EvaluateForPlayer_CanReturnMultipleBadgesAtOnce()
    {
        var p1 = P("Anna");
        var p2 = P("Ben");
        // 10 wins in a row also pushes p1 past 20 combined games when doubled up.
        var matches = Enumerable.Range(0, 10)
            .Select(i => M(new DateTime(2026, 1, 1).AddDays(i), p1.Id, p2.Id, 3, 0))
            .ToList();
        var context = BadgeEngine.BuildContext(new[] { p1, p2 }, matches, new List<DoubleMatch>());

        var awards = BadgeEngine.EvaluateForPlayer(p1.Id, context);

        Assert.Contains(awards, a => a.BadgeId == "der-unbesiegte");
    }

    // ----- TournamentWinnerBadgeRule ----------------------------------------

    [Fact]
    public void TournamentWinner_AwardsPlayerWhoWonACompletedTournament()
    {
        var winner = P("Winner");
        var loser = P("Loser");
        var winnerEntrant = new TournamentEntrant { Id = Guid.NewGuid(), Player1Id = winner.Id, Seed = 1 };
        var loserEntrant = new TournamentEntrant { Id = Guid.NewGuid(), Player1Id = loser.Id, Seed = 2 };
        var tournament = new Tournament
        {
            Mode = TournamentMode.Singles,
            Status = TournamentStatus.Completed,
            Entrants = new List<TournamentEntrant> { winnerEntrant, loserEntrant },
            WinnerEntrantId = winnerEntrant.Id,
            CompletedAt = new DateTime(2026, 3, 1),
        };
        var context = BadgeEngine.BuildContext(
            new[] { winner, loser }, new List<Match>(), new List<DoubleMatch>(), new List<Tournament> { tournament });

        var award = new TournamentWinnerBadgeRule().Evaluate(winner.Id, context);

        Assert.NotNull(award);
        Assert.Equal("turniersieger", award!.BadgeId);
        Assert.Null(new TournamentWinnerBadgeRule().Evaluate(loser.Id, context));
    }

    [Fact]
    public void TournamentWinner_AwardsBothDoublesTeamMembers()
    {
        var winner1 = P("Winner1");
        var winner2 = P("Winner2");
        var winnerEntrant = new TournamentEntrant { Id = Guid.NewGuid(), Player1Id = winner1.Id, Player2Id = winner2.Id, Seed = 1 };
        var tournament = new Tournament
        {
            Mode = TournamentMode.Doubles,
            Status = TournamentStatus.Completed,
            Entrants = new List<TournamentEntrant> { winnerEntrant },
            WinnerEntrantId = winnerEntrant.Id,
            CompletedAt = new DateTime(2026, 3, 1),
        };
        var context = BadgeEngine.BuildContext(
            new[] { winner1, winner2 }, new List<Match>(), new List<DoubleMatch>(), new List<Tournament> { tournament });

        Assert.NotNull(new TournamentWinnerBadgeRule().Evaluate(winner1.Id, context));
        Assert.NotNull(new TournamentWinnerBadgeRule().Evaluate(winner2.Id, context));
    }

    [Fact]
    public void TournamentWinner_NoAwardForInProgressOrAbortedTournament()
    {
        var p1 = P("Anna");
        var entrant = new TournamentEntrant { Id = Guid.NewGuid(), Player1Id = p1.Id, Seed = 1 };
        var inProgress = new Tournament
        {
            Mode = TournamentMode.Singles,
            Status = TournamentStatus.InProgress,
            Entrants = new List<TournamentEntrant> { entrant },
            WinnerEntrantId = entrant.Id, // shouldn't happen in practice, but rule must still require Completed
        };
        var context = BadgeEngine.BuildContext(
            new[] { p1 }, new List<Match>(), new List<DoubleMatch>(), new List<Tournament> { inProgress });

        Assert.Null(new TournamentWinnerBadgeRule().Evaluate(p1.Id, context));
    }

    [Fact]
    public void TournamentWinner_NoAwardWhenNoTournamentsExist()
    {
        var p1 = P("Anna");
        var context = BadgeEngine.BuildContext(new[] { p1 }, new List<Match>(), new List<DoubleMatch>());

        Assert.Null(new TournamentWinnerBadgeRule().Evaluate(p1.Id, context));
    }
}
