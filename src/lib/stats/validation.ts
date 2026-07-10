export class MatchValidationError extends Error {}

export interface MatchInput {
  playerAId: string;
  playerBId: string;
  playerASets: number;
  playerBSets: number;
}

/**
 * Validates a match's raw set score and derives the winner.
 * Throws MatchValidationError with a human-readable message on invalid input.
 */
export function computeWinnerId(input: MatchInput): string {
  const { playerAId, playerBId, playerASets, playerBSets } = input;

  if (!playerAId || !playerBId) {
    throw new MatchValidationError("Beide Spieler müssen ausgewählt sein.");
  }
  if (playerAId === playerBId) {
    throw new MatchValidationError(
      "Spieler A und Spieler B dürfen nicht identisch sein."
    );
  }
  if (
    !Number.isInteger(playerASets) ||
    !Number.isInteger(playerBSets) ||
    playerASets < 0 ||
    playerBSets < 0
  ) {
    throw new MatchValidationError(
      "Satzwerte müssen nicht-negative ganze Zahlen sein."
    );
  }
  if (playerASets === playerBSets) {
    throw new MatchValidationError(
      "Ein Spiel darf nicht unentschieden enden - ein Spieler muss mehr Sätze gewinnen."
    );
  }

  return playerASets > playerBSets ? playerAId : playerBId;
}
