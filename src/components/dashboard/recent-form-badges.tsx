import clsx from "clsx";

export function RecentFormBadges({
  form,
}: {
  form: { matchId: string; result: "W" | "L" }[];
}) {
  if (form.length === 0) return <span className="text-slate-400">–</span>;
  return (
    <div className="flex gap-1">
      {form.map((entry) => (
        <span
          key={entry.matchId}
          className={clsx(
            "flex h-5 w-5 items-center justify-center rounded text-[10px] font-bold text-white",
            entry.result === "W" ? "bg-green-600" : "bg-red-500"
          )}
          title={entry.result === "W" ? "Sieg" : "Niederlage"}
        >
          {entry.result}
        </span>
      ))}
    </div>
  );
}
