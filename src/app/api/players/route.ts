import { NextRequest, NextResponse } from "next/server";
import { toErrorResponse } from "@/lib/api-utils";
import { createPlayer, listPlayers } from "@/lib/services/player-service";

export async function GET(request: NextRequest) {
  const includeInactive = request.nextUrl.searchParams.get("includeInactive") === "true";
  const players = await listPlayers(includeInactive);
  return NextResponse.json(players);
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const player = await createPlayer(body);
    return NextResponse.json(player, { status: 201 });
  } catch (error) {
    return toErrorResponse(error);
  }
}
