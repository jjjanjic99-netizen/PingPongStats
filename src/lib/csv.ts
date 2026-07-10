export interface MatchCsvRow {
  playedAt: string;
  playerADisplayName: string;
  playerBDisplayName: string;
  playerASets: string;
  playerBSets: string;
  notes: string;
}

const CSV_HEADERS = [
  "playedAt",
  "playerADisplayName",
  "playerBDisplayName",
  "playerASets",
  "playerBSets",
  "notes",
] as const;

function escapeCsvField(value: string): string {
  if (value.includes(",") || value.includes('"') || value.includes("\n")) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}

export interface MatchExportRecord {
  playedAt: Date;
  playerADisplayName: string;
  playerBDisplayName: string;
  playerASets: number;
  playerBSets: number;
  notes: string | null;
}

export function matchesToCsv(matches: MatchExportRecord[]): string {
  const lines = [CSV_HEADERS.join(",")];
  for (const m of matches) {
    lines.push(
      [
        m.playedAt.toISOString(),
        escapeCsvField(m.playerADisplayName),
        escapeCsvField(m.playerBDisplayName),
        String(m.playerASets),
        String(m.playerBSets),
        escapeCsvField(m.notes ?? ""),
      ].join(",")
    );
  }
  return lines.join("\n");
}

/** Minimal RFC4180-ish CSV line splitter supporting quoted fields. */
function splitCsvLine(line: string): string[] {
  const fields: string[] = [];
  let current = "";
  let inQuotes = false;

  for (let i = 0; i < line.length; i++) {
    const char = line[i];
    if (inQuotes) {
      if (char === '"') {
        if (line[i + 1] === '"') {
          current += '"';
          i++;
        } else {
          inQuotes = false;
        }
      } else {
        current += char;
      }
    } else if (char === '"') {
      inQuotes = true;
    } else if (char === ",") {
      fields.push(current);
      current = "";
    } else {
      current += char;
    }
  }
  fields.push(current);
  return fields;
}

export function parseMatchesCsv(csvText: string): MatchCsvRow[] {
  const lines = csvText.split(/\r?\n/).filter((line) => line.trim().length > 0);
  if (lines.length === 0) return [];

  const header = splitCsvLine(lines[0]).map((h) => h.trim());
  const rows: MatchCsvRow[] = [];

  for (let i = 1; i < lines.length; i++) {
    const values = splitCsvLine(lines[i]);
    const row: Record<string, string> = {};
    header.forEach((key, idx) => {
      row[key] = values[idx] ?? "";
    });
    rows.push({
      playedAt: row.playedAt ?? "",
      playerADisplayName: row.playerADisplayName ?? "",
      playerBDisplayName: row.playerBDisplayName ?? "",
      playerASets: row.playerASets ?? "",
      playerBSets: row.playerBSets ?? "",
      notes: row.notes ?? "",
    });
  }
  return rows;
}
