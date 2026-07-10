import type { MatchRecord } from "./types";

export const DEFAULT_ELO_RATING = 1500;
export const DEFAULT_K_FACTOR = 32;

/**
 * Computes Elo ratings for all players based on the full match history,
 * processed in chronological order. Players who never played keep the
 * default rating.
 */
export function computeEloRatings(
  matches: MatchRecord[],
  playerIds: string[],
  kFactor: number = DEFAULT_K_FACTOR,
  initialRating: number = DEFAULT_ELO_RATING
): Map<string, number> {
  const ratings = new Map<string, number>();
  for (const id of playerIds) ratings.set(id, initialRating);

  const ordered = matches
    .slice()
    .sort((a, b) => a.playedAt.getTime() - b.playedAt.getTime());

  for (const match of ordered) {
    const ratingA = ratings.get(match.playerAId) ?? initialRating;
    const ratingB = ratings.get(match.playerBId) ?? initialRating;

    const expectedA = 1 / (1 + 10 ** ((ratingB - ratingA) / 400));
    const expectedB = 1 - expectedA;

    const scoreA = match.winnerId === match.playerAId ? 1 : 0;
    const scoreB = 1 - scoreA;

    ratings.set(match.playerAId, ratingA + kFactor * (scoreA - expectedA));
    ratings.set(match.playerBId, ratingB + kFactor * (scoreB - expectedB));
  }

  return ratings;
}
