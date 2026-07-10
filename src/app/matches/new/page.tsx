import { listPlayers } from "@/lib/services/player-service";
import { NewMatchClient } from "@/components/matches/new-match-client";

export const dynamic = "force-dynamic";

export default async function NewMatchPage() {
  const players = await listPlayers(false);
  return <NewMatchClient players={JSON.parse(JSON.stringify(players))} />;
}
