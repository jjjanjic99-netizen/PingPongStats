"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import clsx from "clsx";
import { useTheme } from "./theme-provider";

const LINKS = [
  { href: "/", label: "Dashboard" },
  { href: "/players", label: "Spieler" },
  { href: "/matches", label: "Spiele" },
  { href: "/matches/new", label: "Neues Spiel" },
  { href: "/head-to-head", label: "Head-to-Head" },
  { href: "/settings", label: "Einstellungen" },
];

export function Nav() {
  const pathname = usePathname();
  const { isDark, toggle } = useTheme();

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 backdrop-blur dark:border-slate-700 dark:bg-slate-900/90">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3">
        <div className="flex items-center gap-2 text-lg font-bold text-slate-900 dark:text-white">
          🏓 <span>PingPongStats</span>
        </div>
        <nav className="flex flex-wrap items-center gap-1">
          {(() => {
            // Only the longest matching href is active, so "/matches" and
            // "/matches/new" never highlight simultaneously.
            const bestMatch = LINKS.filter(
              (link) => pathname === link.href || pathname.startsWith(link.href + "/")
            ).sort((a, b) => b.href.length - a.href.length)[0];

            return LINKS.map((link) => {
              const active = link === bestMatch;
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={clsx(
                    "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                    active
                      ? "bg-brand-600 text-white"
                      : "text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
                  )}
                >
                  {link.label}
                </Link>
              );
            });
          })()}
          <button
            onClick={toggle}
            aria-label="Farbschema umschalten"
            className="ml-1 rounded-md px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            {isDark ? "☀️" : "🌙"}
          </button>
        </nav>
      </div>
    </header>
  );
}
