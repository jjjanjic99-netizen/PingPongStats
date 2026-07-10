"use client";

import { FormEvent, useState } from "react";
import clsx from "clsx";
import { Button } from "@/components/ui/button";
import { Input, Label, Select, FieldError } from "@/components/ui/field";
import type { PlayerView } from "@/components/players/players-client";

export interface MatchFormValues {
  playedAt: string; // datetime-local value
  playerAId: string;
  playerBId: string;
  playerASets: number;
  playerBSets: number;
  notes: string;
}

const QUICK_RESULTS: [number, number][] = [
  [1, 0],
  [2, 0],
  [2, 1],
  [3, 0],
  [3, 1],
  [3, 2],
];

function toDatetimeLocal(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(
    date.getHours()
  )}:${pad(date.getMinutes())}`;
}

export function MatchForm({
  players,
  initialValues,
  onSubmit,
  onCancel,
}: {
  players: PlayerView[];
  initialValues?: Partial<MatchFormValues>;
  onSubmit: (values: MatchFormValues) => Promise<void>;
  onCancel: () => void;
}) {
  const [playedAt, setPlayedAt] = useState(initialValues?.playedAt ?? toDatetimeLocal(new Date()));
  const [playerAId, setPlayerAId] = useState(initialValues?.playerAId ?? "");
  const [playerBId, setPlayerBId] = useState(initialValues?.playerBId ?? "");
  const [winnerSide, setWinnerSide] = useState<"A" | "B">("A");
  const [playerASets, setPlayerASets] = useState(initialValues?.playerASets ?? 0);
  const [playerBSets, setPlayerBSets] = useState(initialValues?.playerBSets ?? 0);
  const [notes, setNotes] = useState(initialValues?.notes ?? "");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function applyQuickResult(winnerSets: number, loserSets: number) {
    if (winnerSide === "A") {
      setPlayerASets(winnerSets);
      setPlayerBSets(loserSets);
    } else {
      setPlayerBSets(winnerSets);
      setPlayerASets(loserSets);
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!playerAId || !playerBId) {
      setError("Bitte beide Spieler auswählen.");
      return;
    }
    if (playerAId === playerBId) {
      setError("Spieler A und Spieler B dürfen nicht identisch sein.");
      return;
    }
    if (playerASets === playerBSets) {
      setError("Ein Spiel darf nicht unentschieden enden.");
      return;
    }

    setSubmitting(true);
    try {
      await onSubmit({ playedAt, playerAId, playerBId, playerASets, playerBSets, notes });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <Label htmlFor="playedAt">Datum &amp; Uhrzeit</Label>
          <Input
            id="playedAt"
            type="datetime-local"
            required
            value={playedAt}
            onChange={(e) => setPlayedAt(e.target.value)}
          />
        </div>
        <div />
        <div>
          <Label htmlFor="playerA">Spieler A</Label>
          <Select id="playerA" required value={playerAId} onChange={(e) => setPlayerAId(e.target.value)}>
            <option value="">Auswählen…</option>
            {players.map((p) => (
              <option key={p.id} value={p.id} disabled={p.id === playerBId}>
                {p.displayName}
              </option>
            ))}
          </Select>
        </div>
        <div>
          <Label htmlFor="playerB">Spieler B</Label>
          <Select id="playerB" required value={playerBId} onChange={(e) => setPlayerBId(e.target.value)}>
            <option value="">Auswählen…</option>
            {players.map((p) => (
              <option key={p.id} value={p.id} disabled={p.id === playerAId}>
                {p.displayName}
              </option>
            ))}
          </Select>
        </div>
      </div>

      <div>
        <Label>Gewinner (für Schnellauswahl)</Label>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setWinnerSide("A")}
            className={clsx(
              "rounded-lg border px-3 py-1.5 text-sm",
              winnerSide === "A"
                ? "border-brand-600 bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300"
                : "border-slate-300 dark:border-slate-600"
            )}
          >
            Spieler A
          </button>
          <button
            type="button"
            onClick={() => setWinnerSide("B")}
            className={clsx(
              "rounded-lg border px-3 py-1.5 text-sm",
              winnerSide === "B"
                ? "border-brand-600 bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300"
                : "border-slate-300 dark:border-slate-600"
            )}
          >
            Spieler B
          </button>
        </div>
      </div>

      <div>
        <Label>Schnellauswahl Ergebnis</Label>
        <div className="flex flex-wrap gap-2">
          {QUICK_RESULTS.map(([w, l]) => (
            <button
              key={`${w}-${l}`}
              type="button"
              onClick={() => applyQuickResult(w, l)}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium hover:bg-slate-100 dark:border-slate-600 dark:hover:bg-slate-800"
            >
              {winnerSide === "A" ? `${w}:${l}` : `${l}:${w}`}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:w-64">
        <div>
          <Label htmlFor="setsA">Sätze Spieler A</Label>
          <Input
            id="setsA"
            type="number"
            min={0}
            required
            value={playerASets}
            onChange={(e) => setPlayerASets(Number(e.target.value))}
          />
        </div>
        <div>
          <Label htmlFor="setsB">Sätze Spieler B</Label>
          <Input
            id="setsB"
            type="number"
            min={0}
            required
            value={playerBSets}
            onChange={(e) => setPlayerBSets(Number(e.target.value))}
          />
        </div>
      </div>

      <div>
        <Label htmlFor="notes">Kommentar / Notiz</Label>
        <Input id="notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>

      <FieldError>{error}</FieldError>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancel}>
          Abbrechen
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Speichert…" : "Speichern"}
        </Button>
      </div>
    </form>
  );
}
