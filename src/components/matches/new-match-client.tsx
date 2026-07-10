"use client";

import { useRouter } from "next/navigation";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { useToast } from "@/components/toast";
import { MatchForm, type MatchFormValues } from "./match-form";
import type { PlayerView } from "@/components/players/players-client";
import { EmptyState } from "@/components/ui/empty-state";
import Link from "next/link";
import { Button } from "@/components/ui/button";

export function NewMatchClient({ players }: { players: PlayerView[] }) {
  const router = useRouter();
  const { showSuccess, showError } = useToast();

  async function handleSubmit(values: MatchFormValues) {
    const res = await fetch("/api/matches", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...values,
        playedAt: new Date(values.playedAt).toISOString(),
      }),
    });
    if (!res.ok) {
      const body = await res.json();
      showError(body.error ?? "Spiel konnte nicht gespeichert werden.");
      return;
    }
    showSuccess("Spiel wurde erfasst.");
    router.push("/matches");
    router.refresh();
  }

  if (players.length < 2) {
    return (
      <Card>
        <EmptyState
          title="Mindestens 2 aktive Spieler benötigt"
          description="Lege zuerst weitere aktive Spieler an, um ein Spiel erfassen zu können."
          action={
            <Link href="/players">
              <Button>Zu den Spielern</Button>
            </Link>
          }
        />
      </Card>
    );
  }

  return (
    <Card className="max-w-2xl">
      <CardHeader>
        <CardTitle>Neues Spiel erfassen</CardTitle>
      </CardHeader>
      <CardBody>
        <MatchForm players={players} onSubmit={handleSubmit} onCancel={() => router.push("/matches")} />
      </CardBody>
    </Card>
  );
}
