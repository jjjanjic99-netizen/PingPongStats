import clsx from "clsx";

export function Card({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div
      className={clsx(
        "rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900",
        className
      )}
    >
      {children}
    </div>
  );
}

export function CardHeader({ children }: { children: React.ReactNode }) {
  return (
    <div className="border-b border-slate-200 px-5 py-4 dark:border-slate-700">
      {children}
    </div>
  );
}

export function CardTitle({ children }: { children: React.ReactNode }) {
  return (
    <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {children}
    </h2>
  );
}

export function CardBody({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return <div className={clsx("p-5", className)}>{children}</div>;
}
