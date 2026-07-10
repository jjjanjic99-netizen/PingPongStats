# PingPongStats

Interne Web-Applikation zur Erfassung von Tischtennis-Spielen im Büro, zur
Verwaltung von Spielern und zur Auswertung aussagekräftiger Statistiken (BI).
Für den lokalen Betrieb oder den Betrieb im internen Firmennetzwerk gedacht.

## Tech-Stack

| Bereich     | Wahl                                          |
|-------------|------------------------------------------------|
| Frontend    | Next.js 14 (App Router), React, TypeScript     |
| Backend     | Next.js Route Handlers (API Routes)            |
| Datenbank   | SQLite (Datei-basiert, kein separater Server)  |
| ORM         | Prisma                                         |
| Styling     | Tailwind CSS                                   |
| Charts      | Recharts                                       |
| Tests       | Vitest                                         |

Architekturentscheidung: Ein einziges Next.js-Projekt für Frontend und Backend
ist für den Umfang dieser Anwendung ausreichend und hält die Struktur einfach.
Die Statistik- und Validierungslogik liegt zentral in `src/lib/stats` (reine,
getestete Funktionen) und `src/lib/services` (Datenzugriff über Prisma) — API-
Routes sind dünne Controller, das Frontend enthält keine eigene Business-Logik.

## Setup

Voraussetzung: Node.js 20+.

```bash
npm install
cp .env.example .env   # DATABASE_URL="file:./dev.db"
```

### Datenbank-Migration

```bash
npm run prisma:migrate
```

Erstellt/aktualisiert die SQLite-Datenbank (`prisma/dev.db`) anhand von
`prisma/schema.prisma`.

### Seed-Daten laden

```bash
npm run seed
```

Legt 8 Beispielspieler (einer davon archiviert) und 65 realistische Spiele über
die letzten 120 Tage an (deterministisch, reproduzierbar). Das Skript leert
vorher bestehende Spieler/Spiele — nicht auf einer produktiven Datenbank
ausführen.

### Applikation starten

```bash
npm run dev
```

Anschliessend [http://localhost:3000](http://localhost:3000) öffnen.

Für einen Produktions-ähnlichen Betrieb im Firmennetzwerk:

```bash
npm run build
npm run start
```

### Tests ausführen

```bash
npm run test
```

Führt die Vitest-Suite für die Statistiklogik aus (`src/lib/stats/*.test.ts`).

## Funktionsumfang

- **Spieler-Verwaltung**: Anlegen, Bearbeiten, Deaktivieren/Reaktivieren
  (Archivierung statt Löschen — historische Spiele bleiben erhalten).
- **Spiele erfassen**: Datum/Uhrzeit, Spieler A/B, Satzresultat, optionale
  Notiz. Gewinner wird serverseitig aus dem Satzresultat abgeleitet.
  Validierung: keine identischen Spieler, kein Unentschieden, keine negativen
  Sätze. Schnellauswahl-Buttons für typische Ergebnisse (1:0 … 3:2), beliebige
  andere Resultate sind manuell möglich.
- **Spielübersicht**: Filterbar nach Zeitraum, Spieler, Gewinner; sortierbar
  nach Datum; Bearbeiten und Löschen einzelner Spiele.
- **Dashboard/BI**: Kennzahlen, Ranking-Tabelle, Charts (siehe unten).
- **Head-to-Head**: Direkter Vergleich zweier Spieler mit Spielverlauf.
- **Einstellungen**: Dark Mode, CSV-Export/-Import.

### Zusatzfeatures

- **Elo-Rating** pro Spieler (K-Faktor 32, Startwert 1500), chronologisch über
  die gesamte Spielhistorie berechnet.
- **Ranking-Tabelle** auf dem Dashboard, sortiert nach Elo.
- **Recent Form** (letzte 5 Spiele als W/L, neuestes zuerst).
- **CSV-Import/-Export** von Spielen (`/settings`).
- **Dark Mode** (persistiert in `localStorage`, respektiert Systemeinstellung
  beim ersten Besuch).
- **Achievement-Badges**: "Hot Streak" (≥3 aktuelle Siege), "Comeback King"
  (mind. einmal nach ≥3 Niederlagen in Folge wieder gewonnen), "Most Active
  Player" (meiste erfasste Spiele).
- **Saison-/Turniermodus**: bewusst nicht umgesetzt (Umfang), aber strukturell
  einfach nachrüstbar (z. B. optionales `seasonId`-Feld auf `Match`).

## Statistiklogik

Implementiert in `src/lib/stats/` (reine Funktionen, keine Datenbankzugriffe,
vollständig unit-getestet). Zentrale Definitionen:

- **Siegquote (overall)**: `Siege / gespielte Spiele * 100`.
- **Siegquote letzter Monat**: wie oben, aber nur Spiele der letzten 30 Tage
  relativ zu einem Referenzdatum (Default: jetzt).
- **Aktuelle Siegesserie**: aufeinanderfolgende Siege (oder Niederlagen),
  rückwärts gezählt ab dem aktuellsten Spiel des Spielers, bis das Ergebnis
  wechselt.
- **Längste Siegesserie**: längste zusammenhängende Folge von Siegen in der
  gesamten Historie eines Spielers (chronologisch, unabhängig von aktuell).
- **Head-to-Head**: Spiele, Siege und Siegquote zweier Spieler gegeneinander,
  unabhängig davon, wer Spieler A/B war.
- **Elo-Rating**: Standard-Elo-Formel, Spiele chronologisch verarbeitet.
- **Wenige Spiele**: Spieler mit weniger als 3 Spielen werden in der
  Ranking-Tabelle mit einem Hinweis-Badge markiert.

Diese Funktionen werden von `src/lib/services/stats-service.ts` mit
Datenbankdaten aufgerufen und über `/api/stats/dashboard` bzw.
`/api/stats/head-to-head` bereitgestellt. Das Frontend berechnet keine eigene
Statistiklogik, sondern zeigt nur die vom Backend gelieferten Werte an.

## Visualisierungen (Dashboard)

- Balkendiagramm: Siege pro Spieler
- Balkendiagramm: Siegquote pro Spieler
- Balkendiagramm: Spiele pro Woche
- Balkendiagramm: Streak-Ranking (aktuelle Serie, farblich nach Sieg/Niederlage)
- Head-to-Head: Vergleichs-Balkendiagramm + Spielverlauf-Tabelle

Farben folgen einer festen, kontrastgeprüften kategorialen Palette (siehe
`src/lib/colors.ts`) — jeder Spieler behält seine Farbe über alle Charts
hinweg (Identität statt Reihenfolge). Bei mehr als 8 gleichzeitig dargestellten
Spielern wird auf eine neutrale Graufarbe zurückgefallen.

## Datenmodell

```
Player
  id, displayName, firstName?, lastName?, email?, isActive, createdAt, updatedAt

Match
  id, playedAt, playerAId, playerBId, playerASets, playerBSets,
  winnerId (serverseitig berechnet), notes?, createdAt, updatedAt
```

Spieler werden nie gelöscht, nur über `isActive` archiviert — Fremdschlüssel
auf `Match` sind `onDelete: Restrict`, sodass die Spielhistorie auch nach
Archivierung eines Spielers vollständig erhalten bleibt.

## API-Übersicht

| Methode | Pfad                              | Zweck                                   |
|---------|------------------------------------|------------------------------------------|
| GET     | `/api/players`                     | Spieler auflisten (`?includeInactive=true`) |
| POST    | `/api/players`                     | Spieler erstellen                       |
| PATCH   | `/api/players/:id`                 | Spieler aktualisieren / (de)aktivieren  |
| GET     | `/api/matches`                     | Spiele auflisten (Filter: `playerId`, `winnerId`, `from`, `to`, `sort`) |
| POST    | `/api/matches`                     | Spiel erstellen                         |
| PATCH   | `/api/matches/:id`                 | Spiel aktualisieren                     |
| DELETE  | `/api/matches/:id`                 | Spiel löschen                           |
| GET     | `/api/matches/export`              | Alle Spiele als CSV exportieren         |
| POST    | `/api/matches/import`              | Spiele aus CSV importieren              |
| GET     | `/api/stats/dashboard`             | Dashboard-Statistiken                   |
| GET     | `/api/stats/head-to-head`          | Head-to-Head-Statistik (`?playerAId=&playerBId=`) |

## Projektstruktur

```
prisma/                   Schema, Migrationen, Seed-Skript
src/app/                  Next.js App Router: Seiten + API-Routes
src/components/           UI-Komponenten (nach Seite/Bereich gruppiert)
src/lib/stats/            Reine Statistik- und Validierungslogik (+ Tests)
src/lib/services/         Datenzugriff (Prisma) + Aufruf der Statistiklogik
src/lib/prisma.ts         Prisma-Client-Singleton
src/lib/colors.ts         Chart-Farbpalette
src/lib/csv.ts            CSV-Import/-Export-Hilfsfunktionen
```

## Bekannte Einschränkungen / Hinweise

- `npm audit` zeigt einige verbleibende Findings in `next` (kein gepatchtes
  14.x zum Zeitpunkt der Erstellung verfügbar) sowie in Dev-/Lint-Tooling
  (`glob`, `eslint-plugin-next`). Für den internen, nicht öffentlich
  exponierten Einsatz vertretbar; vor einem Produktiv-Einsatz mit externem
  Zugriff sollten Abhängigkeiten erneut geprüft/aktualisiert werden.
- Die App ist für einen überschaubaren Nutzerkreis (Büro-Tischtennis) und ohne
  Authentifizierung ausgelegt. Für den Betrieb im Firmennetzwerk ohne
  weiteren Zugriffsschutz vorgesehen; bei Bedarf lässt sich eine
  Authentifizierungsschicht ergänzen, ohne die Business-Logik anzupassen.
