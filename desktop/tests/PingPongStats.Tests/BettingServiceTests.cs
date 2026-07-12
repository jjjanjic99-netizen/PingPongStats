using PingPongStats.Core.Models;
using PingPongStats.Core.Services;
using Xunit;

namespace PingPongStats.Tests;

public class BettingServiceTests
{
    private static PendingMatch SinglesMatch(Guid playerAId, Guid playerBId) =>
        BettingService.CreateSinglesPendingMatch(playerAId, playerBId);

    private static PendingMatch DoublesMatch(Guid a1, Guid a2, Guid b1, Guid b2) =>
        BettingService.CreateDoublesPendingMatch(a1, a2, b1, b2);

    [Fact]
    public void PlaceOrUpdateBet_BlocksSelfBetOnSingles()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet>();

        Assert.Throws<ValidationException>(() =>
            BettingService.PlaceOrUpdateBet(bets, pendingMatch, playerA, playerB, null));
    }

    [Fact]
    public void PlaceOrUpdateBet_BlocksSelfBetOnDoubles_ForAnyOfTheFourPlayers()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var pendingMatch = DoublesMatch(a1, a2, b1, b2);
        var bets = new List<Bet>();

        Assert.Throws<ValidationException>(() => BettingService.PlaceOrUpdateBet(bets, pendingMatch, a2, null, "A"));
    }

    [Fact]
    public void PlaceOrUpdateBet_ThrowsWhenMatchAlreadyResolved()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        pendingMatch.IsResolved = true;
        var bets = new List<Bet>();

        Assert.Throws<ValidationException>(() =>
            BettingService.PlaceOrUpdateBet(bets, pendingMatch, bettor, playerA, null));
    }

    [Fact]
    public void PlaceOrUpdateBet_ThrowsForPredictionNotOneOfTheTwoSides()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet>();

        Assert.Throws<ValidationException>(() =>
            BettingService.PlaceOrUpdateBet(bets, pendingMatch, bettor, stranger, null));
    }

    [Fact]
    public void PlaceOrUpdateBet_FirstCallCreatesANewBet()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet>();

        var bet = BettingService.PlaceOrUpdateBet(bets, pendingMatch, bettor, playerA, null);

        Assert.Single(bets);
        Assert.Equal(playerA, bet.PredictedWinnerId);
        Assert.False(bet.IsResolved);
    }

    [Fact]
    public void PlaceOrUpdateBet_SecondCallByTheSamePlayerChangesTheExistingBetInstead()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet>();

        BettingService.PlaceOrUpdateBet(bets, pendingMatch, bettor, playerA, null);
        var changed = BettingService.PlaceOrUpdateBet(bets, pendingMatch, bettor, playerB, null);

        Assert.Single(bets); // still only one bet for this player on this match
        Assert.Equal(playerB, changed.PredictedWinnerId);
        Assert.Equal(playerB, bets[0].PredictedWinnerId);
    }

    [Fact]
    public void ResolveBets_AwardsOnePointForACorrectNonUnderdogPick()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet> { new() { PendingMatchId = pendingMatch.Id, BettorPlayerId = bettor, PredictedWinnerId = playerA } };

        // Even ratings: winner A's pre-match win probability is 50%, well above
        // the 40% underdog threshold.
        var ratings = new Dictionary<Guid, double> { [playerA] = 1000, [playerB] = 1000 };
        var playedAt = new DateTime(2026, 7, 1);

        BettingService.ResolveBets(bets, pendingMatch, "A", playedAt, ratings);

        Assert.True(bets[0].IsResolved);
        Assert.Equal(BettingService.PointsForCorrectPick, bets[0].Points);
        Assert.Equal(playedAt, bets[0].ResolvedAt);
    }

    [Fact]
    public void ResolveBets_AwardsThreePointsWhenTippedWinnerWasAnUnderdog()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet> { new() { PendingMatchId = pendingMatch.Id, BettorPlayerId = bettor, PredictedWinnerId = playerB } };

        // A is heavily favored (1200 vs 800) - B winning is a clear underdog upset.
        var ratings = new Dictionary<Guid, double> { [playerA] = 1200, [playerB] = 800 };
        var winProbabilityB = 1.0 - EloPredictionService.ComputeWinProbability(1200, 800);
        Assert.True(winProbabilityB < BettingService.UnderdogProbabilityThreshold); // sanity-check the test setup itself

        BettingService.ResolveBets(bets, pendingMatch, "B", new DateTime(2026, 7, 1), ratings);

        Assert.Equal(BettingService.PointsForUnderdogPick, bets[0].Points);
    }

    [Fact]
    public void ResolveBets_AwardsZeroPointsForAWrongPick()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet> { new() { PendingMatchId = pendingMatch.Id, BettorPlayerId = bettor, PredictedWinnerId = playerA } };
        var ratings = new Dictionary<Guid, double> { [playerA] = 1000, [playerB] = 1000 };

        BettingService.ResolveBets(bets, pendingMatch, "B", new DateTime(2026, 7, 1), ratings);

        Assert.Equal(0, bets[0].Points);
    }

    [Fact]
    public void ResolveBets_DoesNothingWhenNoBetsExistForThisMatch()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var pendingMatch = SinglesMatch(playerA, playerB);
        var bets = new List<Bet>();
        var ratings = new Dictionary<Guid, double> { [playerA] = 1000, [playerB] = 1000 };

        var exception = Record.Exception(() =>
            BettingService.ResolveBets(bets, pendingMatch, "A", new DateTime(2026, 7, 1), ratings));

        Assert.Null(exception);
        Assert.Empty(bets);
    }

    [Fact]
    public void ResolveBets_DoublesUsesAverageTeamEloAndCorrectSideMatching()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var bettor = Guid.NewGuid();
        var pendingMatch = DoublesMatch(a1, a2, b1, b2);
        var bets = new List<Bet> { new() { PendingMatchId = pendingMatch.Id, BettorPlayerId = bettor, PredictedWinningTeam = "A" } };
        var ratings = new Dictionary<Guid, double> { [a1] = 1000, [a2] = 1000, [b1] = 1000, [b2] = 1000 };

        BettingService.ResolveBets(bets, pendingMatch, "A", new DateTime(2026, 7, 1), ratings);

        Assert.Equal(BettingService.PointsForCorrectPick, bets[0].Points);
    }

    [Fact]
    public void GetLeaderboard_AggregatesPointsAndHitRatePerPlayer_SortedDescending()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var bets = new List<Bet>
        {
            new() { BettorPlayerId = p1, IsResolved = true, Points = 3 },
            new() { BettorPlayerId = p1, IsResolved = true, Points = 1 },
            new() { BettorPlayerId = p1, IsResolved = true, Points = 0 },
            new() { BettorPlayerId = p2, IsResolved = true, Points = 1 },
            new() { BettorPlayerId = p2, IsResolved = false, Points = null }, // unresolved - must not count
        };

        var leaderboard = BettingService.GetLeaderboard(bets);

        Assert.Equal(2, leaderboard.Count);
        Assert.Equal(p1, leaderboard[0].PlayerId);
        Assert.Equal(4, leaderboard[0].TotalPoints);
        Assert.Equal(3, leaderboard[0].BetsPlaced);
        Assert.Equal(2, leaderboard[0].CorrectPicks);
        Assert.Equal(p2, leaderboard[1].PlayerId);
        Assert.Equal(1, leaderboard[1].TotalPoints);
        Assert.Equal(1, leaderboard[1].BetsPlaced);
    }
}
