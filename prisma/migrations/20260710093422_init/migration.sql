-- CreateTable
CREATE TABLE "Player" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "displayName" TEXT NOT NULL,
    "firstName" TEXT,
    "lastName" TEXT,
    "email" TEXT,
    "isActive" BOOLEAN NOT NULL DEFAULT true,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL
);

-- CreateTable
CREATE TABLE "Match" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "playedAt" DATETIME NOT NULL,
    "playerAId" TEXT NOT NULL,
    "playerBId" TEXT NOT NULL,
    "playerASets" INTEGER NOT NULL,
    "playerBSets" INTEGER NOT NULL,
    "winnerId" TEXT NOT NULL,
    "notes" TEXT,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL,
    CONSTRAINT "Match_playerAId_fkey" FOREIGN KEY ("playerAId") REFERENCES "Player" ("id") ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT "Match_playerBId_fkey" FOREIGN KEY ("playerBId") REFERENCES "Player" ("id") ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT "Match_winnerId_fkey" FOREIGN KEY ("winnerId") REFERENCES "Player" ("id") ON DELETE RESTRICT ON UPDATE CASCADE
);

-- CreateIndex
CREATE INDEX "Player_isActive_idx" ON "Player"("isActive");

-- CreateIndex
CREATE INDEX "Match_playedAt_idx" ON "Match"("playedAt");

-- CreateIndex
CREATE INDEX "Match_playerAId_idx" ON "Match"("playerAId");

-- CreateIndex
CREATE INDEX "Match_playerBId_idx" ON "Match"("playerBId");

-- CreateIndex
CREATE INDEX "Match_winnerId_idx" ON "Match"("winnerId");
