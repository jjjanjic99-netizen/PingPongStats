"use client";

import { useTheme } from "@/components/theme-provider";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { StatTile } from "./stat-tile";
import { PlayerBarChart } from "./charts/player-bar-chart";
import { MatchesTimelineChart } from "./charts/matches-timeline-chart";
import { StreakRankingChart } from "./charts/streak-ranking-chart";
import { RecentFormBadges } from "./recent-form-badges";
import { EmptyState } from "@/components/ui/empty-state";
import Link from "next/link";
import { Button } from "@/components/ui/button";

// Mirrors DashboardStats from src/lib/stats, but with dates as ISO strings
// after JSON serialization across the server/client boundary.
export interface DashboardStatsView {
  totalMatches: number;
  activePlayerCount: number;
  players: PlayerStatsSummaryView[];
  mostWins: PlayerStatsSummaryView | null;
  highestOverallWinRate: PlayerStatsSummaryView | null;
  highestWinRateLastMonth: PlayerStatsSummaryView | null;
  bestCurrentStreak: PlayerStatsSummaryView | null;
  matchesPerWeek: { key: string; label: string; count: number }[];
  matchesPerMonth: { key: string; label: string; count: number }[];
  averageSetsWonPerMatchOverall: number;
}

export interface PlayerStatsSummaryView {
  playerId: string;
  displayName: string;
  isActive: boolean;
  played: number;
  wins: number;
  losses: number;
  winRatePct: number;
  winRateLast30dPct: number;
  currentStreak: { type: "WIN" | "LOSS" | "NONE"; length: number };
  longestWinStreak: number;
  avgSetsWonPerMatch: number;
  eloRating: number;
  recentForm: { matchId: string; playedAt: string; result: "W" | "L"; opponentId: string }[];
  achievements: { id: string; label: string; description: string }[];
  lowSampleSize: boolean;
}

function pct(value: number): string {
  return `${value.toFixed(1)}%`;
}

export function DashboardClient({ stats }: { stats: DashboardStatsView }) {
  const { isDark } = useTheme();

  if (stats.totalMatches === 0) {
    return (
      <Card>
        <EmptyState
          title="Noch keine Spiele erfasst"
          description="Sobald Spiele erfasst wurden, erscheinen hier Statistiken und Auswertungen."
          action={
            <Link href="/matches/new">
              <Button>Erstes Spiel erfassen</Button>
            </Link>
          }
        />
      </Card>
    );
  }

  const rankedPlayers = [...stats.players]
    .filter((p) => p.played > 0)
    .sort((a, b) => b.eloRating - a.eloRating);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
        <StatTile label="Spiele total" value={stats.totalMatches} />
        <StatTile label="Aktive Spieler" value={stats.activePlayerCount} />
        <StatTile
          label="Meiste Siege"
          value={stats.mostWins ? stats.mostWins.wins : "–"}
          sub={stats.mostWins?.displayName}
        />
        <StatTile
          label="Höchste Siegquote (overall)"
          value={stats.highestOverallWinRate ? pct(stats.highestOverallWinRate.winRatePct) : "–"}
          sub={stats.highestOverallWinRate?.displayName}
        />
        <StatTile
          label="Höchste Siegquote (30 Tage)"
          value={
            stats.highestWinRateLastMonth
              ? pct(stats.highestWinRateLastMonth.winRateLast30dPct)
              : "–"
          }
          sub={stats.highestWinRateLastMonth?.displayName}
        />
        <StatTile
          label="Beste aktuelle Serie"
          value={stats.bestCurrentStreak ? `${stats.bestCurrentStreak.currentStreak.length}x` : "–"}
          sub={stats.bestCurrentStreak?.displayName}
        />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Siege pro Spieler</CardTitle>
          </CardHeader>
          <CardBody>
            <PlayerBarChart
              players={rankedPlayers}
              valueKey="wins"
              valueLabel="Siege"
              isDark={isDark}
            />
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Siegquote pro Spieler</CardTitle>
          </CardHeader>
          <CardBody>
            <PlayerBarChart
              players={rankedPlayers}
              valueKey="winRatePct"
              valueLabel="Siegquote (%)"
              isDark={isDark}
              formatValue={(v) => `${v.toFixed(0)}%`}
            />
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Spiele pro Woche</CardTitle>
          </CardHeader>
          <CardBody>
            <MatchesTimelineChart buckets={stats.matchesPerWeek} isDark={isDark} />
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Streak-Ranking (aktuelle Serie)</CardTitle>
          </CardHeader>
          <CardBody>
            <StreakRankingChart players={rankedPlayers} isDark={isDark} />
          </CardBody>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Ranking &amp; Spielerübersicht</CardTitle>
        </CardHeader>
        <CardBody className="overflow-x-auto p-0">
          <table className="w-full min-w-[900px] text-sm">
            <thead className="border-b border-slate-200 text-left text-xs uppercase text-slate-500 dark:border-slate-700 dark:text-slate-400">
              <tr>
                <th className="px-4 py-3">#</th>
                <th className="px-4 py-3">Spieler</th>
                <th className="px-4 py-3">Elo</th>
                <th className="px-4 py-3">Spiele</th>
                <th className="px-4 py-3">S/N</th>
                <th className="px-4 py-3">Siegquote</th>
                <th className="px-4 py-3">Siegquote 30T</th>
                <th className="px-4 py-3">Serie</th>
                <th className="px-4 py-3">Längste Serie</th>
                <th className="px-4 py-3">Ø Sätze</th>
                <th className="px-4 py-3">Form</th>
                <th className="px-4 py-3">Achievements</th>
              </tr>
            </thead>
            <tbody>
              {rankedPlayers.map((p, i) => (
                <tr
                  key={p.playerId}
                  className="border-b border-slate-100 last:border-0 dark:border-slate-800"
                >
                  <td className="px-4 py-3 text-slate-500">{i + 1}</td>
                  <td className="px-4 py-3 font-medium">
                    {p.displayName}
                    {!p.isActive && (
                      <Badge variant="neutral" className="ml-2">
                        archiviert
                      </Badge>
                    )}
                    {p.lowSampleSize && (
                      <Badge variant="warning" className="ml-2">
                        &lt; 3 Spiele
                      </Badge>
                    )}
                  </td>
                  <td className="px-4 py-3 tabular-nums">{Math.round(p.eloRating)}</td>
                  <td className="px-4 py-3 tabular-nums">{p.played}</td>
                  <td className="px-4 py-3 tabular-nums">
                    {p.wins} / {p.losses}
                  </td>
                  <td className="px-4 py-3 tabular-nums">{pct(p.winRatePct)}</td>
                  <td className="px-4 py-3 tabular-nums">{pct(p.winRateLast30dPct)}</td>
                  <td className="px-4 py-3">
                    {p.currentStreak.type === "NONE" ? (
                      "–"
                    ) : (
                      <Badge variant={p.currentStreak.type === "WIN" ? "success" : "danger"}>
                        {p.currentStreak.type === "WIN" ? "W" : "L"} x{p.currentStreak.length}
                      </Badge>
                    )}
                  </td>
                  <td className="px-4 py-3 tabular-nums">{p.longestWinStreak}</td>
                  <td className="px-4 py-3 tabular-nums">{p.avgSetsWonPerMatch.toFixed(1)}</td>
                  <td className="px-4 py-3">
                    <RecentFormBadges form={p.recentForm} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {p.achievements.length === 0
                        ? "–"
                        : p.achievements.map((a) => (
                            <Badge key={a.id} variant="neutral" title={a.description}>
                              {a.label}
                            </Badge>
                          ))}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </CardBody>
      </Card>
    </div>
  );
}
