import { NextRequest, NextResponse } from "next/server";
import { toErrorResponse } from "@/lib/api-utils";
import { createMatch, listMatches } from "@/lib/services/match-service";

export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;
  const playerId = params.get("playerId") ?? undefined;
  const winnerId = params.get("winnerId") ?? undefined;
  const from = params.get("from") ? new Date(params.get("from")!) : undefined;
  const to = params.get("to") ? new Date(params.get("to")!) : undefined;
  const sort = params.get("sort") === "asc" ? "asc" : "desc";

  const matches = await listMatches({ playerId, winnerId, from, to, sort });
  return NextResponse.json(matches);
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const match = await createMatch({
      playedAt: new Date(body.playedAt),
      playerAId: body.playerAId,
      playerBId: body.playerBId,
      playerASets: Number(body.playerASets),
      playerBSets: Number(body.playerBSets),
      notes: body.notes,
    });
    return NextResponse.json(match, { status: 201 });
  } catch (error) {
    return toErrorResponse(error);
  }
}
