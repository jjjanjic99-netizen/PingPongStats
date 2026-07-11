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
  vollständig gebaut, alle 123 Unit-Tests laufen grün** (`dotnet test`).
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
Einzel+Doppel-Kombination, alle Tiebreak-Stufen) sowie Bestes Comeback
(Mindest-Rückstand, maximaler Rückstand, Tiebreak, "keine Satzdaten"-Fall).

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
  "DarkMode": false,
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

### Migration alter Daten

Bestehende `players.xml`/`matches.xml`/`doubles.xml` ohne die neuen Felder
(`AvatarFileName`, `PinHash`, `PinSalt`, `SetResults`) laden weiterhin ohne
Fehler - fehlende Felder werden als leerer String bzw. leere Liste
interpretiert (nie als geraten/geschätzt), siehe die Migrationstests in
`XmlRepositoryTests.cs`. Kein manueller Migrationsschritt nötig.

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
