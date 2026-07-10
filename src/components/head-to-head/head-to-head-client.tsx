"use client";

import { useEffect, useState } from "react";
import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, Label } from "@/components/ui/field";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/empty-state";
import { useTheme } from "@/components/theme-provider";
import { categoricalColor } from "@/lib/colors";
import type { PlayerView } from "@/components/players/players-client";

interface HeadToHeadResult {
  playerA: PlayerView;
  playerB: PlayerView;
  stats: {
    totalGames: number;
    playerAWins: number;
    playerBWins: number;
    playerAWinRatePct: number;
    playerBWinRatePct: number;
  };
  matchHistory: {
    id: string;
    playedAt: string;
    playerAId: string;
    playerBId: string;
    playerASets: number;
    playerBSets: number;
    winnerId: string;
  }[];
}

export function HeadToHeadClient({ players }: { players: PlayerView[] }) {
  const { isDark } = useTheme();
  const [playerAId, setPlayerAId] = useState(players[0]?.id ?? "");
  const [playerBId, setPlayerBId] = useState(players[1]?.id ?? "");
  const [result, setResult] = useState<HeadToHeadResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!playerAId || !playerBId || playerAId === playerBId) {
      setResult(null);
      return;
    }
    let cancelled = false;
    fetch(`/api/stats/head-to-head?playerAId=${playerAId}&playerBId=${playerBId}`)
      .then(async (res) => {
        const body = await res.json();
        if (cancelled) return;
        if (!res.ok) {
          setError(body.error);
          setResult(null);
        } else {
          setError(null);
          setResult(body);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [playerAId, playerBId]);

  const chartData = result
    ? [
        { name: result.playerA.displayName, value: result.stats.playerAWins },
        { name: result.playerB.displayName, value: result.stats.playerBWins },
      ]
    : [];

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold">Head-to-Head</h1>

      <Card>
        <CardBody className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Label htmlFor="h2h-a">Spieler A</Label>
            <Select id="h2h-a" value={playerAId} onChange={(e) => setPlayerAId(e.target.value)}>
              {players.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label htmlFor="h2h-b">Spieler B</Label>
            <Select id="h2h-b" value={playerBId} onChange={(e) => setPlayerBId(e.target.value)}>
              {players.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.displayName}
                </option>
              ))}
            </Select>
          </div>
        </CardBody>
      </Card>

      {error && (
        <Card>
          <CardBody>
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </CardBody>
        </Card>
      )}

      {!error && playerAId === playerBId && (
        <Card>
          <EmptyState title="Bitte zwei unterschiedliche Spieler auswählen" />
        </Card>
      )}

      {result && result.stats.totalGames === 0 && (
        <Card>
          <EmptyState
            title="Noch keine Spiele zwischen diesen beiden Spielern"
            description="Sobald ein Spiel zwischen den beiden erfasst wurde, erscheint hier die Statistik."
          />
        </Card>
      )}

      {result && result.stats.totalGames > 0 && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Card className="p-4">
              <p className="text-xs uppercase text-slate-500 dark:text-slate-400">Spiele gesamt</p>
              <p className="mt-1 text-2xl font-semibold">{result.stats.totalGames}</p>
            </Card>
            <Card className="p-4">
              <p className="text-xs uppercase text-slate-500 dark:text-slate-400">
                {result.playerA.displayName}
              </p>
              <p className="mt-1 text-2xl font-semibold">
                {result.stats.playerAWins} Siege ({result.stats.playerAWinRatePct.toFixed(0)}%)
              </p>
            </Card>
            <Card className="p-4">
              <p className="text-xs uppercase text-slate-500 dark:text-slate-400">
                {result.playerB.displayName}
              </p>
              <p className="mt-1 text-2xl font-semibold">
                {result.stats.playerBWins} Siege ({result.stats.playerBWinRatePct.toFixed(0)}%)
              </p>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Siege im direkten Vergleich</CardTitle>
            </CardHeader>
            <CardBody>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 8 }}>
                  <XAxis
                    dataKey="name"
                    tick={{ fill: isDark ? "#c3c2b7" : "#52514e", fontSize: 12 }}
                    axisLine={{ stroke: isDark ? "#2c2c2a" : "#e1e0d9" }}
                    tickLine={false}
                  />
                  <YAxis
                    tick={{ fill: isDark ? "#c3c2b7" : "#52514e", fontSize: 12 }}
                    axisLine={false}
                    tickLine={false}
                    allowDecimals={false}
                  />
                  <Tooltip
                    contentStyle={{
                      background: isDark ? "#1a1a19" : "#fcfcfb",
                      border: "1px solid " + (isDark ? "#2c2c2a" : "#e1e0d9"),
                      borderRadius: 8,
                      color: isDark ? "#ffffff" : "#0b0b0b",
                    }}
                  />
                  <Bar dataKey="value" radius={[4, 4, 0, 0]} maxBarSize={64}>
                    {chartData.map((_, index) => (
                      <Cell key={index} fill={categoricalColor(index, isDark)} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Spielverlauf</CardTitle>
            </CardHeader>
            <CardBody className="overflow-x-auto p-0">
              <table className="w-full min-w-[500px] text-sm">
                <thead className="border-b border-slate-200 text-left text-xs uppercase text-slate-500 dark:border-slate-700 dark:text-slate-400">
                  <tr>
                    <th className="px-4 py-3">Datum</th>
                    <th className="px-4 py-3">Ergebnis</th>
                    <th className="px-4 py-3">Gewinner</th>
                  </tr>
                </thead>
                <tbody>
                  {result.matchHistory.map((m) => {
                    const aSets = m.playerAId === result.playerA.id ? m.playerASets : m.playerBSets;
                    const bSets = m.playerAId === result.playerA.id ? m.playerBSets : m.playerASets;
                    const winnerName =
                      m.winnerId === result.playerA.id
                        ? result.playerA.displayName
                        : result.playerB.displayName;
                    return (
                      <tr key={m.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800">
                        <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                          {new Date(m.playedAt).toLocaleDateString("de-DE")}
                        </td>
                        <td className="px-4 py-3 tabular-nums font-medium">
                          {aSets}:{bSets}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant="success">{winnerName}</Badge>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </CardBody>
          </Card>
        </>
      )}
    </div>
  );
}
