# PingPongStats (Desktop)

Eine klassische Windows-Desktop-Applikation (WPF, .NET 8, MVVM) zur Erfassung
von Tischtennis-Spielen, Verwaltung von Spielern und Auswertung von
Statistiken im Unternehmen. Keine Datenbank, kein Webserver, keine Cloud -
alle Daten liegen als XML-Dateien auf einem frei wählbaren Pfad (lokal oder
Netzwerkfreigabe).

> Dieses Verzeichnis (`desktop/`) ist ein eigenständiges .NET-Projekt neben der
> bereits vorhandenen Next.js-Web-Applikation im Repository-Root. Beide
> Deliverables sind unabhängig voneinander lauffähig.

## Architekturentscheidung

- **Trennung Core/App**: Alle Fachlogik (Models, Repositories, Services,
  ViewModels) liegt in `PingPongStats.Core`, einer reinen .NET-8-Klassen-
  bibliothek **ohne WPF-Abhängigkeit**. `PingPongStats.App` (WPF) enthält
  ausschliesslich Views (XAML) und ein paar dünne, plattformspezifische
  Adapter (Ordner-Dialog, Explorer öffnen). Das macht die gesamte
  Business-Logik unabhängig von WPF testbar und plattformunabhängig baubar.
- **MVVM** über [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
  (`ObservableObject`, `[ObservableProperty]`, `RelayCommand`) - keine
  WPF-Abhängigkeit, funktioniert daher problemlos in `PingPongStats.Core`.
- **Navigation**: Klassisches View-Model-first-Pattern. `MainViewModel` hält
  `CurrentViewModel`; `MainWindow.xaml` definiert `DataTemplate`s pro
  ViewModel-Typ, die automatisch die passende View rendern.
- **XML-Persistenz**: Ein generischer `AtomicXmlFileStore<T>` kapselt Sperre,
  Backup, Temp-Datei, Validierung und atomaren Replace; `PlayerXmlRepository`
  und `MatchXmlRepository` nutzen ihn für `players.xml` bzw. `matches.xml`.
- **Charts**: Bewusst keine externe Chart-Bibliothek, sondern einfache,
  handgebaute Balkendiagramme (ItemsControl + Border), um das
  Single-File-Publish nicht durch zusätzliche native Abhängigkeiten zu
  gefährden. Das Chart-Template ist zentral in `Themes/Styles.xaml` definiert
  und wird von Dashboard und Doppel-Ansicht gemeinsam genutzt.
- **Zeitraum-Filter**: `DashboardRangeFilter` ist eine einzige, geteilte
  Instanz (erzeugt in `MainViewModel`), die sowohl vom Einzel- als auch vom
  Doppel-Dashboard referenziert wird - eine Änderung des Zeitraums auf einer
  Seite gilt für beide.
- **UI-Grösse**: Statt jede Schriftgrösse/jedes Padding einzeln zu skalieren,
  wird ein `ScaleTransform` (`LayoutTransform`) auf den gesamten
  Fensterinhalt angewendet (`MainWindow.xaml`/`UiScaleManager`) - das skaliert
  wirklich alle Elemente proportional mit einer einzigen Einstellung.

### Wichtiger Hinweis zur Build-Umgebung dieser Session

Diese Session lief in einer **Linux**-Sandbox. WPF-Projekte (`net8.0-windows`,
`UseWPF=true`) können **grundsätzlich nur unter Windows kompiliert werden** -
das WPF-Build-Tooling (`Microsoft.NET.Sdk.WindowsDesktop.targets`,
Markup-Compiler) wird von Microsoft nicht für Linux/macOS ausgeliefert. Das
ist keine Einschränkung dieses Projekts, sondern eine generelle Grenze von
.NET/WPF.

Um trotzdem maximale Qualität zu liefern, wurde deshalb wie folgt vorgegangen:

- **`PingPongStats.Core` und `PingPongStats.Tests` wurden in dieser Session
  vollständig gebaut, alle 197 Unit-Tests laufen grün** (`dotnet test`).
- **`PingPongStats.App` (WPF) konnte nicht kompiliert werden.** Der Code wurde
  daher besonders sorgfältig von Hand geschrieben und zusätzlich statisch
  geprüft: alle XAML-Dateien sind wohlgeformtes XML, alle `x:Class`-Werte
  stimmen exakt mit Namespace/Klasse im Code-Behind überein, alle
  `StaticResource`/`DynamicResource`-Schlüssel sind definiert, und jede in
  XAML gebundene Property/Command wurde gegen das jeweilige ViewModel
  abgeglichen.
- **Bitte führen Sie vor dem produktiven Einsatz einmal `dotnet build` bzw.
  `dotnet publish` auf einem Windows-Rechner aus**, um die WPF-Kompilierung
  zu verifizieren (siehe unten). Sollten dabei kleinere XAML-Tippfehler
  auftauchen, sind das lokal isolierte, leicht behebbare Fehler - die
  Architektur und die gesamte Fachlogik sind unabhängig davon bereits
  vollständig getestet.

## Voraussetzungen

- **Windows 10/11** zum Bauen und Ausführen von `PingPongStats.App`.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Kein SQL Server, kein IIS, keine weitere Software nötig.

## Projektstruktur

```
desktop/
  PingPongStats.sln
  src/
    PingPongStats.Core/       Models, Repositories, Services, ViewModels (kein WPF)
      Models/                 Player, Match, AppSettingsModel, AuditLogEntry
      Repositories/           XML-Persistenz, Locking, atomare Schreiblogik
      Services/               Validierung, Statistik, Elo, Dashboard, CSV, Seed
      ViewModels/             MVVM-ViewModels je Bildschirm
    PingPongStats.App/        WPF: Views (XAML) + plattformspezifische Adapter
      Views/                  DashboardView, PlayersView, MatchesView, LoginView, ProfileView, ...
      Controls/               AvatarControl (S/M/L, überall wiederverwendet)
      Themes/                 Light.xaml, Dark.xaml, Styles.xaml
      Converters/             WPF-Value-Converter
      Services/               IFolderPickerService-/IShellService-/IAvatarImageService-Implementierungen
  tests/
    PingPongStats.Tests/      xUnit-Tests für Core
```

## Build-Anleitung

Auf einem Windows-Rechner mit installiertem .NET 8 SDK:

```powershell
cd desktop
dotnet restore
dotnet build
```

Nur die plattformunabhängigen Teile bauen/testen (funktioniert auch unter
Linux/macOS, z. B. in CI ohne Windows-Runner):

```bash
dotnet build src/PingPongStats.Core/PingPongStats.Core.csproj
dotnet test tests/PingPongStats.Tests/PingPongStats.Tests.csproj
```

## Tests ausführen

```powershell
cd desktop
dotnet test
```

Deckt ab: Gewinnerberechnung, ungültige Satzresultate, Siegquote overall,
Siegquote letzte 30 Tage, aktuelle Siegesserie, längste Sieges-/
Niederlagenserie, Satzdifferenz, Head-to-Head-Statistik, Elo-Berechnung
(inkl. Verlaufs-Historie), XML laden/speichern/Backup, Verhalten bei
fehlenden/beschädigten XML-Dateien und bei altem XML-Format ohne die neuen
optionalen Felder (Migration), Datenpfad-Bootstrap, die zentrale
`PingPongDataService`-Fassade (inkl. "Löschen nur ohne Spiele"), PIN-Hashing/
-Verifikation, Angstgegner/Lieblingsgegner (inkl. Tiebreak und
Mindest-Spiele-Schwelle), Player of the Week (Score-Formel, 7-Tage-Fenster,
Einzel+Doppel-Kombination, alle Tiebreak-Stufen), Bestes Comeback
(Mindest-Rückstand, maximaler Rückstand, Tiebreak, "keine Satzdaten"-Fall),
alle Badge-Regeln (exakte Schwellwerte, Grenzfälle, Gleichstände),
Trash-Talk-Sprüche (Kategorie-Priorität, "Kategorie fehlt"-Fall, Seeding/
Nicht-Überschreiben von `quotes.xml`), die Elo-Prognoseformel (inkl.
Symmetrie und Team-Elo-Durchschnitt), Rivalität des Monats (Zeitfenster,
Mindest-Spiele, Tiebreak), die häufigsten-Gegner-Ermittlung fürs
Bilanz-Countdown, die Tageszeit-Statistik (exakte Stunden-Grenzen aller fünf
Blöcke, Mindest-Spiele für Anzeige vs. für den Beste-Zeit-Hinweis) sowie die
Ligatabelle (Punkteformel, Zeitfenster-basierte Saison-Zuordnung, Set-
differenz- und direkter-Vergleich-Tiebreak, getrennte Einzel-/Doppel-Auswertung).

## Anwendung starten (Entwicklung)

```powershell
cd desktop
dotnet run --project src/PingPongStats.App
```

Beim allerersten Start (kein Datenpfad konfiguriert) öffnet sich ein Dialog
zur Auswahl des Datenordners - siehe [Datenpfad](#datenpfad).

## Publish als Single-File-EXE

```powershell
cd desktop
dotnet publish src/PingPongStats.App/PingPongStats.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Das Ergebnis liegt danach unter:

```
desktop/src/PingPongStats.App/bin/Release/net8.0-windows/win-x64/publish/PingPongStats.exe
```

Diese `PingPongStats.exe` ist eigenständig lauffähig (keine .NET-Installation
auf dem Zielrechner nötig, kein Installer erforderlich) - einfach kopieren und
starten. Das Projekt ist bereits mit `SelfContained`, `PublishSingleFile` und
`IncludeNativeLibrariesForSelfExtract` als Standardwerte vorkonfiguriert
(`PingPongStats.App.csproj`), der obige Befehl funktioniert daher auch ganz
ohne die `/p:...`-Parameter - sie sind hier der Vollständigkeit halber
trotzdem aufgeführt.

## Datenpfad

Der Datenpfad ist der Ordner, in dem `players.xml`, `matches.xml` und
`audit-log.xml` liegen. Er kann ein lokaler Ordner sein:

```
C:\PingPongStats\Data\
```

oder ein Netzwerkpfad, damit mehrere Kolleg:innen dieselben Daten nutzen:

```
\\fileserver\PingPongStats\
```

**Erststart-Ablauf:**

1. Die App prüft `%AppData%\PingPongStats\appsettings.json` auf einen
   konfigurierten `DataPath`.
2. Ist keiner konfiguriert (oder nicht erreichbar/beschreibbar), öffnet sich
   ein Dialog zur Auswahl/Eingabe eines Ordners.
3. Der gewählte Pfad wird lokal in `appsettings.json` gespeichert.
4. Fehlende Ordner/XML-Dateien werden automatisch angelegt
   (`players.xml`, `matches.xml`, `audit-log.xml`, Unterordner `backups\`).
5. Danach startet die Anwendung normal.

Der Pfad kann jederzeit unter **Einstellungen → Datenpfad ändern** angepasst
werden; dabei wird erneut geprüft, ob der Ordner erreichbar und beschreibbar
ist, bevor umgeschaltet wird.

Beispiel `appsettings.json`:

```json
{
  "DataPath": "\\\\fileserver\\PingPongStats\\",
  "UiScale": "Medium",
  "ShowWinAnimation": true
}
```

## XML-Dateien

| Datei/Ordner | Inhalt |
|---|---|
| `players.xml` | Alle Spieler (auch archivierte - Historie bleibt erhalten), inkl. `AvatarFileName`, `PinHash`, `PinSalt` |
| `matches.xml` | Alle Einzel-Spiele, optional inkl. `SetResults` (Satzdetail) |
| `doubles.xml` | Alle Doppel-Spiele (2 vs 2), optional inkl. `SetResults` |
| `audit-log.xml` | Optionales Änderungsprotokoll (wer hat was geändert) |
| `quotes.xml` | Trash-Talk-Sprüche fürs Gewinn-Overlay, bewusst frei editierbar |
| `seasons.xml` | Manuell angelegte Liga-Saisons |
| `tournaments.xml` | Turniere (Teilnehmer, Setzliste, Baum, Status) |
| `pendingmatches.xml` | Angekündigte, noch nicht gespielte Partien fürs Tippspiel |
| `bets.xml` | Tipps der Spieler auf angekündigte Partien |
| `avatars\` | Verarbeitete Profilbilder, `{PlayerId}.png`, max. 512x512 px |

Alle XML-Dateien werden **UTF-8 ohne BOM**, eingerückt und ohne
`xmlns`/`xsi`-Rauschen geschrieben (siehe `XmlSerialization` in
`PlayerXmlRepository.cs`). Struktur entspricht exakt der Vorgabe:

```xml
<Players>
  <Player>
    <Id>...</Id>
    <DisplayName>Markus</DisplayName>
    <FirstName>Markus</FirstName>
    <LastName>Schlegel</LastName>
    <Email></Email>
    <IsActive>true</IsActive>
    <CreatedAt>2026-07-10T12:00:00</CreatedAt>
    <UpdatedAt>2026-07-10T12:00:00</UpdatedAt>
  </Player>
</Players>
```

## Doppel-Spiele (2 vs 2)

Neben Einzel-Spielen können auch Doppel-Spiele erfasst werden (Nav-Eintrag
**Doppel**): zwei Teams à zwei Spieler, Satzresultat, Gewinner wird analog zum
Einzel serverseitig aus den Sätzen abgeleitet (`ValidationService.ComputeWinningTeam`).
Alle vier Spieler müssen unterschiedlich sein.

Datenmodell (`DoubleMatch`, persistiert in `doubles.xml`):

```
Id, PlayedAt,
TeamAPlayer1Id, TeamAPlayer2Id, TeamBPlayer1Id, TeamBPlayer2Id,
TeamASets, TeamBSets, WinningTeam ("A"/"B"),
Notes, CreatedAt, UpdatedAt
```

Die **Doppel**-Seite kombiniert:

- Dashboard-Kennzahlen (Anzahl Doppel-Spiele, beste Team-Konstellation) für
  den gewählten Zeitraum (geteilter Filter mit dem Haupt-Dashboard)
- Ein Ranking aller **Team-Konstellationen** (`DoublesStatsService.GetPairingRankings`):
  jede Zweier-Paarung wird unabhängig davon aggregiert, auf welcher Seite und
  in welcher Reihenfolge sie gespielt hat, und nach Siegquote sortiert - das
  beantwortet direkt "welche Teamkonstellation ist besser als andere"
- Ein Balkendiagramm der Siegquote pro Konstellation
- Die chronologische Liste aller Doppel-Spiele mit Löschen-Funktion
- Ein Formular zum Erfassen eines neuen Doppel-Spiels (analog zum
  Einzel-Formular: Datum/Uhrzeit, 4 Spielerauswahlen, Schnellauswahl-Buttons
  für typische Ergebnisse, manuelle Satzeingabe, Notiz)

## Neue Features

Erweiterung dieser Session in 7 Phasen (0-6), jede Phase committet einzeln;
alle bestehenden Tests blieben grün, keine Web-App-Änderungen. Alle
Berechnungslogik liegt wie zuvor ausschliesslich in `PingPongStats.Core`
(keine WPF-Abhängigkeit), jede neue Statistik-Funktion hat xUnit-Tests.

### Satzergebnisse (optional)

Match und DoubleMatch können pro Satz `SetNumber`/`PointsA`/`PointsB` erfassen
(`SetResults`-Liste, `Models/SetResult.cs`). Komplett optional - alte Spiele
ohne Satzdetail bleiben unverändert nutzbar (leere Liste statt erfundener
Werte). Wird beim Speichern etwas eingetragen, muss die Anzahl gewonnener
Sätze je Seite exakt zum eingegebenen Gesamt-Score passen
(`ValidationService.ValidateSetResults`), sonst schlägt das Speichern mit
einer klaren Fehlermeldung fehl.

### Profilbilder

Im Spieler-Dialog kann ein Bild (jpg/png) hochgeladen werden. Es wird
serverseitig (im WPF-App-Projekt, hinter `IAvatarImageService`) quadratisch
zentriert zugeschnitten, auf maximal 512x512 px skaliert und als
`{DataPath}\avatars\{PlayerId}.png` gespeichert. Ohne Bild: ein Kreis mit den
Initialen des Spielers und einer deterministisch aus der Spieler-ID
abgeleiteten Farbe (`AvatarService`). Das wiederverwendbare `AvatarControl`
(Grössen S/M/L) wird überall verwendet: Spielerliste/-dialog,
Dashboard-Ranking, Login, Mein Profil, Gewinn-Animation.

### Anmeldung & Mein Profil

> **Wichtig: Das ist Bequemlichkeit, keine Sicherheit.** Der optionale
> 4-stellige PIN pro Spieler verhindert nur das versehentliche Öffnen eines
> fremden Profils. Die XML-Datendateien bleiben für jeden mit Zugriff auf den
> Datenordner uneingeschränkt lesbar und editierbar - PIN oder nicht. Es gibt
> **keine Authentifizierung** im eigentlichen Sinn, keine Zugriffskontrolle,
> keine Verschlüsselung der Daten.

Über **Anmelden** in der Seitenleiste öffnet sich eine Kachel-Auswahl aller
aktiven Spieler (mit Avatar). Ist für einen Spieler kein PIN gesetzt, loggt
ein Klick auf die Kachel direkt ein. Ist ein PIN gesetzt (im Spieler-Dialog
konfigurierbar, gespeichert als PBKDF2-Hash + Salt in `Player.PinHash`/
`PinSalt`, nie im Klartext), muss er erst korrekt eingegeben werden.

Nach dem Login zeigt **Mein Profil**: Spiele/Siege/Niederlagen, Siegquote,
Elo-Rating mit Verlaufs-Liniendiagramm (`EloService.GetRatingHistory`),
längste Sieges-/Niederlagenserie, Satzdifferenz, sowie **Angstgegner**
(schlechteste persönliche Siegquote) und **Lieblingsgegner** (beste
persönliche Siegquote) - jeweils nur bei mindestens 3 gemeinsamen
Einzel-Spielen, sonst "Noch zu wenig Spiele." (Tiebreak: mehr gemeinsame
Spiele). Der aktive Spieler ist oben rechts sichtbar, inkl. Abmelden-Button.

### Player of the Week

Dashboard-Hero-Card für die letzten 7 Tage (fester Zeitraum, unabhängig vom
sonstigen 7/30/90-Tage/gesamt-Filter): Score = (Siege × 2) − Niederlagen +
Satzdifferenz × 0,5, Einzel **und** Doppel zählen beide (ein Doppel-Spiel
zählt für beide Team-Mitglieder). Mindestens 3 Spiele im Zeitraum nötig,
sonst nicht qualifiziert. Tiebreak: höhere Siegquote, dann mehr Spiele, dann
Elo. Qualifiziert niemand, erscheint ein dezenter Platzhalter-Hinweis.

### Bestes Comeback

Dashboard-Karte (respektiert den normalen Zeitraum-Filter): Ein Comeback
liegt vor, wenn der Sieger zwischenzeitlich mit mindestens 2 Sätzen im
Rückstand lag und trotzdem gewonnen hat. Comeback-Wert = maximal
aufgeholter Rückstand; Tiebreak: knapperer Endstand. Benötigt zwingend die
Satzergebnisse aus Phase 0 - Spiele ohne Satzdetail werden komplett
ignoriert, nie geschätzt oder nachträglich konstruiert.

### Gewinn-Animation

Nach erfolgreichem Speichern eines Spiels (Einzel oder Doppel) erscheint für
ca. 3 Sekunden eine Vollbild-Überlagerung (jederzeit durch Klick schliessbar):
Gewinner-Avatar (bzw. bei Doppel beide Gewinner-Avatare nebeneinander) +
Name(n) + Endstand, plus ein "COMEBACK!"-Badge, falls
`ComebackService.IsComeback`/`IsComebackDoubles` für genau dieses Spiel
zutrifft. Die Konfetti-Partikel sind reines WPF/XAML (keine externe
Bibliothek) und laufen über `RenderTransform`-`DoubleAnimation`s auf dem
Compositor-Thread, blockieren also die UI nicht. Über **Einstellungen →
Gewinn-Animation nach dem Speichern anzeigen** (`ShowWinAnimation`,
Default: an) abschaltbar.

### Titel & Badges

Eine regelbasierte Badge-Engine (`Services/Badges/`) prüft für jeden Spieler
eine feste Liste von `IBadgeRule`-Implementierungen (`BadgeEngine.AllRules`)
gegen einen einmal aufgebauten `BadgeContext` (alle Spieler/Spiele/
Doppel-Spiele, aktuelle Elo-Werte). Ein neues Badge hinzufügen heisst: Regel
implementieren, in `BadgeEngine.AllRules` eintragen - keine weitere Stelle im
Code muss angepasst werden (keine wachsende if/else-Kette).

Startset an Badges:

- **Der Unbesiegte** 🔥 - mindestens 10 Siege in Folge, aktuell laufend
  (eine später gebrochene Serie zählt nicht mehr)
- **Aschenputtel** 🥿 - Sieg gegen einen aktuell in den Elo-Top-3 platzierten
  Spieler (nur Einzel)
- **Stammgast** 📆 - die meisten Spiele (Einzel + Doppel kombiniert) im
  laufenden Kalendermonat; bei Gleichstand erhalten alle Führenden das Badge
- **Eisenmann** 🦾 - 20+ Spiele insgesamt (Einzel + Doppel kombiniert)
- **Doppel-Spezialist** 🤝 - Doppel-Siegquote höher als Einzel-Siegquote, ab
  mindestens 5 Doppel-Spielen

Icons erscheinen neben dem Namen in der Spielerliste und im
Dashboard-Ranking (Tooltip zeigt alle verdienten Badges); auf **Mein Profil**
wird die volle Sammlung als Chips mit Tooltip (Beschreibung + Verdient-am-Datum)
angezeigt.

### Trash-Talk-Sprüche

Eine editierbare Spruch-Sammlung liegt unter `{DataPath}\quotes.xml` und wird
beim ersten Start mit einem Standard-Set angelegt (siehe
`Repositories/DefaultQuotes.cs`). Kategorien: `CleanSweep` (3:0/2:0 ohne
Satzverlust), `KnapperSieg` (Sieg mit genau 1 Satz Unterschied), `Comeback`,
`DoppelSieg`, `UnderdogSieg` (Sieger hatte vor dem Spiel die niedrigere
Elo-Bewertung; bei Doppel wird die durchschnittliche Team-Elo verglichen).

Im Konfetti-Overlay wird nach dem Speichern ein zufälliger Spruch der
zutreffenden Kategorie eingeblendet. Können mehrere Kategorien zutreffen
(z. B. ein Doppel-Comeback), gilt diese Priorität: Comeback →
Underdog-Sieg → Doppel-Sieg → Clean-Sweep → Knapper Sieg. Fehlt eine
Kategorie in der XML (gelöscht oder Tippfehler beim Bearbeiten), wird
einfach kein Spruch angezeigt - kein Fehler, kein Absturz.

**Die Datei ist bewusst editierbar**: Einträge hinzufügen, ändern oder
löschen wirkt sich sofort aus - `quotes.xml` wird bei jeder Spielerfassung neu
eingelesen (`PingPongDataService.Reload()`), kein Neustart der App nötig.
Format:

```xml
<Quotes>
  <Quote>
    <Category>CleanSweep</Category>
    <Text>Nicht einen Satz abgegeben - Respekt, aber auch: autsch.</Text>
  </Quote>
</Quotes>
```

Gültige `Category`-Werte: `CleanSweep`, `KnapperSieg`, `Comeback`,
`DoppelSieg`, `UnderdogSieg`. Die Datei wird nur einmal (beim ersten Start
bzw. beim ersten Zugriff auf einen neuen Datenpfad) mit den Standard-Sprüchen
angelegt und danach nie mehr automatisch überschrieben.

### Prognose, Rivalität des Monats, Bilanz-Countdown

- **Prognose**: Sobald im Spiel-Erfassungsdialog (Einzel oder Doppel) alle
  Spieler gewählt sind, wird die Elo-basierte Gewinnwahrscheinlichkeit
  angezeigt - Standardformel `1 / (1 + 10^((EloB - EloA) / 400))`
  (`EloPredictionService.ComputeWinProbability`). Bei Doppel wird pro Team
  der Durchschnitt der beiden Spieler-Elos verwendet
  (`ComputeTeamElo`) - ein separates Doppel-Elo gibt es nicht.
- **Rivalität des Monats** (Dashboard-Karte, immer feste 30-Tage-Fensicht,
  unabhängig vom 7/30/90-Tage/gesamt-Filter): die Einzel-Paarung mit den
  meisten gemeinsamen Spielen in den letzten 30 Tagen, ab mindestens 3
  Spielen; Tiebreak: knappere Bilanz (`RivalryService.FindRivalryOfTheMonth`).
  Zeigt beide Avatare + Bilanz.
- **Bilanz-Countdown** (Mein Profil): für die 3 häufigsten Gegner
  (`StatsService.GetMostFrequentOpponents`) wird angezeigt, wie viele Siege
  bis zur ausgeglichenen Bilanz fehlen ("Noch 2 Siege gegen Marco"); bei
  bereits ausgeglichener oder positiver Bilanz erscheint stattdessen
  "Ausgeglichen gegen ..." bzw. "Vorsprung: N gegen ...".

### Tageszeit-Statistik

Auf **Mein Profil** zeigt ein Balkendiagramm (`TimeOfDayService`) die
Siegquote in fünf festen Tageszeit-Blöcken: vor 10, 10-12, 12-14, 14-17,
nach 17 Uhr (Zuordnung über die Stunde von `Match.PlayedAt`). Blöcke mit
weniger als 3 Spielen werden nicht ausgeblendet, sondern nur ausgegraut.
Eine Textzeile ("Deine beste Zeit: vor 10 Uhr, 70% Siege") erscheint nur,
wenn der stärkste Block mindestens 5 Spiele hat.

### Saison / Ligatabelle

Saisons (`Season`, persistiert in `seasons.xml`) werden manuell unter
**Einstellungen** angelegt (Name, Start-/Enddatum, optional sofort aktiv) -
es ist zu jedem Zeitpunkt höchstens eine Saison aktiv; das Aktivieren einer
Saison deaktiviert automatisch jede andere.

Spiele werden **nicht** über ein explizites Feld einer Saison zugeordnet,
sondern rein anhand von `PlayedAt`: ein Spiel zählt für die Ligatabelle einer
Saison, wenn sein Datum im `[StartDate, EndDate]`-Fenster liegt. Spiele
ausserhalb jeder Saison bleiben ganz normal gültig und zählen für alle
anderen Statistiken (Elo, Profil, Badges, ...) - nur eben nicht in die
Liga-Tabelle.

Die neue Seite **Liga** zeigt die Tabelle der aktiven Saison, mit einem
Umschalter zwischen Einzel- und Doppel-Auswertung (`LeagueTableService` -
beide vollständig getrennt berechnet; ein Doppel-Ergebnis zählt für beide
Team-Mitglieder einzeln). Punktesystem: 3 Punkte pro Sieg, 0 pro Niederlage;
Tiebreak zuerst über die Satzdifferenz, danach über den direkten Vergleich
zwischen den betroffenen Spielern. Podest für die Top 3 plus die volle
Tabelle mit Avataren.

### Turniermodus

Auf der neuen Seite **Turnier** kann manuell (max. ein laufendes Turnier
gleichzeitig) ein K.-o.-Turnier gestartet werden - Einzel oder Doppel.
Teilnehmer werden per Checkbox-Liste ausgewählt; im Doppel-Modus können die
Teams entweder manuell über Dropdowns zusammengestellt oder per "Teams
auslosen" zufällig (Fisher-Yates, `TournamentService.DrawRandomTeams`)
gebildet werden.

Die Setzliste (`BracketService`) ordnet die Teilnehmer nach aktuellem
Elo-Rating (Doppel: Team-Elo = Mittelwert der beiden Spieler) und erzeugt
die Paarungen über das Standard-Turnierraster-Verfahren (rekursive
Seed-Reihenfolge), sodass Seed 1 und Seed 2 sich frühestens im Finale
treffen können. Ist die Teilnehmerzahl keine Zweierpotenz, erhalten die
besten Seeds automatisch ein Freilos in Runde 1 - ergibt sich direkt aus
der Seed-Reihenfolge, ohne Sonderfall-Logik.

Der Baum wird links-nach-rechts rundenweise dargestellt (Achtelfinale,
Viertelfinale, Halbfinale, Finale, ...). Ein Klick auf eine spielbare
Paarung öffnet den normalen Ergebnis-Dialog (inkl. Sätzen) mit fest
vorgegebenen (nicht änderbaren) Spielern/Teams; das Ergebnis wird wie jedes
andere Spiel in `matches.xml`/`doubles.xml` gespeichert (nur zusätzlich mit
`TournamentId` markiert) und zählt daher ganz normal für Elo, Statistiken
und Badges. Der Sieger rückt automatisch in die nächste Runde vor
(`BracketService.AdvanceWinner`). Nach dem Finalsieg erscheint die
Gewinn-Overlay in einer grösseren "Turniersieger"-Aufmachung (Pokal-Symbol +
Konfetti + Trash-Talk-Spruch); der/die Turniersieger erhalten das neue Badge
"Turniersieger".

Ein laufendes Turnier kann jederzeit mit Sicherheitsabfrage abgebrochen
werden (`AbortTournamentCommand`) - bereits gespielte Partien bleiben
unverändert in der Statistik erhalten. Abgeschlossene und abgebrochene
Turniere bleiben über "Anzeigen" in der Turnier-Historie mit ihrem
vollständigen Baum einsehbar.

### Streak-Alarm

Neue Dashboard-Karte (`StreakAlarmService`): zeigt alle Spieler mit einer
aktuell laufenden Siegserie von mindestens 3 Spielen, absteigend nach
Serienlänge sortiert. Die Serie wird rein chronologisch nach `PlayedAt`
rückwärts ab dem jeweils letzten Spiel des Spielers gezählt - Einzel- **und**
Doppel-Spiele zählen gemeinsam zu derselben Serie (eine Doppel-Niederlage
bricht eine Einzel-Siegserie genauso wie umgekehrt). Die erste Niederlage
(egal welcher Art) beendet die Serie. Ab 5 Siegen in Folge wird die Zeile
zusätzlich mit einem Flammen-Symbol hervorgehoben. Ist niemand aktuell auf
einer Serie, wird die Karte komplett ausgeblendet statt leer angezeigt. Die
Berechnung ist bewusst unabhängig vom Dashboard-Zeitraumfilter (wie "Player
of the Week"/"Rivalität des Monats") - eine durch einen Zeitfilter
abgeschnittene Serie wäre irreführend.

### Sound-Effekte

Kurze WAV-Sounds (`ISoundService`/`WpfSoundService`, abgespielt über
`System.Windows.Media.MediaPlayer` - keine externe Bibliothek) bei drei
Ereignissen: Sieg-Overlay/Konfetti (`win.wav`), Turniersieg (`tournament-
win.wav`, eigener, länger gedachter Sound) und einem neu verdienten Badge
(`badge-earned.wav`, kurzer "Ding"). Abspielen blockiert nie die Oberfläche
(`MediaPlayer.Play()` ist asynchron).

Die eigentlichen Audiodateien werden **nicht** generiert oder aus dem
Internet heruntergeladen - das war für diese Phase explizit ausgeschlossen.
Stattdessen liegen unter `desktop/assets/sounds/` drei 0-Byte-Platzhalter mit
den exakt richtigen Dateinamen; die genauen Anforderungen (Format, empfohlene
Länge) stehen in `desktop/assets/sounds/README.md`. Eine fehlende oder nicht
abspielbare Datei führt zu keinem Ton und **nie** zu einem Fehler/Absturz.
Diese Dateien werden beim Build nach `sounds\` neben die EXE kopiert
(`AppContext.BaseDirectory\sounds\*.wav`), unabhängig vom Datenpfad.

Einstellung **"Sound-Effekte aktivieren"** (Default: an) plus ein
Lautstärkeregler (0-100 %, Default 70 %) unter Einstellungen. Ein neu
verdientes Badge wird erkannt, indem beim Start einer Sitzung einmal alle
aktuell gehaltenen Badges als "bekannt" vorgemerkt werden; taucht danach (nach
einem gespeicherten Spiel) ein Badge auf, das vorher nicht bekannt war, spielt
der "Ding"-Sound genau einmal - das ist reine Sitzungs-Buchführung in
`MainViewModel`, keine neue Kennzahl, und deshalb bewusst ohne eigene
Unit-Tests (anders als die reinen Berechnungs-Services).

### Wettbüro (Tippspiel)

**Reine Punkte-Wette - es geht nie um echtes Geld oder Guthaben.** Auf der
neuen Seite **Tippspiel** (und direkt auf einer spielbaren Turnier-Paarung
über den neuen "Tippen"-Button) können anstehende Partien angekündigt werden,
bevor sie gespielt sind - Einzel oder Doppel, mit oder ohne Turnier-Bezug.
Eingeloggte Spieler tippen den Sieger; ein Tipp pro Spieler pro Partie, bis
zur Ergebniserfassung beliebig änderbar. Auf eine Partie, an der man selbst
beteiligt ist, kann nicht getippt werden.

Sobald das Ergebnis erfasst wird (auf der Turnier-Bracket-Seite wie gehabt,
oder für freundschaftliche Partien über "Ergebnis erfassen" auf der
Tippspiel-Seite - beides öffnet den normalen, gesperrten Ergebnis-Dialog),
werden alle Tipps automatisch aufgelöst: richtiger Tipp = 1 Punkt; war der
getippte Sieger laut Elo-Prognose ein Underdog (unter 40 % Siegwahrscheinlichkeit),
gibt es 3 Punkte statt 1 (`BettingService`, Konstanten `PointsForCorrectPick`/
`PointsForUnderdogPick`/`UnderdogProbabilityThreshold`). Falscher Tipp = 0
Punkte. Die Prognose wird - wie beim bestehenden Gewinn-Overlay - aus den
Elo-Ratings **vor** dieser einen Partie berechnet, nicht danach.

Die Tipp-Rangliste (eigene Karte auf der Tippspiel-Seite) zeigt Punkte,
Trefferquote und Anzahl Tipps pro Spieler. Das neue Badge "Hellseher" geht an
den Führenden dieser Rangliste, ausgewertet nur über Tipps, deren Partie
innerhalb der aktuell aktiven Liga-Saison gespielt wurde (keine aktive
Saison oder niemand mit Punkten = kein Träger dieses Badges).

### Hall of Fame

Neue Seite **Hall of Fame** in zwei Bereichen:

**Abgeschlossene Saisons/Turniere**: eine Saison gilt als abgeschlossen,
sobald ihr Enddatum in der Vergangenheit liegt (unabhängig vom `IsActive`-
Flag) - pro Saison werden Sieger Einzel und Doppel (jeweils die Nr. 1 der
bestehenden Liga-Tabelle - Doppel bleibt wie auf der Liga-Seite eine
Einzelspieler-Wertung, kein Team-Konstrukt), das Einzel-Podest (Top 3) mit
Avataren, Zeitraum und Gesamtspielzahl gezeigt. Dazu alle abgeschlossenen
Turniere mit ihrem Sieger.

**Rekord-Tafel (all-time)**, jeweils mit Avatar, Wert und Datum
(`HallOfFameService`):

- Längste Siegserie, höchstes je erreichtes Elo und beste Siegquote
  (min. 20 Spiele) sind **Einzel-only** - dieselbe Definition, die auch auf
  Mein Profil/im Dashboard bereits pro Spieler gezeigt wird (Phase 13s
  Streak-Alarm kombiniert Einzel+Doppel bewusst nur für die *aktuell
  laufende* Serie auf dem Dashboard, nicht für diesen All-Time-Rekord, um
  keine zwei unterschiedlichen Zahlen für "Siegserie" im selben Programm zu
  zeigen).
- Meiste Spiele an einem Tag und meiste Spiele gesamt zählen Einzel und
  Doppel zusammen.
- Grösster Comeback verwendet unverändert `ComebackService.FindBestComeback`
  (inkl. dessen eigenem, bereits bestehenden Tiebreak).
- Grösste Elo-Überraschung (Einzel-only) sucht den Sieg mit der niedrigsten
  Vorab-Siegwahrscheinlichkeit laut Elo-Prognose (Rating jeweils *vor* der
  betreffenden Partie berechnet).

Kein Rekord vorhanden (z. B. noch niemand mit 20+ Spielen): dezenter
Platzhalter-Text statt eines erfundenen Werts. Bei einem echten Gleichstand
im Wert gewinnt durchgängig der ältere Eintrag (wer den Wert zuerst erreicht
hat) - ausser bei "Grösster Comeback", wo bewusst der bereits bestehende
Tiebreak dieser Funktion erhalten bleibt.

### Migration alter Daten

Bestehende `players.xml`/`matches.xml`/`doubles.xml` ohne die neuen Felder
(`AvatarFileName`, `PinHash`, `PinSalt`, `SetResults`) laden weiterhin ohne
Fehler - fehlende Felder werden als leerer String bzw. leere Liste
interpretiert (nie als geraten/geschätzt), siehe die Migrationstests in
`XmlRepositoryTests.cs`. Kein manueller Migrationsschritt nötig.

## Design-System (Phasen D1–D4)

Die komplette visuelle Gestaltung folgt verbindlich `desktop/design/mockup.html`
(ein statisches HTML/CSS-Mockup, kein Teil der Anwendung selbst). Abweichungen,
bei denen eine Mockup-Eigenschaft in WPF nicht 1:1 umsetzbar war, stehen
ausführlich in `desktop/design/ABWEICHUNGEN.md`.

### Design-Tokens (`Themes/DesignTokens.xaml`)

Alle Farben, Fonts, die Typo-Skala, Eck-Radien und Abstände sind exakt aus dem
Mockup übernommene Werte, als WPF-Ressourcen (`Brush.*`, `Font.*`, `Typo.*`,
`Radius.*`, `Spacing.*`). Bestehende `Brush.*`-Aliasnamen (`Brush.Background`,
`Brush.Accent`, ...) bleiben erhalten und zeigen jetzt auf die neuen
Mockup-Farben, damit auch nicht individuell überarbeitete Views automatisch die
neue Palette erhalten.

### Schriften

Big Shoulders Display (Überschriften/Kennzahlen), Space Grotesk (Fliesstext)
und JetBrains Mono (Zahlen/Eyebrow-Labels) - Google Fonts, SIL Open Font
License. Die Dateien selbst sind **nicht** im Repo (nicht heruntergeladen);
`desktop/src/PingPongStats.App/Assets/Fonts/README.md` dokumentiert exakt,
welche Dateien mit welchem Namen dort abzulegen sind. Ohne diese Dateien
läuft die App normal weiter und fällt automatisch auf Segoe UI zurück (WPFs
eingebauter Fallback über eine kommagetrennte `FontFamily`-Liste) - kein
Absturz.

### Control-Styles (`Themes/Controls.xaml`)

Überschreibt WPFs Standard-Optik für Window, Button (Primary/Ghost/Nav),
TextBox, ComboBox (inkl. eines vollständigen `ControlTemplate` fürs
Dropdown-Popup - siehe unten), CheckBox/RadioButton, DataGrid/DataGridCell,
ListView, ScrollBar (schmal, ohne Pfeil-Buttons) und ToolTip. Kein
Standard-Chrome (graue Buttons, weisse Popups, Systemblau bei Selektion)
bleibt sichtbar. Fenster bekommen zusätzlich eine dunkel eingefärbte native
Titelleiste (`DarkTitleBar.cs`, siehe `ABWEICHUNGEN.md` für die Begründung
gegen eine komplett eigene Titelleiste).

Zwei konkrete Lesbarkeits-Bugs aus der alten (nur per Property-Setter
gestylten) Optik sind damit behoben: aufgeklappte ComboBox-Listen und
ToolTips waren weiss auf weiss/hell nicht lesbar - beide haben jetzt einen
expliziten dunklen Hintergrund (`Brush.Panel2`) statt der WPF-Systemfarbe.

Der bestehende Hell/Dunkel-Umschalter wurde vollständig entfernt (nicht nur
stillgelegt) - die App zeigt jetzt ausschliesslich die Mockup-Palette.

### Bausteine

Wiederverwendbare Controls (`Controls/`): `AvatarControl` (Grössen S/M/L/XL,
deterministische Farbe aus der Spieler-Id), `CardControl` (Panel-Fläche mit
1px-Rahmen und der Signatur-"Tischmittellinie" als 2px-Verlaufslinie am
unteren Rand), `EyebrowLabel`, `Pill`, `StatTile`, `BarRow`, `RowItem`. Jede
Ansicht verwendet ausschliesslich diese Bausteine statt handgemalter
Ein-Weg-Layouts.

### Reduced Motion

Alle Animationen (Konfetti/Ping-Pong-Bälle im Sieg-Overlay, Übergänge) prüfen
die bestehende Einstellung und lassen sich global abschalten - siehe
Settings.

## Backup-Konzept

Vor **jedem** Schreibvorgang wird die bestehende Datei nach
`<DataPath>\backups\` kopiert, z. B. `players_20260710_143000.xml`. Es wird
nichts automatisch aufgeräumt - alte Backups bleiben erhalten, bis sie manuell
gelöscht werden (Ordner ist über **Einstellungen → Backup-Ordner öffnen**
direkt erreichbar).

## Mehrbenutzer-/File-Locking-Konzept

Da mehrere Benutzer gleichzeitig auf demselben Netzwerkpfad arbeiten können,
implementiert `AtomicXmlFileStore<T>` (`Repositories/AtomicXmlFileStore.cs`)
folgenden Ablauf für **jeden** Schreibvorgang:

1. **Exklusive Sperre** anfordern: eine `*.lock`-Marker-Datei wird per
   `FileMode.CreateNew` + `FileShare.None` erstellt - das ist auch auf
   SMB-Netzlaufwerken atomar, nur ein Prozess kann diesen Wettlauf gewinnen.
   Ist die Datei bereits gesperrt, wird bis zu 10 Sekunden lang alle 300 ms
   erneut versucht; danach erscheint eine verständliche Fehlermeldung
   ("Die Datei wird gerade von einem anderen Benutzer bearbeitet...").
   Sperren, die älter als 60 Sekunden sind (z. B. nach einem Absturz), werden
   automatisch als verwaist erkannt und entfernt.
2. Innerhalb der Sperre: **aktuelle Datei frisch einlesen** (nicht den evtl.
   veralteten In-Memory-Stand verwenden) - das verhindert verlorene Änderungen
   bei zwei gleichzeitigen Bearbeitungen.
3. **Backup** der bestehenden Datei erstellen.
4. Änderungen in eine **temporäre Datei** (`*.xml.tmp`) schreiben.
5. Temporäre Datei **validieren** (erneut einlesen/deserialisieren); schlägt
   das fehl, wird die temporäre Datei verworfen und die Original-Datei bleibt
   unverändert - eine klare Fehlermeldung wird angezeigt.
6. Temporäre Datei **atomar** über die Original-Datei legen (`File.Replace`,
   mit Fallback auf Delete+Move falls das Dateisystem `File.Replace` nicht
   unterstützt).
7. Sperre freigeben (Marker-Datei löschen).

**Lesezugriffe** (`GetAll()`) öffnen die Datei mit `FileShare.ReadWrite` und
benötigen keine Sperre - beliebig viele Benutzer können gleichzeitig lesen,
auch während ein anderer Benutzer gerade schreibt (der schreibende Prozess
arbeitet bis zum letzten Schritt ausschliesslich auf der `.tmp`-Datei).

**Bekannte Einschränkung:** Die Sperre serialisiert den kompletten
Lese-Ändern-Schreiben-Zyklus pro Datei, verhindert also Datenverlust
zuverlässig. Sie bietet aber keine feingranulare Konfliktauflösung auf
Feld-Ebene (z. B. wenn zwei Benutzer im selben Moment unterschiedliche Felder
desselben Spiels ändern, gewinnt der zweite Schreibvorgang vollständig). Für
den Einsatz im Rahmen eines Büro-Tischtennis-Tools mit gelegentlichen,
kurzen Schreibzugriffen ist das ein angemessener Kompromiss.

## Statistikdefinitionen

Implementiert zentral in `PingPongStats.Core/Services/StatsService.cs` und
`EloService.cs` (keine duplizierte Logik in ViewModels oder Views):

- **Siegquote overall** = Siege / gespielte Spiele × 100
- **Siegquote letzter Monat** = wie oben, aber nur Spiele der letzten 30 Tage
  relativ zu einem Referenzdatum (Default: jetzt)
- **Aktuelle Siegesserie** = aufeinanderfolgende Siege (oder Niederlagen),
  rückwärts gezählt ab dem aktuellsten Spiel des Spielers, bis das Ergebnis
  wechselt
- **Längste Siegesserie** = längste zusammenhängende Folge von Siegen in der
  gesamten Historie eines Spielers
- **Elo-Rating**: Start 1000, K-Faktor 32, Standardformel, Spiele
  chronologisch verarbeitet
- **Recent Form**: die letzten 5 Ergebnisse eines Spielers, neuestes zuerst
  (z. B. `W W L W L`)
- **Head-to-Head**: Spiele, Siege und Siegquote zweier Spieler gegeneinander,
  unabhängig davon, wer auf welcher Seite (A/B) stand
- **Team-Konstellation (Doppel)**: Siege/Niederlagen/Siegquote pro
  unbenannter Zweier-Paarung, aggregiert unabhängig von Team-Seite und
  Spielerreihenfolge (`DoublesStatsService.GetPairingRankings`)
- Spieler bzw. Team-Konstellationen mit weniger als 3 Spielen werden in der UI
  mit einem Hinweis markiert (`LowSampleSize` / `IsLowSampleSize`)
- **Angstgegner/Lieblingsgegner** (nur Einzel) = Gegner mit der schlechtesten
  bzw. besten persönlichen Siegquote, ab mindestens 3 gemeinsamen Spielen;
  Tiebreak: mehr gemeinsame Spiele (`StatsService.GetNemesis`/`GetFavoriteOpponent`)
- **Player of the Week** (letzte 7 Tage, Einzel + Doppel) = (Siege × 2) −
  Niederlagen + Satzdifferenz × 0,5, ab mindestens 3 Spielen im Zeitraum;
  Tiebreak: Siegquote, dann Spiele, dann Elo (`PlayerOfTheWeekService.Compute`)
- **Bestes Comeback** (nur Einzel, benötigt Satzdetail) = maximaler
  Satzrückstand (≥ 2), den der Sieger im Spielverlauf aufgeholt hat;
  Tiebreak: knapperer Endstand (`ComebackService.FindBestComeback`)

Gewinnerberechnung und Validierung (`ValidationService.cs`): Spieler A/B
dürfen nicht identisch sein, beide müssen existieren, Sätze dürfen nicht
negativ sein, kein Unentschieden erlaubt. Der Gewinner wird ausschliesslich
serverseitig (in `ValidationService.ComputeWinnerId`) aus den Satzwerten
abgeleitet, nie direkt vom UI gesetzt.

## Getroffene Annahmen (Defaults)

- **Tippspiel-Datenmodell**: Die Aufgabenstellung nannte für `Bet` nur
  `MatchId` (nullable bis Spiel erfasst). Um zu wissen, *auf welche* noch
  ungespielte Paarung überhaupt getippt wird (nötig für den Selbst-Tipp-Block
  und die Anzeige), gibt es zusätzlich ein eigenes `PendingMatch`-Modell
  ("angekündigte Partie") mit eigener `PendingMatchId` auf dem Bet; `MatchId`/
  `DoubleMatchId` bleiben wie beschrieben null, bis das echte Ergebnis erfasst
  ist. Eine reine Strukturentscheidung zur Umsetzung, keine erfundene
  Kennzahl.
- **Aktive-Spieler-Pflicht bei neuen Spielen**: Die Spielerauswahl bei
  "Neues Spiel"/"Spiel bearbeiten" zeigt nur aktive Spieler (plus die beiden
  Spieler eines gerade bearbeiteten Bestandsspiels, auch wenn diese seither
  archiviert wurden). Die Spezifikation sagt "sollten aktiv sein" (nicht
  "müssen") - das wird als weiche Vorgabe interpretiert, hart durchgesetzt
  nur bei der Auswahl für *neue* Einträge.
- **Spieler löschen**: Nur möglich, wenn keine Spiele existieren, die auf den
  Spieler verweisen (siehe `PingPongDataService.DeletePlayer`); sonst
  konsistente Fehlermeldung mit Hinweis auf "stattdessen deaktivieren".
- **Statistik-Charts**: Auf handgebaute WPF-Balkendiagramme (Siege pro
  Spieler, Siegquote pro Spieler, Spiele pro Woche/Monat, Siegquote pro
  Doppel-Konstellation) statt einer externen Chart-Bibliothek reduziert -
  Elo, Serien und Recent Form werden zusätzlich in der Ranking-Tabelle des
  Dashboards dargestellt (Risikominimierung fürs Single-File-Publish).
- **Zeitraum-Filter im Dashboard**: Feste Presets (letzte 7/30/90 Tage,
  gesamter Zeitraum) statt eines frei wählbaren Datumsbereichs - deckt den
  typischen Anwendungsfall ab und reduziert UI-Komplexität. Die Filterung
  wirkt auf **alle** Kennzahlen inkl. Elo-Rating (das Elo-Rating im gefilterten
  Zeitraum wird ab Fensteranfang neu von 1000 berechnet, nicht als Fortsetzung
  der Gesamt-Historie - ein bewusster Kompromiss für Konsistenz "alles bezieht
  sich auf den gewählten Zeitraum").
- **Dashboard-Kachel "Siegquote 30 Tage" entfernt**: War eine feste
  30-Tage-Kennzahl unabhängig vom Zeitraum-Filter - seit dieser den
  gleichen Zeitraum bereits abdeckt (inkl. der Voreinstellung "30T"), war
  die Kachel redundant. Die pro Spieler feste 30-Tage-Siegquote in der
  Ranking-Tabelle ("Siegquote 30T"-Spalte) bleibt unverändert bestehen -
  das ist eine andere, unabhängig vom Zeitraum-Filter immer gleich
  berechnete Kennzahl.
- **UI-Grösse**: Drei Stufen (Klein/Mittel/Gross), persistiert pro Benutzer.
  Skaliert per `LayoutTransform` den gesamten Fensterinhalt gleichmässig -
  einfacher und robuster als jede Style-Grösse einzeln zu parametrisieren.
- **Debug-/Seed-Funktion**: Sichtbar nur in Debug-Builds
  (`SettingsViewModel.IsSeedDataAvailable`), im veröffentlichten
  Release-EXE nicht vorhanden. Überschreibt beim Ausführen alle bestehenden
  Daten am aktuellen Datenpfad (mit vorherigem automatischem Backup) - der
  Benutzer muss das in einem Bestätigungsdialog explizit bestätigen.
- **Audit-Log**: Rudimentär (Zeitstempel, Windows-Benutzername, Aktion,
  Details), Best-Effort - ein Fehler beim Schreiben des Audit-Logs blockiert
  nie die eigentliche Datenänderung.
- **CSV-Export**: Semikolon-getrennt, UTF-8 ohne BOM (Excel-kompatibel in
  deutschsprachigen Gebietsschemata), Export-Dateien landen unter
  `<DataPath>\exports\`.

## Bekannte Einschränkungen

- WPF-Teil in dieser Session nicht kompiliert (siehe Hinweis oben) - vor
  Produktiveinsatz einmal auf Windows bauen/testen.
- Keine feingranulare Konfliktauflösung bei simultanen Schreibzugriffen
  (siehe Locking-Konzept oben) - für ein Büro-Tool mit gelegentlichen
  Schreibzugriffen ausreichend, für sehr hohe Parallelität nicht ausgelegt.
- Kein Undo für gelöschte Spieler/Spiele ausser über die automatischen
  Backups (manuelle Wiederherstellung aus `backups\`).
- Keine echte Authentifizierung/Benutzerverwaltung; `Environment.UserName`
  wird nur fürs Audit-Log verwendet. Der optionale Spieler-PIN (siehe
  [Anmeldung & Mein Profil](#anmeldung--mein-profil)) ist ausdrücklich nur
  Bequemlichkeit, keine Zugriffskontrolle - die XML-Dateien bleiben für
  jeden mit Dateizugriff uneingeschränkt lesbar/editierbar.
- Doppel-Spiele können erfasst und gelöscht, aber (anders als Einzel-Spiele)
  nicht nachträglich bearbeitet werden - bei einem Tippfehler: löschen und neu
  erfassen.
