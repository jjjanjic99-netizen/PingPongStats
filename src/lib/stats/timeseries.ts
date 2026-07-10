import type { MatchRecord } from "./types";

export type TimeseriesGranularity = "week" | "month";

export interface TimeseriesBucket {
  /** Sortable bucket key, e.g. "2026-W12" or "2026-03". */
  key: string;
  /** Human-readable label for chart axes. */
  label: string;
  count: number;
}

const MONTH_LABELS = [
  "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez",
];

function isoWeekKey(date: Date): { key: string; label: string } {
  // ISO week calculation (Monday-based weeks, week 1 contains the first Thursday).
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
  const key = `${d.getUTCFullYear()}-W${String(weekNo).padStart(2, "0")}`;
  const label = `KW${String(weekNo).padStart(2, "0")}`;
  return { key, label };
}

function monthKey(date: Date): { key: string; label: string } {
  const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
  const label = `${MONTH_LABELS[date.getMonth()]} ${String(date.getFullYear()).slice(2)}`;
  return { key, label };
}

/**
 * Groups matches into chronological buckets (by ISO week or by month) and
 * counts matches per bucket. Buckets with zero matches are not included.
 */
export function matchesPerBucket(
  matches: MatchRecord[],
  granularity: TimeseriesGranularity
): TimeseriesBucket[] {
  const buckets = new Map<string, TimeseriesBucket>();

  for (const match of matches) {
    const { key, label } =
      granularity === "week" ? isoWeekKey(match.playedAt) : monthKey(match.playedAt);
    const existing = buckets.get(key);
    if (existing) {
      existing.count += 1;
    } else {
      buckets.set(key, { key, label, count: 1 });
    }
  }

  return Array.from(buckets.values()).sort((a, b) => a.key.localeCompare(b.key));
}
