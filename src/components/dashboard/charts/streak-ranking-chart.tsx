"use client";

import { Bar, BarChart, CartesianGrid, Cell, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { STATUS } from "@/lib/colors";
import type { PlayerStatsSummaryView } from "../dashboard-client";

export function StreakRankingChart({
  players,
  isDark,
}: {
  players: PlayerStatsSummaryView[];
  isDark: boolean;
}) {
  if (players.length === 0) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Keine Daten vorhanden.</p>;
  }

  const gridColor = isDark ? "#2c2c2a" : "#e1e0d9";
  const tickColor = isDark ? "#c3c2b7" : "#52514e";
  const goodColor = isDark ? STATUS.good.dark : STATUS.good.light;
  const criticalColor = isDark ? STATUS.critical.dark : STATUS.critical.light;

  const data = players
    .map((p) => ({
      name: p.displayName,
      value:
        p.currentStreak.type === "WIN"
          ? p.currentStreak.length
          : p.currentStreak.type === "LOSS"
            ? -p.currentStreak.length
            : 0,
    }))
    .sort((a, b) => b.value - a.value);

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 8 }}>
        <CartesianGrid strokeDasharray="0" vertical={false} stroke={gridColor} />
        <XAxis
          dataKey="name"
          tick={{ fill: tickColor, fontSize: 12 }}
          axisLine={{ stroke: gridColor }}
          tickLine={false}
          interval={0}
          angle={-20}
          textAnchor="end"
          height={60}
        />
        <YAxis tick={{ fill: tickColor, fontSize: 12 }} axisLine={false} tickLine={false} allowDecimals={false} />
        <ReferenceLine y={0} stroke={gridColor} />
        <Tooltip
          formatter={(value: number) => [
            value === 0 ? "keine Serie" : `${Math.abs(value)}x ${value > 0 ? "Siege" : "Niederlagen"}`,
            "Aktuelle Serie",
          ]}
          contentStyle={{
            background: isDark ? "#1a1a19" : "#fcfcfb",
            border: "1px solid " + gridColor,
            borderRadius: 8,
            color: isDark ? "#ffffff" : "#0b0b0b",
          }}
        />
        <Bar dataKey="value" radius={[4, 4, 4, 4]} maxBarSize={32}>
          {data.map((entry, index) => (
            <Cell key={index} fill={entry.value >= 0 ? goodColor : criticalColor} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
