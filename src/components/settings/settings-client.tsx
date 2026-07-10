"use client";

import { useRef, useState } from "react";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/components/theme-provider";
import { useToast } from "@/components/toast";

export function SettingsClient() {
  const { isDark, toggle } = useTheme();
  const { showSuccess, showError } = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [importSummary, setImportSummary] = useState<{
    importedCount: number;
    errors: { row: number; reason: string }[];
  } | null>(null);
  const [importing, setImporting] = useState(false);

  async function handleImport(file: File) {
    setImporting(true);
    setImportSummary(null);
    try {
      const text = await file.text();
      const res = await fetch("/api/matches/import", {
        method: "POST",
        headers: { "Content-Type": "text/csv" },
        body: text,
      });
      const body = await res.json();
      if (!res.ok) {
        showError(body.error ?? "Import fehlgeschlagen.");
        return;
      }
      setImportSummary(body);
      showSuccess(`${body.importedCount} Spiel(e) importiert.`);
    } finally {
      setImporting(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold">Einstellungen</h1>

      <Card>
        <CardHeader>
          <CardTitle>Darstellung</CardTitle>
        </CardHeader>
        <CardBody className="flex items-center justify-between">
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Dunkles Farbschema {isDark ? "aktiviert" : "deaktiviert"}
          </p>
          <Button variant="secondary" onClick={toggle}>
            {isDark ? "Zu hellem Modus wechseln" : "Zu dunklem Modus wechseln"}
          </Button>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Daten exportieren</CardTitle>
        </CardHeader>
        <CardBody className="flex flex-col gap-3">
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Exportiert alle erfassten Spiele als CSV-Datei (Datum, Spieler, Ergebnis, Notiz).
          </p>
          <a href="/api/matches/export" download>
            <Button variant="secondary">CSV exportieren</Button>
          </a>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Daten importieren</CardTitle>
        </CardHeader>
        <CardBody className="flex flex-col gap-3">
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Importiert Spiele aus einer CSV-Datei mit den Spalten{" "}
            <code className="rounded bg-slate-100 px-1 py-0.5 text-xs dark:bg-slate-800">
              playedAt, playerADisplayName, playerBDisplayName, playerASets, playerBSets, notes
            </code>
            . Spielernamen müssen exakt den bestehenden Anzeigenamen entsprechen.
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,text/csv"
            disabled={importing}
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) handleImport(file);
            }}
            className="text-sm"
          />
          {importSummary && (
            <div className="rounded-lg bg-slate-50 p-3 text-sm dark:bg-slate-800">
              <p>{importSummary.importedCount} Spiel(e) erfolgreich importiert.</p>
              {importSummary.errors.length > 0 && (
                <ul className="mt-2 list-disc pl-5 text-red-600 dark:text-red-400">
                  {importSummary.errors.map((err, i) => (
                    <li key={i}>
                      Zeile {err.row}: {err.reason}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Roadmap</CardTitle>
        </CardHeader>
        <CardBody>
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Saison-/Turniermodus ist strukturell vorbereitbar (z. B. als optionales{" "}
            <code className="rounded bg-slate-100 px-1 py-0.5 text-xs dark:bg-slate-800">
              seasonId
            </code>{" "}
            auf dem Match-Modell), wurde in dieser Version aber bewusst nicht umgesetzt, um den
            Umfang beherrschbar zu halten.
          </p>
        </CardBody>
      </Card>
    </div>
  );
}
