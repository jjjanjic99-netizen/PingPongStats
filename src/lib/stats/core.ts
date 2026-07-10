import type {
  HeadToHeadStats,
  MatchRecord,
  RecentFormEntry,
  StreakInfo,
} from "./types";

/** Threshold below which a player's stats are flagged as low sample size. */
export const LOW_SAMPLE_SIZE_THRESHOLD = 3;

export const LAST_MONTH_DAYS = 30;

function involvesPlayer(match: MatchRecord, playerId: string): boolean {
  return match.playerAId === playerId || match.playerBId === playerId;
}

function isWin(match: MatchRecord, playerId: string): boolean {
  return match.winnerId === playerId;
}

/** Returns matches a player took part in, sorted ascending by playedAt. */
export function matchesForPlayer(
  matches: MatchRecord[],
  playerId: string
): MatchRecord[] {
  return matches
    .filter((m) => involvesPlayer(m, playerId))
    .sort((a, b) => a.playedAt.getTime() - b.playedAt.getTime());
}

export interface WinLossRecord {
  played: number;
  wins: number;
  losses: number;
  winRatePct: number;
}

function toWinLossRecord(matches: MatchRecord[], playerId: string): WinLossRecord {
  const played = matches.length;
  const wins = matches.filter((m) => isWin(m, playerId)).length;
  const losses = played - wins;
  const winRatePct = played === 0 ? 0 : (wins / played) * 100;
  return { played, wins, losses, winRatePct };
}

/** Overall win rate: wins / played matches * 100. */
export function overallWinRate(
  matches: MatchRecord[],
  playerId: string
): WinLossRecord {
  return toWinLossRecord(matchesForPlayer(matches, playerId), playerId);
}

/**
 * Win rate over the last N days (default 30), relative to a reference date
 * (defaults to now). A match counts if playedAt >= referenceDate - N days.
 */
export function winRateInLastNDays(
  matches: MatchRecord[],
  playerId: string,
  days: number = LAST_MONTH_DAYS,
  referenceDate: Date = new Date()
): WinLossRecord {
  const cutoff = new Date(referenceDate.getTime() - days * 24 * 60 * 60 * 1000);
  const recent = matchesForPlayer(matches, playerId).filter(
    (m) => m.playedAt >= cutoff && m.playedAt <= referenceDate
  );
  return toWinLossRecord(recent, playerId);
}

/**
 * Current streak: consecutive wins (or losses) counted backward from the
 * player's most recent match. Stops at the first result that breaks the
 * streak. Returns type "NONE" with length 0 if the player has no matches.
 */
export function currentStreak(
  matches: MatchRecord[],
  playerId: string
): StreakInfo {
  const ordered = matchesForPlayer(matches, playerId).slice().reverse(); // newest first
  if (ordered.length === 0) return { type: "NONE", length: 0 };

  const latestIsWin = isWin(ordered[0], playerId);
  let length = 0;
  for (const match of ordered) {
    const win = isWin(match, playerId);
    if (win === latestIsWin) {
      length += 1;
    } else {
      break;
    }
  }
  return { type: latestIsWin ? "WIN" : "LOSS", length };
}

/**
 * Longest winning streak in a player's entire history (chronological run of
 * consecutive wins, regardless of losses in between runs).
 */
export function longestWinStreak(
  matches: MatchRecord[],
  playerId: string
): number {
  const ordered = matchesForPlayer(matches, playerId); // oldest first
  let longest = 0;
  let current = 0;
  for (const match of ordered) {
    if (isWin(match, playerId)) {
      current += 1;
      longest = Math.max(longest, current);
    } else {
      current = 0;
    }
  }
  return longest;
}

/** Average number of sets won per match by this player. */
export function averageSetsWonPerMatch(
  matches: MatchRecord[],
  playerId: string
): number {
  const played = matchesForPlayer(matches, playerId);
  if (played.length === 0) return 0;
  const totalSetsWon = played.reduce((sum, m) => {
    const setsWon = m.playerAId === playerId ? m.playerASets : m.playerBSets;
    return sum + setsWon;
  }, 0);
  return totalSetsWon / played.length;
}

/** Recent form: the last N results, newest first. */
export function recentForm(
  matches: MatchRecord[],
  playerId: string,
  n: number = 5
): RecentFormEntry[] {
  const ordered = matchesForPlayer(matches, playerId).slice().reverse();
  return ordered.slice(0, n).map((m) => ({
    matchId: m.id,
    playedAt: m.playedAt,
    result: isWin(m, playerId) ? "W" : "L",
    opponentId: m.playerAId === playerId ? m.playerBId : m.playerAId,
  }));
}

/** Head-to-head statistics between two specific players. */
export function headToHead(
  matches: MatchRecord[],
  playerAId: string,
  playerBId: string
): HeadToHeadStats {
  const relevant = matches.filter(
    (m) =>
      (m.playerAId === playerAId && m.playerBId === playerBId) ||
      (m.playerAId === playerBId && m.playerBId === playerAId)
  );
  const totalGames = relevant.length;
  const playerAWins = relevant.filter((m) => m.winnerId === playerAId).length;
  const playerBWins = relevant.filter((m) => m.winnerId === playerBId).length;

  return {
    playerAId,
    playerBId,
    totalGames,
    playerAWins,
    playerBWins,
    playerAWinRatePct: totalGames === 0 ? 0 : (playerAWins / totalGames) * 100,
    playerBWinRatePct: totalGames === 0 ? 0 : (playerBWins / totalGames) * 100,
  };
}

export function isLowSampleSize(played: number): boolean {
  return played < LOW_SAMPLE_SIZE_THRESHOLD;
}
