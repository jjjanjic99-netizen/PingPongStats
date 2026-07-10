import {
  averageSetsWonPerMatch,
  currentStreak,
  isLowSampleSize,
  longestWinStreak,
  matchesForPlayer,
  overallWinRate,
  winRateInLastNDays,
} from "./core";
import { computeEloRatings, DEFAULT_ELO_RATING } from "./elo";
import { recentForm } from "./core";
import {
  computePlayerAchievements,
  findMostActivePlayerId,
  type Achievement,
} from "./achievements";
import { matchesPerBucket, type TimeseriesBucket } from "./timeseries";
import type { MatchRecord, PlayerRecord, RecentFormEntry, StreakInfo } from "./types";

export interface PlayerStatsSummary {
  playerId: string;
  displayName: string;
  isActive: boolean;
  played: number;
  wins: number;
  losses: number;
  winRatePct: number;
  winRateLast30dPct: number;
  currentStreak: StreakInfo;
  longestWinStreak: number;
  avgSetsWonPerMatch: number;
  eloRating: number;
  recentForm: RecentFormEntry[];
  achievements: Achievement[];
  lowSampleSize: boolean;
}

export interface DashboardStats {
  totalMatches: number;
  activePlayerCount: number;
  players: PlayerStatsSummary[];
  mostWins: PlayerStatsSummary | null;
  highestOverallWinRate: PlayerStatsSummary | null;
  highestWinRateLastMonth: PlayerStatsSummary | null;
  bestCurrentStreak: PlayerStatsSummary | null;
  matchesPerWeek: TimeseriesBucket[];
  matchesPerMonth: TimeseriesBucket[];
  averageSetsWonPerMatchOverall: number;
}

function buildPlayerStatsSummary(
  player: PlayerRecord,
  matches: MatchRecord[],
  eloRatings: Map<string, number>,
  mostActivePlayerId: string | null
): PlayerStatsSummary {
  const overall = overallWinRate(matches, player.id);
  const last30d = winRateInLastNDays(matches, player.id);

  return {
    playerId: player.id,
    displayName: player.displayName,
    isActive: player.isActive,
    played: overall.played,
    wins: overall.wins,
    losses: overall.losses,
    winRatePct: overall.winRatePct,
    winRateLast30dPct: last30d.winRatePct,
    currentStreak: currentStreak(matches, player.id),
    longestWinStreak: longestWinStreak(matches, player.id),
    avgSetsWonPerMatch: averageSetsWonPerMatch(matches, player.id),
    eloRating: eloRatings.get(player.id) ?? DEFAULT_ELO_RATING,
    recentForm: recentForm(matches, player.id, 5),
    achievements: computePlayerAchievements(matches, player.id, mostActivePlayerId),
    lowSampleSize: isLowSampleSize(overall.played),
  };
}

function pickBy<T>(items: T[], key: (item: T) => number): T | null {
  if (items.length === 0) return null;
  return items.reduce((best, item) => (key(item) > key(best) ? item : best));
}

/**
 * Builds the complete dashboard statistics from raw player and match data.
 * Pure function: no I/O, fully unit-testable.
 */
export function buildDashboardStats(
  players: PlayerRecord[],
  matches: MatchRecord[]
): DashboardStats {
  const activePlayers = players.filter((p) => p.isActive);
  const allPlayerIds = players.map((p) => p.id);
  const eloRatings = computeEloRatings(matches, allPlayerIds);
  const mostActivePlayerId = findMostActivePlayerId(matches, allPlayerIds);

  const summaries = players.map((p) =>
    buildPlayerStatsSummary(p, matches, eloRatings, mostActivePlayerId)
  );
  const playersWithGames = summaries.filter((s) => s.played > 0);

  const totalSetsWon = matches.reduce(
    (sum, m) => sum + m.playerASets + m.playerBSets,
    0
  );
  const averageSetsWonPerMatchOverall =
    matches.length === 0 ? 0 : totalSetsWon / (matches.length * 2);

  return {
    totalMatches: matches.length,
    activePlayerCount: activePlayers.length,
    players: summaries,
    mostWins: pickBy(playersWithGames, (s) => s.wins),
    highestOverallWinRate: pickBy(playersWithGames, (s) => s.winRatePct),
    highestWinRateLastMonth: pickBy(playersWithGames, (s) => s.winRateLast30dPct),
    bestCurrentStreak: pickBy(playersWithGames, (s) =>
      s.currentStreak.type === "WIN" ? s.currentStreak.length : -1
    ),
    matchesPerWeek: matchesPerBucket(matches, "week"),
    matchesPerMonth: matchesPerBucket(matches, "month"),
    averageSetsWonPerMatchOverall,
  };
}

// Re-export matchesForPlayer for services that need per-player match slices.
export { matchesForPlayer };
