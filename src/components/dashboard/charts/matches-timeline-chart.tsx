"use client";

import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { SEQUENTIAL_BLUE_DARK, SEQUENTIAL_BLUE_LIGHT } from "@/lib/colors";

interface Bucket {
  key: string;
  label: string;
  count: number;
}

export function MatchesTimelineChart({ buckets, isDark }: { buckets: Bucket[]; isDark: boolean }) {
  if (buckets.length === 0) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">Keine Daten vorhanden.</p>;
  }

  const gridColor = isDark ? "#2c2c2a" : "#e1e0d9";
  const tickColor = isDark ? "#c3c2b7" : "#52514e";
  const color = isDark ? SEQUENTIAL_BLUE_DARK : SEQUENTIAL_BLUE_LIGHT;

  // Show at most the last 12 buckets to keep axis labels legible.
  const data = buckets.slice(-12);

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 24 }}>
        <CartesianGrid strokeDasharray="0" vertical={false} stroke={gridColor} />
        <XAxis
          dataKey="label"
          tick={{ fill: tickColor, fontSize: 11 }}
          axisLine={{ stroke: gridColor }}
          tickLine={false}
          interval={0}
          angle={-35}
          textAnchor="end"
          height={50}
        />
        <YAxis
          tick={{ fill: tickColor, fontSize: 12 }}
          axisLine={false}
          tickLine={false}
          allowDecimals={false}
        />
        <Tooltip
          formatter={(value: number) => [value, "Spiele"]}
          contentStyle={{
            background: isDark ? "#1a1a19" : "#fcfcfb",
            border: "1px solid " + gridColor,
            borderRadius: 8,
            color: isDark ? "#ffffff" : "#0b0b0b",
          }}
        />
        <Bar dataKey="count" fill={color} radius={[4, 4, 0, 0]} maxBarSize={24} />
      </BarChart>
    </ResponsiveContainer>
  );
}
