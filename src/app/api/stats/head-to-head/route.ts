import { NextRequest, NextResponse } from "next/server";
import { getHeadToHeadStats } from "@/lib/services/stats-service";

export async function GET(request: NextRequest) {
  const playerAId = request.nextUrl.searchParams.get("playerAId");
  const playerBId = request.nextUrl.searchParams.get("playerBId");

  if (!playerAId || !playerBId) {
    return NextResponse.json(
      { error: "playerAId und playerBId sind erforderlich." },
      { status: 400 }
    );
  }
  if (playerAId === playerBId) {
    return NextResponse.json(
      { error: "Bitte zwei unterschiedliche Spieler auswählen." },
      { status: 400 }
    );
  }

  const result = await getHeadToHeadStats(playerAId, playerBId);
  if (!result) {
    return NextResponse.json({ error: "Spieler nicht gefunden." }, { status: 404 });
  }
  return NextResponse.json(result);
}
