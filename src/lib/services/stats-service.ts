import { prisma } from "@/lib/prisma";
import { buildDashboardStats, headToHead, type MatchRecord } from "@/lib/stats";

async function loadAllMatchRecords(): Promise<MatchRecord[]> {
  const matches = await prisma.match.findMany();
  return matches.map((m) => ({
    id: m.id,
    playedAt: m.playedAt,
    playerAId: m.playerAId,
    playerBId: m.playerBId,
    playerASets: m.playerASets,
    playerBSets: m.playerBSets,
    winnerId: m.winnerId,
  }));
}

export async function getDashboardStats() {
  const [players, matches] = await Promise.all([
    prisma.player.findMany(),
    loadAllMatchRecords(),
  ]);
  return buildDashboardStats(players, matches);
}

export async function getHeadToHeadStats(playerAId: string, playerBId: string) {
  const [playerA, playerB, matches] = await Promise.all([
    prisma.player.findUnique({ where: { id: playerAId } }),
    prisma.player.findUnique({ where: { id: playerBId } }),
    loadAllMatchRecords(),
  ]);

  if (!playerA || !playerB) {
    return null;
  }

  const stats = headToHead(matches, playerAId, playerBId);
  const matchHistory = matches
    .filter(
      (m) =>
        (m.playerAId === playerAId && m.playerBId === playerBId) ||
        (m.playerAId === playerBId && m.playerBId === playerAId)
    )
    .sort((a, b) => b.playedAt.getTime() - a.playedAt.getTime());

  return {
    playerA,
    playerB,
    stats,
    matchHistory,
  };
}
