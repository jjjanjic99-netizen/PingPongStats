"use client";

import { useRouter } from "next/navigation";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { useToast } from "@/components/toast";
import { MatchForm, type MatchFormValues } from "./match-form";
import type { MatchView } from "./matches-client";
import type { PlayerView } from "@/components/players/players-client";

function toDatetimeLocal(iso: string): string {
  const date = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(
    date.getHours()
  )}:${pad(date.getMinutes())}`;
}

export function EditMatchClient({ match, players }: { match: MatchView; players: PlayerView[] }) {
  const router = useRouter();
  const { showSuccess, showError } = useToast();

  async function handleSubmit(values: MatchFormValues) {
    const res = await fetch(`/api/matches/${match.id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...values,
        playedAt: new Date(values.playedAt).toISOString(),
      }),
    });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Spiel konnte nicht aktualisiert werden.");
      return;
    }
    showSuccess("Spiel wurde aktualisiert.");
    router.push("/matches");
    router.refresh();
  }

  return (
    <Card className="max-w-2xl">
      <CardHeader>
        <CardTitle>Spiel bearbeiten</CardTitle>
      </CardHeader>
      <CardBody>
        <MatchForm
          players={players}
          initialValues={{
            playedAt: toDatetimeLocal(match.playedAt),
            playerAId: match.playerA.id,
            playerBId: match.playerB.id,
            playerASets: match.playerASets,
            playerBSets: match.playerBSets,
            notes: match.notes ?? "",
          }}
          onSubmit={handleSubmit}
          onCancel={() => router.push("/matches")}
        />
      </CardBody>
    </Card>
  );
}
