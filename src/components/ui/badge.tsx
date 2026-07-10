import clsx from "clsx";

type Variant = "neutral" | "success" | "danger" | "warning";

const VARIANT_CLASSES: Record<Variant, string> = {
  neutral: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300",
  success: "bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300",
  danger: "bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300",
  warning: "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300",
};

export function Badge({
  children,
  variant = "neutral",
  className,
  title,
}: {
  children: React.ReactNode;
  variant?: Variant;
  className?: string;
  title?: string;
}) {
  return (
    <span
      title={title}
      className={clsx(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
        VARIANT_CLASSES[variant],
        className
      )}
    >
      {children}
    </span>
  );
}
