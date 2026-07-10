import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { matchesToCsv } from "@/lib/csv";

export async function GET() {
  const matches = await prisma.match.findMany({
    include: { playerA: true, playerB: true },
    orderBy: { playedAt: "asc" },
  });

  const csv = matchesToCsv(
    matches.map((m) => ({
      playedAt: m.playedAt,
      playerADisplayName: m.playerA.displayName,
      playerBDisplayName: m.playerB.displayName,
      playerASets: m.playerASets,
      playerBSets: m.playerBSets,
      notes: m.notes,
    }))
  );

  return new NextResponse(csv, {
    headers: {
      "Content-Type": "text/csv; charset=utf-8",
      "Content-Disposition": `attachment; filename="matches-export.csv"`,
    },
  });
}
