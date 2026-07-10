import { describe, expect, it } from "vitest";
import { computeEloRatings, DEFAULT_ELO_RATING } from "./elo";
import { computeWinnerId } from "./validation";
import type { MatchRecord } from "./types";

function match(id: string, playedAt: string, playerAId: string, playerBId: string, a: number, b: number): MatchRecord {
  return {
    id,
    playedAt: new Date(playedAt),
    playerAId,
    playerBId,
    playerASets: a,
    playerBSets: b,
    winnerId: computeWinnerId({ playerAId, playerBId, playerASets: a, playerBSets: b }),
  };
}

describe("computeEloRatings", () => {
  it("keeps default rating for players with no matches", () => {
    const ratings = computeEloRatings([], ["p1", "p2"]);
    expect(ratings.get("p1")).toBe(DEFAULT_ELO_RATING);
    expect(ratings.get("p2")).toBe(DEFAULT_ELO_RATING);
  });

  it("increases the winner's rating and decreases the loser's after an even match", () => {
    const matches = [match("m1", "2026-01-01", "p1", "p2", 3, 0)];
    const ratings = computeEloRatings(matches, ["p1", "p2"]);
    expect(ratings.get("p1")!).toBeGreaterThan(DEFAULT_ELO_RATING);
    expect(ratings.get("p2")!).toBeLessThan(DEFAULT_ELO_RATING);
    // Zero-sum: total rating points stay constant.
    expect(ratings.get("p1")! + ratings.get("p2")!).toBeCloseTo(
      DEFAULT_ELO_RATING * 2
    );
  });

  it("processes matches in chronological order regardless of input order", () => {
    const chronological = [
      match("m1", "2026-01-01", "p1", "p2", 3, 0),
      match("m2", "2026-01-02", "p1", "p2", 3, 0),
    ];
    const reversed = [chronological[1], chronological[0]];

    const r1 = computeEloRatings(chronological, ["p1", "p2"]);
    const r2 = computeEloRatings(reversed, ["p1", "p2"]);
    expect(r1.get("p1")).toBeCloseTo(r2.get("p1")!);
    expect(r1.get("p2")).toBeCloseTo(r2.get("p2")!);
  });
});
