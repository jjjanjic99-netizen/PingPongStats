import { describe, expect, it } from "vitest";
import {
  averageSetsWonPerMatch,
  currentStreak,
  headToHead,
  isLowSampleSize,
  longestWinStreak,
  overallWinRate,
  recentForm,
  winRateInLastNDays,
} from "./core";
import { computeWinnerId } from "./validation";
import type { MatchRecord } from "./types";

let counter = 0;
function match(
  playedAt: string,
  playerAId: string,
  playerBId: string,
  playerASets: number,
  playerBSets: number
): MatchRecord {
  counter += 1;
  const winnerId = computeWinnerId({ playerAId, playerBId, playerASets, playerBSets });
  return {
    id: `m${counter}`,
    playedAt: new Date(playedAt),
    playerAId,
    playerBId,
    playerASets,
    playerBSets,
    winnerId,
  };
}

describe("overallWinRate", () => {
  it("computes wins / played * 100", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 0), // p1 win
      match("2026-01-02", "p1", "p2", 3, 1), // p1 win
      match("2026-01-03", "p2", "p1", 3, 2), // p1 loss
      match("2026-01-04", "p1", "p3", 3, 0), // p1 win
    ];
    const result = overallWinRate(matches, "p1");
    expect(result.played).toBe(4);
    expect(result.wins).toBe(3);
    expect(result.losses).toBe(1);
    expect(result.winRatePct).toBeCloseTo(75);
  });

  it("returns 0% for a player with no matches", () => {
    const result = overallWinRate([], "p1");
    expect(result.played).toBe(0);
    expect(result.winRatePct).toBe(0);
  });
});

describe("winRateInLastNDays", () => {
  it("only counts matches within the last 30 days of the reference date", () => {
    const reference = new Date("2026-07-10T12:00:00Z");
    const matches = [
      match("2026-07-05", "p1", "p2", 3, 0), // within 30 days, win
      match("2026-06-15", "p1", "p2", 3, 0), // within 30 days, win
      match("2026-05-01", "p1", "p2", 0, 3), // older than 30 days, excluded
    ];
    const result = winRateInLastNDays(matches, "p1", 30, reference);
    expect(result.played).toBe(2);
    expect(result.wins).toBe(2);
    expect(result.winRatePct).toBe(100);
  });
});

describe("currentStreak", () => {
  it("counts consecutive wins backward from the most recent match", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 0, 3), // loss
      match("2026-01-02", "p1", "p2", 3, 0), // win
      match("2026-01-03", "p1", "p2", 3, 1), // win
      match("2026-01-04", "p1", "p2", 3, 2), // win (most recent)
    ];
    const streak = currentStreak(matches, "p1");
    expect(streak.type).toBe("WIN");
    expect(streak.length).toBe(3);
  });

  it("returns a loss streak when the most recent match was lost", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 0), // win
      match("2026-01-02", "p1", "p2", 0, 3), // loss (most recent)
    ];
    const streak = currentStreak(matches, "p1");
    expect(streak.type).toBe("LOSS");
    expect(streak.length).toBe(1);
  });

  it("returns NONE for a player without matches", () => {
    expect(currentStreak([], "p1")).toEqual({ type: "NONE", length: 0 });
  });
});

describe("longestWinStreak", () => {
  it("finds the longest historical run of consecutive wins", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 0), // W
      match("2026-01-02", "p1", "p2", 3, 0), // W
      match("2026-01-03", "p1", "p2", 0, 3), // L - breaks streak
      match("2026-01-04", "p1", "p2", 3, 0), // W
      match("2026-01-05", "p1", "p2", 3, 0), // W
      match("2026-01-06", "p1", "p2", 3, 0), // W (longest run: 3)
      match("2026-01-07", "p1", "p2", 0, 3), // L
    ];
    expect(longestWinStreak(matches, "p1")).toBe(3);
  });
});

describe("headToHead", () => {
  it("aggregates games and win rates between two players regardless of side", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 0), // p1 win
      match("2026-01-02", "p2", "p1", 3, 1), // p1 loss
      match("2026-01-03", "p1", "p2", 3, 2), // p1 win
      match("2026-01-04", "p1", "p3", 3, 0), // irrelevant, different opponent
    ];
    const h2h = headToHead(matches, "p1", "p2");
    expect(h2h.totalGames).toBe(3);
    expect(h2h.playerAWins).toBe(2);
    expect(h2h.playerBWins).toBe(1);
    expect(h2h.playerAWinRatePct).toBeCloseTo((2 / 3) * 100);
    expect(h2h.playerBWinRatePct).toBeCloseTo((1 / 3) * 100);
  });
});

describe("averageSetsWonPerMatch", () => {
  it("averages the sets won by the player across their matches", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 1), // p1 won 3 sets
      match("2026-01-02", "p2", "p1", 3, 2), // p1 won 2 sets
    ];
    expect(averageSetsWonPerMatch(matches, "p1")).toBeCloseTo(2.5);
  });
});

describe("recentForm", () => {
  it("returns the last N results, newest first", () => {
    const matches = [
      match("2026-01-01", "p1", "p2", 3, 0), // W
      match("2026-01-02", "p1", "p2", 0, 3), // L
      match("2026-01-03", "p1", "p2", 3, 0), // W
    ];
    const form = recentForm(matches, "p1", 2);
    expect(form.map((f) => f.result)).toEqual(["W", "L"]);
  });
});

describe("isLowSampleSize", () => {
  it("flags players with fewer than 3 games", () => {
    expect(isLowSampleSize(0)).toBe(true);
    expect(isLowSampleSize(2)).toBe(true);
    expect(isLowSampleSize(3)).toBe(false);
  });
});
