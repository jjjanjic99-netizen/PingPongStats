using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>Standard Elo rating calculation. Start value 1000, K-factor 32 per spec.</summary>
public static class EloService
{
    public const double DefaultInitialRating = 1000;
    public const double DefaultKFactor = 32;

    /// <summary>Computes Elo ratings for all players, processing the full match history
    /// in chronological order. Players who never played keep the initial rating.</summary>
    public static Dictionary<Guid, double> ComputeRatings(
        IEnumerable<Match> matches,
        IEnumerable<Guid> playerIds,
        double kFactor = DefaultKFactor,
        double initialRating = DefaultInitialRating)
    {
        var ratings = playerIds.ToDictionary(id => id, _ => initialRating);

        var ordered = matches.OrderBy(m => m.PlayedAt).ToList();
        foreach (var match in ordered)
        {
            var ratingA = ratings.GetValueOrDefault(match.PlayerAId, initialRating);
            var ratingB = ratings.GetValueOrDefault(match.PlayerBId, initialRating);

            var expectedA = 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
            var expectedB = 1.0 - expectedA;

            var scoreA = match.WinnerId == match.PlayerAId ? 1.0 : 0.0;
            var scoreB = 1.0 - scoreA;

            ratings[match.PlayerAId] = ratingA + kFactor * (scoreA - expectedA);
            ratings[match.PlayerBId] = ratingB + kFactor * (scoreB - expectedB);
        }

        return ratings;
    }
}
