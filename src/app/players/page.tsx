import { listPlayers } from "@/lib/services/player-service";
import { PlayersClient } from "@/components/players/players-client";

export const dynamic = "force-dynamic";

export default async function PlayersPage() {
  const players = await listPlayers(true);
  const serializable = JSON.parse(JSON.stringify(players));
  return <PlayersClient initialPlayers={serializable} />;
}
