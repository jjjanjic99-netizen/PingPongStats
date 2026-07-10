import { describe, expect, it } from "vitest";
import { computeWinnerId, MatchValidationError } from "./validation";

describe("computeWinnerId", () => {
  it("determines player A as winner when they won more sets", () => {
    const winner = computeWinnerId({
      playerAId: "a",
      playerBId: "b",
      playerASets: 3,
      playerBSets: 1,
    });
    expect(winner).toBe("a");
  });

  it("determines player B as winner when they won more sets", () => {
    const winner = computeWinnerId({
      playerAId: "a",
      playerBId: "b",
      playerASets: 0,
      playerBSets: 1,
    });
    expect(winner).toBe("b");
  });

  it("rejects identical players", () => {
    expect(() =>
      computeWinnerId({
        playerAId: "a",
        playerBId: "a",
        playerASets: 3,
        playerBSets: 1,
      })
    ).toThrow(MatchValidationError);
  });

  it("rejects a tied result", () => {
    expect(() =>
      computeWinnerId({
        playerAId: "a",
        playerBId: "b",
        playerASets: 2,
        playerBSets: 2,
      })
    ).toThrow(MatchValidationError);
  });

  it("rejects negative set counts", () => {
    expect(() =>
      computeWinnerId({
        playerAId: "a",
        playerBId: "b",
        playerASets: -1,
        playerBSets: 2,
      })
    ).toThrow(MatchValidationError);
  });

  it("rejects non-integer set counts", () => {
    expect(() =>
      computeWinnerId({
        playerAId: "a",
        playerBId: "b",
        playerASets: 2.5,
        playerBSets: 1,
      })
    ).toThrow(MatchValidationError);
  });
});
