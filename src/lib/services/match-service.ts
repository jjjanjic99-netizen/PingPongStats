import { prisma } from "@/lib/prisma";
import { computeWinnerId, MatchValidationError } from "@/lib/stats";

export class MatchNotFoundError extends Error {}

export interface MatchInput {
  playedAt: Date;
  playerAId: string;
  playerBId: string;
  playerASets: number;
  playerBSets: number;
  notes?: string | null;
}

export interface MatchFilters {
  playerId?: string;
  winnerId?: string;
  from?: Date;
  to?: Date;
  sort?: "asc" | "desc";
}

async function assertPlayersExist(playerAId: string, playerBId: string) {
  const players = await prisma.player.findMany({
    where: { id: { in: [playerAId, playerBId] } },
    select: { id: true },
  });
  const foundIds = new Set(players.map((p) => p.id));
  if (!foundIds.has(playerAId) || !foundIds.has(playerBId)) {
    throw new MatchValidationError("Einer der ausgewählten Spieler existiert nicht.");
  }
}

export async function listMatches(filters: MatchFilters = {}) {
  return prisma.match.findMany({
    where: {
      AND: [
        filters.playerId
          ? {
              OR: [
                { playerAId: filters.playerId },
                { playerBId: filters.playerId },
              ],
            }
          : {},
        filters.winnerId ? { winnerId: filters.winnerId } : {},
        filters.from ? { playedAt: { gte: filters.from } } : {},
        filters.to ? { playedAt: { lte: filters.to } } : {},
      ],
    },
    include: { playerA: true, playerB: true, winner: true },
    orderBy: { playedAt: filters.sort === "asc" ? "asc" : "desc" },
  });
}

export async function getMatch(id: string) {
  const match = await prisma.match.findUnique({
    where: { id },
    include: { playerA: true, playerB: true, winner: true },
  });
  if (!match) throw new MatchNotFoundError(`Spiel ${id} nicht gefunden.`);
  return match;
}

export async function createMatch(input: MatchInput) {
  await assertPlayersExist(input.playerAId, input.playerBId);
  const winnerId = computeWinnerId(input);

  return prisma.match.create({
    data: {
      playedAt: input.playedAt,
      playerAId: input.playerAId,
      playerBId: input.playerBId,
      playerASets: input.playerASets,
      playerBSets: input.playerBSets,
      winnerId,
      notes: input.notes?.trim() || null,
    },
    include: { playerA: true, playerB: true, winner: true },
  });
}

export async function updateMatch(id: string, input: MatchInput) {
  const existing = await prisma.match.findUnique({ where: { id } });
  if (!existing) throw new MatchNotFoundError(`Spiel ${id} nicht gefunden.`);

  await assertPlayersExist(input.playerAId, input.playerBId);
  const winnerId = computeWinnerId(input);

  return prisma.match.update({
    where: { id },
    data: {
      playedAt: input.playedAt,
      playerAId: input.playerAId,
      playerBId: input.playerBId,
      playerASets: input.playerASets,
      playerBSets: input.playerBSets,
      winnerId,
      notes: input.notes?.trim() || null,
    },
    include: { playerA: true, playerB: true, winner: true },
  });
}

export async function deleteMatch(id: string) {
  const existing = await prisma.match.findUnique({ where: { id } });
  if (!existing) throw new MatchNotFoundError(`Spiel ${id} nicht gefunden.`);
  await prisma.match.delete({ where: { id } });
}

export { MatchValidationError };
