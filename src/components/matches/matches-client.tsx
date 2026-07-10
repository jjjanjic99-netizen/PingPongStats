"use client";

import { useState } from "react";
import Link from "next/link";
import { Card, CardBody } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Select, Label, Input } from "@/components/ui/field";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/empty-state";
import { useToast } from "@/components/toast";
import type { PlayerView } from "@/components/players/players-client";

export interface MatchView {
  id: string;
  playedAt: string;
  playerASets: number;
  playerBSets: number;
  notes: string | null;
  playerA: PlayerView;
  playerB: PlayerView;
  winner: PlayerView;
}

export function MatchesClient({
  initialMatches,
  players,
}: {
  initialMatches: MatchView[];
  players: PlayerView[];
}) {
  const [matches, setMatches] = useState<MatchView[]>(initialMatches);
  const [playerId, setPlayerId] = useState("");
  const [winnerId, setWinnerId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [sort, setSort] = useState<"asc" | "desc">("desc");
  const [loading, setLoading] = useState(false);
  const { showSuccess, showError } = useToast();

  async function applyFilters() {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (playerId) params.set("playerId", playerId);
      if (winnerId) params.set("winnerId", winnerId);
      if (from) params.set("from", new Date(from).toISOString());
      if (to) params.set("to", new Date(to).toISOString());
      params.set("sort", sort);
      const res = await fetch(`/api/matches?${params.toString()}`);
      setMatches(await res.json());
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(match: MatchView) {
    if (
      !confirm(
        `Spiel ${match.playerA.displayName} vs. ${match.playerB.displayName} wirklich löschen?`
      )
    ) {
      return;
    }
    const res = await fetch(`/api/matches/${match.id}`, { method: "DELETE" });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Spiel konnte nicht gelöscht werden.");
      return;
    }
    setMatches((prev) => prev.filter((m) => m.id !== match.id));
    showSuccess("Spiel wurde gelöscht.");
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">Spiele</h1>
        <Link href="/matches/new">
          <Button>+ Neues Spiel</Button>
        </Link>
      </div>

      <Card>
        <CardBody className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
          <div>
            <Label htmlFor="filter-player">Spieler</Label>
            <Select id="filter-player" value={playerId} onChange={(e) => setPlayerId(e.target.value)}>
              <option value="">Alle</option>
              {players.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label htmlFor="filter-winner">Gewinner</Label>
            <Select id="filter-winner" value={winnerId} onChange={(e) => setWinnerId(e.target.value)}>
              <option value="">Alle</option>
              {players.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label htmlFor="filter-from">Von</Label>
            <Input id="filter-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="filter-to">Bis</Label>
            <Input id="filter-to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <div>
            <Label htmlFor="filter-sort">Sortierung</Label>
            <Select
              id="filter-sort"
              value={sort}
              onChange={(e) => setSort(e.target.value as "asc" | "desc")}
            >
              <option value="desc">Neueste zuerst</option>
              <option value="asc">Älteste zuerst</option>
            </Select>
          </div>
          <div className="flex items-end">
            <Button className="w-full" onClick={applyFilters} disabled={loading}>
              {loading ? "Lädt…" : "Filtern"}
            </Button>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardBody className="overflow-x-auto p-0">
          {matches.length === 0 ? (
            <EmptyState
              title="Keine Spiele gefunden"
              description="Passe die Filter an oder erfasse ein neues Spiel."
            />
          ) : (
            <table className="w-full min-w-[800px] text-sm">
              <thead className="border-b border-slate-200 text-left text-xs uppercase text-slate-500 dark:border-slate-700 dark:text-slate-400">
                <tr>
                  <th className="px-4 py-3">Datum</th>
                  <th className="px-4 py-3">Spieler A</th>
                  <th className="px-4 py-3">Spieler B</th>
                  <th className="px-4 py-3">Ergebnis</th>
                  <th className="px-4 py-3">Gewinner</th>
                  <th className="px-4 py-3">Notiz</th>
                  <th className="px-4 py-3 text-right">Aktionen</th>
                </tr>
              </thead>
              <tbody>
                {matches.map((m) => (
                  <tr
                    key={m.id}
                    className="border-b border-slate-100 last:border-0 dark:border-slate-800"
                  >
                    <td className="px-4 py-3 whitespace-nowrap text-slate-500 dark:text-slate-400">
                      {new Date(m.playedAt).toLocaleString("de-DE", {
                        dateStyle: "short",
                        timeStyle: "short",
                      })}
                    </td>
                    <td className="px-4 py-3">{m.playerA.displayName}</td>
                    <td className="px-4 py-3">{m.playerB.displayName}</td>
                    <td className="px-4 py-3 tabular-nums font-medium">
                      {m.playerASets}:{m.playerBSets}
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant="success">{m.winner.displayName}</Badge>
                    </td>
                    <td className="px-4 py-3 max-w-[200px] truncate text-slate-500 dark:text-slate-400">
                      {m.notes || "–"}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <Link href={`/matches/${m.id}/edit`}>
                          <Button variant="secondary">Bearbeiten</Button>
                        </Link>
                        <Button variant="danger" onClick={() => handleDelete(m)}>
                          Löschen
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
