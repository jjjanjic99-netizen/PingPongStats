namespace PingPongStats.Core.Services;

/// <summary>
/// Pre-match win probability from the standard Elo expectation formula (same
/// formula EloService already uses internally for rating updates - this just
/// exposes it for display before a match is even played). For doubles, team
/// Elo is the plain average of the two players' (singles) Elo ratings.
/// </summary>
public static class EloPredictionService
{
    /// <summary>Probability (0..1) that a player/team rated eloA beats one rated
    /// eloB: 1 / (1 + 10^((eloB - eloA) / 400)).</summary>
    public static double ComputeWinProbability(double eloA, double eloB) =>
        1.0 / (1.0 + Math.Pow(10, (eloB - eloA) / 400.0));

    public static double ComputeTeamElo(double player1Elo, double player2Elo) => (player1Elo + player2Elo) / 2.0;
}
