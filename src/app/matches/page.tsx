import { listMatches } from "@/lib/services/match-service";
import { listPlayers } from "@/lib/services/player-service";
import { MatchesClient } from "@/components/matches/matches-client";

export const dynamic = "force-dynamic";

export default async function MatchesPage() {
  const [matches, players] = await Promise.all([listMatches(), listPlayers(true)]);
  return (
    <MatchesClient
      initialMatches={JSON.parse(JSON.stringify(matches))}
      players={JSON.parse(JSON.stringify(players))}
    />
  );
}
