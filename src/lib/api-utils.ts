import { NextResponse } from "next/server";
import { MatchValidationError } from "@/lib/stats";
import { PlayerNotFoundError, PlayerValidationError } from "@/lib/services/player-service";
import { MatchNotFoundError } from "@/lib/services/match-service";

/** Maps known domain errors to appropriate HTTP responses; rethrows unknown errors. */
export function toErrorResponse(error: unknown): NextResponse {
  if (
    error instanceof MatchValidationError ||
    error instanceof PlayerValidationError
  ) {
    return NextResponse.json({ error: error.message }, { status: 400 });
  }
  if (error instanceof PlayerNotFoundError || error instanceof MatchNotFoundError) {
    return NextResponse.json({ error: error.message }, { status: 404 });
  }

  console.error(error);
  return NextResponse.json(
    { error: "Ein unerwarteter Fehler ist aufgetreten." },
    { status: 500 }
  );
}
