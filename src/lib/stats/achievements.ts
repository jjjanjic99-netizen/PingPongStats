import { matchesForPlayer, currentStreak } from "./core";
import type { MatchRecord } from "./types";

export type AchievementId = "HOT_STREAK" | "COMEBACK_KING" | "MOST_ACTIVE_PLAYER";

export interface Achievement {
  id: AchievementId;
  label: string;
  description: string;
}

export const ACHIEVEMENT_DEFINITIONS: Record<AchievementId, Achievement> = {
  HOT_STREAK: {
    id: "HOT_STREAK",
    label: "Hot Streak",
    description: "Aktuell mindestens 3 Siege in Folge.",
  },
  COMEBACK_KING: {
    id: "COMEBACK_KING",
    label: "Comeback King",
    description:
      "Hat mindestens einmal nach 3 oder mehr Niederlagen in Folge wieder gewonnen.",
  },
  MOST_ACTIVE_PLAYER: {
    id: "MOST_ACTIVE_PLAYER",
    label: "Most Active Player",
    description: "Spieler mit den meisten erfassten Spielen.",
  },
};

const HOT_STREAK_THRESHOLD = 3;
const COMEBACK_LOSS_THRESHOLD = 3;

function hasComebackFromLosingStreak(
  matches: MatchRecord[],
  playerId: string
): boolean {
  const ordered = matchesForPlayer(matches, playerId); // oldest first
  let lossRun = 0;
  for (const match of ordered) {
    const won = match.winnerId === playerId;
    if (won) {
      if (lossRun >= COMEBACK_LOSS_THRESHOLD) return true;
      lossRun = 0;
    } else {
      lossRun += 1;
    }
  }
  return false;
}

/**
 * Computes which achievements a player currently holds, given the full
 * match history and the total-matches-played count of every player (needed
 * to determine "Most Active Player").
 */
export function computePlayerAchievements(
  matches: MatchRecord[],
  playerId: string,
  mostActivePlayerId: string | null
): Achievement[] {
  const achievements: Achievement[] = [];

  const streak = currentStreak(matches, playerId);
  if (streak.type === "WIN" && streak.length >= HOT_STREAK_THRESHOLD) {
    achievements.push(ACHIEVEMENT_DEFINITIONS.HOT_STREAK);
  }

  if (hasComebackFromLosingStreak(matches, playerId)) {
    achievements.push(ACHIEVEMENT_DEFINITIONS.COMEBACK_KING);
  }

  if (mostActivePlayerId === playerId) {
    achievements.push(ACHIEVEMENT_DEFINITIONS.MOST_ACTIVE_PLAYER);
  }

  return achievements;
}

/** Determines the player with the most matches played (ties: first found). */
export function findMostActivePlayerId(
  matches: MatchRecord[],
  playerIds: string[]
): string | null {
  let bestId: string | null = null;
  let bestCount = -1;
  for (const id of playerIds) {
    const count = matchesForPlayer(matches, id).length;
    if (count > bestCount) {
      bestCount = count;
      bestId = id;
    }
  }
  return bestCount > 0 ? bestId : null;
}
