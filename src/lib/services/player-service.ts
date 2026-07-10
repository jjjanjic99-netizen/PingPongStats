import { prisma } from "@/lib/prisma";

export interface PlayerInput {
  displayName: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
}

export class PlayerNotFoundError extends Error {}
export class PlayerValidationError extends Error {}

function assertValidPlayerInput(input: PlayerInput) {
  if (!input.displayName || !input.displayName.trim()) {
    throw new PlayerValidationError("Anzeigename ist erforderlich.");
  }
  if (input.email && !/^\S+@\S+\.\S+$/.test(input.email)) {
    throw new PlayerValidationError("E-Mail-Adresse ist ungültig.");
  }
}

export async function listPlayers(includeInactive: boolean) {
  return prisma.player.findMany({
    where: includeInactive ? {} : { isActive: true },
    orderBy: { displayName: "asc" },
  });
}

export async function getPlayer(id: string) {
  const player = await prisma.player.findUnique({ where: { id } });
  if (!player) throw new PlayerNotFoundError(`Spieler ${id} nicht gefunden.`);
  return player;
}

export async function createPlayer(input: PlayerInput) {
  assertValidPlayerInput(input);
  return prisma.player.create({
    data: {
      displayName: input.displayName.trim(),
      firstName: input.firstName?.trim() || null,
      lastName: input.lastName?.trim() || null,
      email: input.email?.trim() || null,
    },
  });
}

export async function updatePlayer(id: string, input: Partial<PlayerInput>) {
  const existing = await prisma.player.findUnique({ where: { id } });
  if (!existing) throw new PlayerNotFoundError(`Spieler ${id} nicht gefunden.`);

  if (input.displayName !== undefined || input.email !== undefined) {
    assertValidPlayerInput({
      displayName: input.displayName ?? existing.displayName,
      email: input.email ?? existing.email,
    });
  }

  return prisma.player.update({
    where: { id },
    data: {
      displayName: input.displayName?.trim(),
      firstName: input.firstName === undefined ? undefined : input.firstName?.trim() || null,
      lastName: input.lastName === undefined ? undefined : input.lastName?.trim() || null,
      email: input.email === undefined ? undefined : input.email?.trim() || null,
    },
  });
}

/** Archives a player without deleting historical matches. */
export async function setPlayerActive(id: string, isActive: boolean) {
  const existing = await prisma.player.findUnique({ where: { id } });
  if (!existing) throw new PlayerNotFoundError(`Spieler ${id} nicht gefunden.`);
  return prisma.player.update({ where: { id }, data: { isActive } });
}
