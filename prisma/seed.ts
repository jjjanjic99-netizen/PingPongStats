import { PrismaClient } from "@prisma/client";
import { computeWinnerId } from "../src/lib/stats/validation";

const prisma = new PrismaClient();

// Deterministic PRNG (mulberry32) so the seed produces the same demo data
// on every run, which makes screenshots and manual testing reproducible.
function mulberry32(seed: number) {
  let a = seed;
  return function () {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const random = mulberry32(42);

function randomInt(min: number, max: number): number {
  return Math.floor(random() * (max - min + 1)) + min;
}

function pick<T>(items: T[]): T {
  return items[randomInt(0, items.length - 1)];
}

const PLAYERS = [
  { displayName: "Anna Berger", firstName: "Anna", lastName: "Berger", skill: 1650 },
  { displayName: "Ben Hofer", firstName: "Ben", lastName: "Hofer", skill: 1580 },
  { displayName: "Clara Wagner", firstName: "Clara", lastName: "Wagner", skill: 1720 },
  { displayName: "David Keller", firstName: "David", lastName: "Keller", skill: 1500 },
  { displayName: "Elena Frei", firstName: "Elena", lastName: "Frei", skill: 1610 },
  { displayName: "Fabian Roth", firstName: "Fabian", lastName: "Roth", skill: 1470 },
  { displayName: "Giulia Meier", firstName: "Giulia", lastName: "Meier", skill: 1550 },
  { displayName: "Hannes Stark", firstName: "Hannes", lastName: "Stark", skill: 1440, isActive: false },
];

// Typical best-of result modes; the winner is always assigned the higher score.
const RESULT_MODES: [number, number][] = [
  [1, 0],
  [2, 0],
  [2, 1],
  [3, 0],
  [3, 1],
  [3, 2],
];

const MATCH_COUNT = 65;
const DAYS_SPAN = 120;

async function main() {
  console.log("Clearing existing data...");
  await prisma.match.deleteMany();
  await prisma.player.deleteMany();

  console.log("Creating players...");
  const players = await Promise.all(
    PLAYERS.map((p) =>
      prisma.player.create({
        data: {
          displayName: p.displayName,
          firstName: p.firstName,
          lastName: p.lastName,
          email: `${p.firstName.toLowerCase()}.${p.lastName.toLowerCase()}@firma.example`,
          isActive: p.isActive ?? true,
        },
      })
    )
  );
  const skillById = new Map(players.map((p, i) => [p.id, PLAYERS[i].skill]));

  console.log(`Creating ${MATCH_COUNT} matches...`);
  const now = new Date();
  const matchDates: Date[] = [];
  for (let i = 0; i < MATCH_COUNT; i++) {
    const daysAgo = randomInt(0, DAYS_SPAN);
    const date = new Date(now);
    date.setDate(date.getDate() - daysAgo);
    date.setHours(randomInt(8, 18), pick([0, 15, 30, 45]), 0, 0);
    matchDates.push(date);
  }
  matchDates.sort((a, b) => a.getTime() - b.getTime());

  for (const playedAt of matchDates) {
    let playerA = pick(players);
    let playerB = pick(players);
    while (playerB.id === playerA.id) {
      playerB = pick(players);
    }

    const skillA = skillById.get(playerA.id)!;
    const skillB = skillById.get(playerB.id)!;
    const expectedA = 1 / (1 + 10 ** ((skillB - skillA) / 400));
    const aWins = random() < expectedA;

    const [loserSets, winnerSets] = pick(RESULT_MODES);
    const playerASets = aWins ? winnerSets : loserSets;
    const playerBSets = aWins ? loserSets : winnerSets;

    const winnerId = computeWinnerId({
      playerAId: playerA.id,
      playerBId: playerB.id,
      playerASets,
      playerBSets,
    });

    await prisma.match.create({
      data: {
        playedAt,
        playerAId: playerA.id,
        playerBId: playerB.id,
        playerASets,
        playerBSets,
        winnerId,
        notes: random() < 0.15 ? pick(["Knappes Spiel!", "Rematch gefordert", "Gutes Niveau heute", null]) : null,
      },
    });
  }

  const totalMatches = await prisma.match.count();
  console.log(`Done. ${players.length} players, ${totalMatches} matches.`);
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
