import { NextRequest, NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { parseMatchesCsv } from "@/lib/csv";
import { createMatch } from "@/lib/services/match-service";
import { MatchValidationError } from "@/lib/stats";

interface ImportError {
  row: number;
  reason: string;
}

export async function POST(request: NextRequest) {
  const csvText = await request.text();
  if (!csvText.trim()) {
    return NextResponse.json({ error: "Keine CSV-Daten übermittelt." }, { status: 400 });
  }

  const rows = parseMatchesCsv(csvText);
  const players = await prisma.player.findMany();
  const playerIdByName = new Map(
    players.map((p) => [p.displayName.trim().toLowerCase(), p.id])
  );

  let importedCount = 0;
  const errors: ImportError[] = [];

  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const rowNumber = i + 2; // account for header row, 1-indexed for humans

    const playerAId = playerIdByName.get(row.playerADisplayName.trim().toLowerCase());
    const playerBId = playerIdByName.get(row.playerBDisplayName.trim().toLowerCase());

    if (!playerAId || !playerBId) {
      errors.push({
        row: rowNumber,
        reason: `Spieler nicht gefunden: ${
          !playerAId ? row.playerADisplayName : row.playerBDisplayName
        }`,
      });
      continue;
    }

    const playedAt = new Date(row.playedAt);
    if (Number.isNaN(playedAt.getTime())) {
      errors.push({ row: rowNumber, reason: `Ungültiges Datum: ${row.playedAt}` });
      continue;
    }

    try {
      await createMatch({
        playedAt,
        playerAId,
        playerBId,
        playerASets: Number(row.playerASets),
        playerBSets: Number(row.playerBSets),
        notes: row.notes || null,
      });
      importedCount += 1;
    } catch (error) {
      const reason =
        error instanceof MatchValidationError ? error.message : "Unbekannter Fehler";
      errors.push({ row: rowNumber, reason });
    }
  }

  return NextResponse.json({ importedCount, errors });
}
