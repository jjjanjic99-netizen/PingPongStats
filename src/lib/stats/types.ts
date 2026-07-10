export interface MatchRecord {
  id: string;
  playedAt: Date;
  playerAId: string;
  playerBId: string;
  playerASets: number;
  playerBSets: number;
  winnerId: string;
}

export interface PlayerRecord {
  id: string;
  displayName: string;
  isActive: boolean;
}

export type StreakType = "WIN" | "LOSS" | "NONE";

export interface StreakInfo {
  type: StreakType;
  length: number;
}

export interface HeadToHeadStats {
  playerAId: string;
  playerBId: string;
  totalGames: number;
  playerAWins: number;
  playerBWins: number;
  playerAWinRatePct: number;
  playerBWinRatePct: number;
}

export interface RecentFormEntry {
  matchId: string;
  playedAt: Date;
  result: "W" | "L";
  opponentId: string;
}
