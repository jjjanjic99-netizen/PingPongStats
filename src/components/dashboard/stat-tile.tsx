import { Card } from "@/components/ui/card";

export function StatTile({
  label,
  value,
  sub,
}: {
  label: string;
  value: string | number;
  sub?: string;
}) {
  return (
    <Card className="p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold text-slate-900 dark:text-white">{value}</p>
      {sub && <p className="mt-0.5 truncate text-xs text-slate-500 dark:text-slate-400">{sub}</p>}
    </Card>
  );
}
