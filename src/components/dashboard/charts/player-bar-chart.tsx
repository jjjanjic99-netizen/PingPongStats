"use client";

import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { categoricalColor } from "@/lib/colors";
import type { PlayerStatsSummaryView } from "../dashboard-client";

interface Props {
  players: PlayerStatsSummaryView[];
  valueKey: "wins" | "winRatePct";
  valueLabel: string;
  isDark: boolean;
  formatValue?: (value: number) => string;
}

export function PlayerBarChart({ players, valueKey, valueLabel, isDark, formatValue }: Props) {
  if (players.length === 0) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Keine Daten vorhanden.</p>;
  }

  const gridColor = isDark ? "#2c2c2a" : "#e1e0d9";
  const tickColor = isDark ? "#c3c2b7" : "#52514e";

  const data = players.map((p) => ({
    name: p.displayName,
    value: p[valueKey],
  }));

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
        <YAxis tick={{ fill: tickColor, fontSize: 12 }} axisLine={false} tickLine={false} />
        <Tooltip
          formatter={(value: number) => [formatValue ? formatValue(value) : value, valueLabel]}
          contentStyle={{
            background: isDark ? "#1a1a19" : "#fcfcfb",
            border: "1px solid " + gridColor,
            borderRadius: 8,
            color: isDark ? "#ffffff" : "#0b0b0b",
          }}
        />
        <Bar dataKey="value" radius={[4, 4, 0, 0]} maxBarSize={32}>
          {data.map((_, index) => (
            <Cell key={index} fill={categoricalColor(index, isDark)} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
