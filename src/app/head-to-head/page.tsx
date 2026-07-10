import { listPlayers } from "@/lib/services/player-service";
import { HeadToHeadClient } from "@/components/head-to-head/head-to-head-client";

export const dynamic = "force-dynamic";

export default async function HeadToHeadPage() {
  const players = await listPlayers(true);
  return <HeadToHeadClient players={JSON.parse(JSON.stringify(players))} />;
}
