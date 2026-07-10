"use client";

import { useState } from "react";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/empty-state";
import { useToast } from "@/components/toast";
import { PlayerForm, type PlayerFormValues } from "./player-form";

export interface PlayerView {
  id: string;
  displayName: string;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  isActive: boolean;
  createdAt: string;
}

export function PlayersClient({ initialPlayers }: { initialPlayers: PlayerView[] }) {
  const [players, setPlayers] = useState<PlayerView[]>(initialPlayers);
  const [showInactive, setShowInactive] = useState(true);
  const [formOpen, setFormOpen] = useState(false);
  const [editingPlayer, setEditingPlayer] = useState<PlayerView | null>(null);
  const { showSuccess, showError } = useToast();

  const visiblePlayers = showInactive ? players : players.filter((p) => p.isActive);

  async function refresh() {
    const res = await fetch("/api/players?includeInactive=true");
    setPlayers(await res.json());
  }

  async function handleCreate(values: PlayerFormValues) {
    const res = await fetch("/api/players", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Spieler konnte nicht erstellt werden.");
      return;
    }
    await refresh();
    setFormOpen(false);
    showSuccess("Spieler wurde angelegt.");
  }

  async function handleUpdate(id: string, values: PlayerFormValues) {
    const res = await fetch(`/api/players/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(values),
    });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Spieler konnte nicht aktualisiert werden.");
      return;
    }
    await refresh();
    setEditingPlayer(null);
    showSuccess("Spieler wurde aktualisiert.");
  }

  async function toggleActive(player: PlayerView) {
    const res = await fetch(`/api/players/${player.id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isActive: !player.isActive }),
    });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Aktion fehlgeschlagen.");
      return;
    }
    await refresh();
    showSuccess(
      player.isActive ? "Spieler wurde archiviert." : "Spieler wurde reaktiviert."
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">Spieler</h1>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
            <input
              type="checkbox"
              checked={showInactive}
              onChange={(e) => setShowInactive(e.target.checked)}
            />
            Archivierte anzeigen
          </label>
          <Button
            onClick={() => {
              setEditingPlayer(null);
              setFormOpen(true);
            }}
          >
            + Neuer Spieler
          </Button>
        </div>
      </div>

      {(formOpen || editingPlayer) && (
        <Card>
          <CardHeader>
            <CardTitle>{editingPlayer ? "Spieler bearbeiten" : "Neuer Spieler"}</CardTitle>
          </CardHeader>
          <CardBody>
            <PlayerForm
              initialValues={editingPlayer ?? undefined}
              onCancel={() => {
                setFormOpen(false);
                setEditingPlayer(null);
              }}
              onSubmit={(values) =>
                editingPlayer ? handleUpdate(editingPlayer.id, values) : handleCreate(values)
              }
            />
          </CardBody>
        </Card>
      )}

      <Card>
        <CardBody className="overflow-x-auto p-0">
          {visiblePlayers.length === 0 ? (
            <EmptyState
              title="Keine Spieler vorhanden"
              description="Lege den ersten Spieler an, um Spiele erfassen zu können."
            />
          ) : (
            <table className="w-full min-w-[700px] text-sm">
              <thead className="border-b border-slate-200 text-left text-xs uppercase text-slate-500 dark:border-slate-700 dark:text-slate-400">
                <tr>
                  <th className="px-4 py-3">Anzeigename</th>
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">E-Mail</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Erstellt</th>
                  <th className="px-4 py-3 text-right">Aktionen</th>
                </tr>
              </thead>
              <tbody>
                {visiblePlayers.map((p) => (
                  <tr
                    key={p.id}
                    className="border-b border-slate-100 last:border-0 dark:border-slate-800"
                  >
                    <td className="px-4 py-3 font-medium">{p.displayName}</td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                      {[p.firstName, p.lastName].filter(Boolean).join(" ") || "–"}
                    </td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                      {p.email || "–"}
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={p.isActive ? "success" : "neutral"}>
                        {p.isActive ? "Aktiv" : "Archiviert"}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                      {new Date(p.createdAt).toLocaleDateString("de-DE")}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          variant="secondary"
                          onClick={() => {
                            setFormOpen(false);
                            setEditingPlayer(p);
                          }}
                        >
                          Bearbeiten
                        </Button>
                        <Button
                          variant={p.isActive ? "danger" : "secondary"}
                          onClick={() => toggleActive(p)}
                        >
                          {p.isActive ? "Deaktivieren" : "Aktivieren"}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
