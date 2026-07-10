import { notFound } from "next/navigation";
import { getMatch, MatchNotFoundError } from "@/lib/services/match-service";
import { listPlayers } from "@/lib/services/player-service";
import { EditMatchClient } from "@/components/matches/edit-match-client";

export const dynamic = "force-dynamic";

export default async function EditMatchPage({ params }: { params: { id: string } }) {
  try {
    const [match, players] = await Promise.all([getMatch(params.id), listPlayers(true)]);
    return (
      <EditMatchClient
        match={JSON.parse(JSON.stringify(match))}
        players={JSON.parse(JSON.stringify(players))}
      />
    );
  } catch (error) {
    if (error instanceof MatchNotFoundError) notFound();
    throw error;
  }
}
