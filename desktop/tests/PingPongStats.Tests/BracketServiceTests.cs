using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class BracketServiceTests
{
    private static List<TournamentEntrant> MakeEntrants(int count)
    {
        return Enumerable.Range(1, count)
            .Select(seed => new TournamentEntrant { Id = Guid.NewGuid(), Player1Id = Guid.NewGuid(), Seed = seed })
            .ToList();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(6, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    [InlineData(16, 16)]
    public void NextPowerOfTwo_RoundsUpCorrectly(int n, int expected)
    {
        Assert.Equal(expected, BracketService.NextPowerOfTwo(n));
    }

    [Fact]
    public void GenerateSeedOrder_ForSizeTwo_IsOneVsTwo()
    {
        Assert.Equal(new[] { 1, 2 }, BracketService.GenerateSeedOrder(2));
    }

    [Fact]
    public void GenerateSeedOrder_ForSizeFour_PairsOneVsFourAndTwoVsThree()
    {
        var order = BracketService.GenerateSeedOrder(4);
        Assert.Equal(new[] { 1, 4, 2, 3 }, order);
    }

    [Fact]
    public void GenerateSeedOrder_ForSizeEight_KeepsTopSeedsApartUntilLate()
    {
        var order = BracketService.GenerateSeedOrder(8);
        Assert.Equal(new[] { 1, 8, 4, 5, 2, 7, 3, 6 }, order);

        // Seed 1 and Seed 2 are in opposite halves of the bracket -> can only meet in the final.
        var seed1Position = order.IndexOf(1);
        var seed2Position = order.IndexOf(2);
        Assert.True(seed1Position < 4);
        Assert.True(seed2Position >= 4);
    }

    [Fact]
    public void GenerateSeedOrder_RejectsNonPowerOfTwo()
    {
        Assert.Throws<ArgumentException>(() => BracketService.GenerateSeedOrder(6));
    }

    [Fact]
    public void BuildBracket_RejectsFewerThanTwoEntrants()
    {
        Assert.Throws<ValidationException>(() => BracketService.BuildBracket(MakeEntrants(1)));
    }

    [Fact]
    public void BuildBracket_FourEntrants_NoByesTwoRounds()
    {
        var entrants = MakeEntrants(4);
        var bracket = BracketService.BuildBracket(entrants);

        Assert.Equal(2, bracket.Max(s => s.Round));
        var round1 = bracket.Where(s => s.Round == 1).ToList();
        Assert.Equal(2, round1.Count);
        Assert.All(round1, s => Assert.False(s.EntrantAIsBye || s.EntrantBIsBye));
        Assert.All(round1, s => Assert.Null(s.WinnerEntrantId));

        var final = bracket.Single(s => s.Round == 2);
        Assert.Null(final.EntrantAId);
        Assert.Null(final.EntrantBId);
    }

    [Fact]
    public void BuildBracket_EightEntrants_NoByesThreeRounds()
    {
        var entrants = MakeEntrants(8);
        var bracket = BracketService.BuildBracket(entrants);

        Assert.Equal(3, bracket.Max(s => s.Round));
        Assert.Equal(4, bracket.Count(s => s.Round == 1));
        Assert.Equal(2, bracket.Count(s => s.Round == 2));
        Assert.Equal(1, bracket.Count(s => s.Round == 3));
        Assert.All(bracket.Where(s => s.Round == 1), s => Assert.False(s.EntrantAIsBye || s.EntrantBIsBye));
    }

    [Fact]
    public void BuildBracket_SixteenEntrants_NoByesFourRounds()
    {
        var entrants = MakeEntrants(16);
        var bracket = BracketService.BuildBracket(entrants);

        Assert.Equal(4, bracket.Max(s => s.Round));
        Assert.Equal(8, bracket.Count(s => s.Round == 1));
        Assert.Equal(4, bracket.Count(s => s.Round == 2));
        Assert.Equal(2, bracket.Count(s => s.Round == 3));
        Assert.Equal(1, bracket.Count(s => s.Round == 4));
    }

    [Fact]
    public void BuildBracket_SixEntrants_GivesByesToTopTwoSeeds()
    {
        var entrants = MakeEntrants(6);
        var bracket = BracketService.BuildBracket(entrants);

        // bracketSize=8, byes=2 -> seeds 7 and 8 don't exist -> per seed order
        // [1,8,4,5,2,7,3,6], seed 1 (paired with 8) and seed 2 (paired with 7) get byes.
        var round1 = bracket.Where(s => s.Round == 1).ToList();
        Assert.Equal(4, round1.Count);

        var seed1Entrant = entrants.Single(e => e.Seed == 1);
        var seed2Entrant = entrants.Single(e => e.Seed == 2);
        var seed1Slot = round1.Single(s => s.EntrantAId == seed1Entrant.Id || s.EntrantBId == seed1Entrant.Id);
        var seed2Slot = round1.Single(s => s.EntrantAId == seed2Entrant.Id || s.EntrantBId == seed2Entrant.Id);

        Assert.Equal(seed1Entrant.Id, seed1Slot.WinnerEntrantId);
        Assert.Equal(seed2Entrant.Id, seed2Slot.WinnerEntrantId);
        Assert.True(seed1Slot.EntrantAIsBye || seed1Slot.EntrantBIsBye);
        Assert.True(seed2Slot.EntrantAIsBye || seed2Slot.EntrantBIsBye);

        // The other two round-1 matches are real (seeds 3-6, all present).
        var realMatches = round1.Where(s => !s.EntrantAIsBye && !s.EntrantBIsBye).ToList();
        Assert.Equal(2, realMatches.Count);
        Assert.All(realMatches, s => Assert.Null(s.WinnerEntrantId));
    }

    [Fact]
    public void BuildBracket_ByeWinnersAreAlreadyFedIntoRoundTwo()
    {
        var entrants = MakeEntrants(6);
        var bracket = BracketService.BuildBracket(entrants);
        var seed1Entrant = entrants.Single(e => e.Seed == 1);

        var round2 = bracket.Where(s => s.Round == 2).ToList();
        Assert.Contains(round2, s => s.EntrantAId == seed1Entrant.Id || s.EntrantBId == seed1Entrant.Id);
    }

    [Fact]
    public void IsPlayable_TrueOnlyWhenBothEntrantsKnownAndUndecided()
    {
        var entrants = MakeEntrants(4);
        var bracket = BracketService.BuildBracket(entrants);

        var round1Slot = bracket.First(s => s.Round == 1);
        Assert.True(BracketService.IsPlayable(round1Slot));

        var finalSlot = bracket.First(s => s.Round == 2);
        Assert.False(BracketService.IsPlayable(finalSlot)); // entrants not yet known
    }

    [Fact]
    public void AdvanceWinner_FeedsWinnerIntoNextRoundCorrectSlot()
    {
        var entrants = MakeEntrants(4);
        var tournament = new Tournament { Mode = TournamentMode.Singles, Entrants = entrants, Bracket = BracketService.BuildBracket(entrants) };
        var round1Slots = tournament.Bracket.Where(s => s.Round == 1).OrderBy(s => s.PositionInRound).ToList();
        var firstSlot = round1Slots[0];
        var winnerId = firstSlot.EntrantAId!.Value;

        BracketService.AdvanceWinner(tournament, firstSlot.Id, winnerId, Guid.NewGuid());

        var final = tournament.Bracket.Single(s => s.Round == 2);
        Assert.Equal(winnerId, final.EntrantAId);
        Assert.Null(final.EntrantBId);
        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
    }

    [Fact]
    public void AdvanceWinner_OnFinalRound_CompletesTournament()
    {
        var entrants = MakeEntrants(2);
        var tournament = new Tournament { Mode = TournamentMode.Singles, Entrants = entrants, Bracket = BracketService.BuildBracket(entrants) };
        var finalSlot = tournament.Bracket.Single();
        var winnerId = finalSlot.EntrantAId!.Value;

        BracketService.AdvanceWinner(tournament, finalSlot.Id, winnerId, Guid.NewGuid());

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        Assert.Equal(winnerId, tournament.WinnerEntrantId);
        Assert.NotNull(tournament.CompletedAt);
    }

    [Fact]
    public void AdvanceWinner_ThrowsForUnknownSlot()
    {
        var entrants = MakeEntrants(2);
        var tournament = new Tournament { Mode = TournamentMode.Singles, Entrants = entrants, Bracket = BracketService.BuildBracket(entrants) };

        Assert.Throws<NotFoundException>(() => BracketService.AdvanceWinner(tournament, Guid.NewGuid(), Guid.NewGuid(), null));
    }

    [Fact]
    public void FullSixteenEntrantTournament_PlaysThroughToASingleWinner()
    {
        var entrants = MakeEntrants(16);
        var tournament = new Tournament { Mode = TournamentMode.Singles, Entrants = entrants, Bracket = BracketService.BuildBracket(entrants) };

        // Simulate: the lower-seeded (stronger) entrant always wins.
        for (var round = 1; round <= 4; round++)
        {
            var roundSlots = tournament.Bracket.Where(s => s.Round == round).OrderBy(s => s.PositionInRound).ToList();
            foreach (var slot in roundSlots)
            {
                if (slot.WinnerEntrantId is not null) continue; // already decided via bye
                Assert.True(BracketService.IsPlayable(slot));

                var entrantA = entrants.First(e => e.Id == slot.EntrantAId);
                var entrantB = entrants.First(e => e.Id == slot.EntrantBId);
                var winner = entrantA.Seed < entrantB.Seed ? entrantA : entrantB;
                BracketService.AdvanceWinner(tournament, slot.Id, winner.Id, Guid.NewGuid());
            }
        }

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        Assert.Equal(entrants.Single(e => e.Seed == 1).Id, tournament.WinnerEntrantId);
    }
}
