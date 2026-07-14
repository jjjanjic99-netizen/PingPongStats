# Abweichungen vom Mockup

Diese Datei dokumentiert jede Stelle, an der `desktop/design/mockup.html`
nicht 1:1 in WPF umsetzbar war oder eine bewusste Interpretationsentscheidung
nötig war. Reihenfolge: chronologisch nach Phase (D1–D4).

## Phase D1 — Design-Tokens & Fonts

- **Kein Light-Theme**: Der Mockup definiert nur eine einzige, dunkle
  Farbpalette. Die App hatte bereits einen Hell/Dunkel-Umschalter
  (`SettingsViewModel.DarkMode`, `ThemeManager`, `Themes/Light.xaml`/
  `Dark.xaml`). Auf ausdrücklichen Wunsch wurde dieser Schalter komplett
  entfernt (nicht nur deaktiviert) - `AppSettingsModel.DarkMode`,
  `SettingsViewModel.DarkMode`, die zugehörige CheckBox in
  Einstellungen, `ThemeManager` sowie `Themes/Light.xaml`/`Dark.xaml`
  existieren nicht mehr. Die App ist jetzt fest auf die Mockup-Palette
  (`Themes/DesignTokens.xaml`, direkt von `App.xaml` referenziert)
  eingestellt. Eine echte helle Variante war nicht Teil des Auftrags.
- **Schriftdateien nicht heruntergeladen**: siehe
  `Assets/Fonts/README.md`. Die `FontFamily`-Ressourcen sind als
  kommagetrennte Fallback-Listen definiert
  (`pack://application:,,,/Assets/Fonts/#<Familie>, Segoe UI`) - WPFs
  eingebauter Mechanismus, um ohne Absturz auf Segoe UI zurückzufallen,
  wenn die erste Familie am Pack-Pfad nicht gefunden wird. **Muss auf
  einem echten Windows-Rechner geprüft werden** (siehe Checkliste am Ende
  der Session) - dieser Effekt kann in der Linux-Sandbox nicht kompiliert
  oder gerendert werden.
- ~~`letter-spacing` → `TextBlock.CharacterSpacing`~~ **(korrigiert, siehe
  "Build-Fehler behoben" weiter unten)**: Diese Zeile behauptete
  ursprünglich, WPF habe seit .NET Core 3.0 eine `CharacterSpacing`-
  Eigenschaft auf `TextBlock`. Das war falsch - eine Verwechslung mit
  `Windows.UI.Xaml.Controls.TextBlock.CharacterSpacing` aus UWP/WinUI,
  einem anderen Framework. Der reale Windows-Build bestätigte, dass WPF
  gar keine Buchstabenabstands-Eigenschaft kennt (weder auf `TextBlock`
  noch als angehängte `TextElement`-Eigenschaft). Das Mockup-Letter-
  Spacing auf H1/Eyebrow-Labels wird daher nicht nachgebildet; die
  Werte 17/152 aus der ersten Fassung sind entfernt und keine gültige
  WPF-Einheit für irgendetwas.
- **`text-transform:uppercase`**: In WPF gibt es keine deklarative
  Textumwandlung für beliebig gebundenen Text. Wird durch `EyebrowLabel`
  (Phase D3) in C# gelöst (`.ToUpper()` auf den gebundenen String), nicht
  über eine XAML-Style-Eigenschaft.

## Phase D2 — Control-Styles

- **Eigene Fenster-Titelleiste**: Eine vollständig eigene Titelleiste
  (`WindowStyle="None"` + `WindowChrome`, inkl. eigenem Ziehen/
  Minimieren/Maximieren/Schliessen/Aero-Snap) wurde als zu riskant
  eingeschätzt, um sie ungetestet aus dieser Umgebung auszuliefern (kein
  Windows-Rechner zum Kompilieren/Prüfen verfügbar). Stattdessen färbt
  `DarkTitleBar.cs` die **native** Windows-10/11-Titelleiste über die
  dokumentierte DWM-Eigenschaft "Immersive Dark Mode" dunkel ein - der
  Rest des Chroms (Grösse ändern, Minimieren/Maximieren/Schliessen, Aero
  Snap) bleibt Standard-Windows-Verhalten. Auf älteren Windows-Versionen,
  die dieses Attribut nicht kennen, ist der Aufruf ein no-op (kein
  Absturz).
- **ComboBox-Dropdown & ToolTip waren unlesbar (weiss auf weiss)**: WPFs
  Standard-`ComboBox`-Template zeichnet die Popup-Fläche aus einer festen
  Systemfarbe, unabhängig von der `Background`-Eigenschaft - ein reiner
  Property-Setter (wie vor dieser Überarbeitung) ändert nur die
  geschlossene Fläche, nicht das geöffnete Dropdown. `Themes/Controls.xaml`
  überschreibt jetzt das komplette `ControlTemplate` von `ComboBox` (inkl.
  eigenem Popup-`Border` in `Brush.Panel2`) sowie `ComboBoxItem` (Hervorhebung
  in `Brush.Table`). Ebenso gab es zuvor **gar keinen** `ToolTip`-Style - er
  erbte Vordergrundfarben aus dem dunklen Baum auf einem hellen
  System-Tooltip-Hintergrund. Jetzt hat `ToolTip` einen eigenen dunklen
  `Brush.Panel2`-Hintergrund mit `Brush.Line`-Rahmen.
- **DatePicker-Kalender**: Nur das Eingabefeld von `DatePicker` ist neu
  gestyled; das aufklappende Kalender-Popup selbst (Monatsansicht,
  Vor-/Zurück-Pfeile) verwendet weiterhin WPFs Standard-Template. Eine
  vollständige Neugestaltung des Kalender-Popups wäre ein deutlich
  grösserer Aufwand (verschachteltes `CalendarItem`-Template) und war
  nicht Teil dieser Phase - **bitte auf einem Windows-Rechner prüfen**,
  ob das aufgeklappte Kalender-Popup lesbar bleibt.
- **`CardBorder`-Style ohne Tischmittellinie**: Die Signatur-Linie am
  unteren Kartenrand (Phase D3, `CardControl`) lässt sich auf einem
  reinen `Border`-Element (ein `Decorator`, kein `Control` mit eigenem
  `ControlTemplate`) nicht per Style nachrüsten - ein `Border` hat nur
  einen Inhalts-Slot, keinen Platz für ein zweites Deko-Element. Views,
  die in Phase D4 nicht auf die neue `CardControl` migriert werden
  (Spieler, Spiele, Head-to-Head, Einstellungen, Anmeldung), behalten
  daher die flachere `CardBorder`-Optik ohne diese Linie.

## Phase D3 — Wiederverwendbare Bausteine (UserControls)

- **`RowItem` deckt nur einfache Listen-Zeilen ab**: Der Baustein bildet
  das Mockup-`.row`-Muster nach (Position, kleiner Avatar, Name, optionaler
  Trailing-Inhalt wie eine `Pill`, mono Wert rechts, dünner Trenner unten) -
  passend für Elo-Rangliste, Hall-of-Fame-Listen, Bilanz-Countdown u.ä.
  Zeilen mit mehreren eigenständigen Zahlenspalten (die Liga-Tabelle mit
  Siege/Niederlagen/Sätzen/Punkten nebeneinander) werden in Phase D4
  weiterhin direkt von Hand als eigenes Grid gebaut, statt sie gewaltsam
  in `RowItem`s `TrailingContent`-Slot zu pressen - das würde die
  Spaltenausrichtung über mehrere Zeilen hinweg unnötig verkomplizieren.
- **Avatar-Ring bei Grösse L/XL**: Das Mockup zeichnet den farbigen Ring
  um grosse Avatare als CSS-`box-shadow`/`inset border` (liegt "in" der
  Kreisfläche, verkleinert das sichtbare Bild leicht). WPF's `Ellipse.Stroke`
  zentriert die Linie stattdessen auf dem geometrischen Rand (halb innen,
  halb aussen) - eine minimale, rein optische Annäherung ohne
  Funktionsunterschied. Falls der Ring auf einem Windows-Rechner sichtbar
  dicker/dünner wirkt als im Mockup, lässt sich das bei Bedarf über eine
  minimal grössere/kleinere `diameter` je Grösse feinjustieren.
- **Avatar-Fallback-Palette**: `AvatarService.FallbackColors` wurde von den
  ursprünglichen generischen Web-Farben auf die acht Beispiel-Avatarfarben
  aus dem Mockup umgestellt. Die Zuordnung Spieler→Farbe (deterministisch
  per MD5-Hash der PlayerId) bleibt unverändert - nur die Farbwerte selbst
  ändern sich, wodurch sich bestehende Zuordnungen zwischen den Farben in
  der Praxis verschieben können. Kein Test prüft konkrete Farbwerte, daher
  unkritisch.
- **Bugfix in `CardControl` (noch vor dem ersten produktiven Einsatz in D4
  gefunden)**: Die ursprüngliche Fassung liess den inneren
  `ContentPresenter` auf die von `UserControl` geerbte `Content`-Eigenschaft
  zurückbinden - dieselbe Eigenschaft, über die `CardControl.xaml`s eigener
  Wurzelknoten (der Rahmen mit der Tischlinie) implizit gesetzt wird. Jede
  Verwendung mit eigenem Inhalt (z. B. `StatTile`, das ein `StackPanel` in
  eine `CardControl` einbettet) hätte dadurch den gesamten Rahmen/die Linie
  stillschweigend überschrieben und durch den nackten Inhalt ohne jede
  Dekoration ersetzt. Behoben durch eine eigene `CardContent`-Property
  (plus `[ContentProperty(nameof(CardContent))]`), sodass die bestehende
  Verwendungssyntax (`&lt;controls:CardControl&gt;...&lt;/controls:CardControl&gt;`)
  unverändert bleibt, aber jetzt korrekt in einen separaten Slot fliesst statt
  mit der eigenen Rahmen-Definition zu kollidieren.

## Phase D4 — Shell (Rail + Topbar)

- **Zusätzliche Nav-Einträge**: Der Mockup zeigt nur sechs Seiten
  (Dashboard, Mein Profil, Liga, Turnier, Hall of Fame, Tippspiel) - er ist
  eine Demo, kein vollständiges Abbild der App. Spieler, Spiele, Doppel,
  Head-to-Head und Einstellungen existieren real und müssen erreichbar
  bleiben; sie stehen als zweite, mit "Verwaltung" (`EyebrowLabel`)
  abgetrennte Gruppe unterhalb der Mockup-Navigation, optisch gleich
  gestylt aber ohne eigenes Icon-Glyph (der Mockup definiert dafür keine
  Symbole).
  Aktiv-Zustand jeder Nav-Schaltfläche (Table-Füllung, weisser Text) wird
  über einen `DataTrigger` auf `ActiveSection` (bereits vorhandene
  Property auf `MainViewModel`, wird bei jeder `Navigate(...)` gesetzt)
  gesetzt statt über einen zusätzlichen Enum/Converter.
- **Icon-Glyphe der Mockup-Navigation**: Die Unicode-Symbole aus dem
  Mockup (◧ ◉ ▤ ⑂ ★ ◆) werden 1:1 übernommen. Ob alle auf einem
  Windows-System mit der Body-Schrift-Fallback-Kette sauber rendern
  (insbesondere ⑂, ein selteneres CJK-Zeichen), lässt sich von hier aus
  nicht prüfen - **bitte auf dem Windows-Rechner sichtprüfen**; im
  Zweifel ist ein Ersatz-Glyph eine rein kosmetische Änderung ohne
  Funktionsauswirkung.
- **Rail-Fuss ohne Elo-Trendpfeil**: Der Mockup zeigt "Elo 1287 ▲" im
  `.me`-Block. Ein Trendpfeil braucht eine Vergleichsbasis (z. B. "seit
  wann"), die für den eingeloggten Spieler an dieser Stelle nicht ohne
  Weiteres verfügbar ist (anders als in der Rangliste, wo die aktuelle
  Sieg-/Verlustserie als Näherung dient - siehe Dashboard-Abschnitt
  unten). Der Rail-Fuss zeigt daher nur `Elo {gerundeter Wert}` ohne
  Pfeil - keine erfundene Kennzahl.
- **Topbar-Zeitraum-Filter wirkt nicht auf jede Seite**: Das gemeinsame
  `RangeFilter` (bereits aus Task #16 vorhanden) steuert weiterhin nur
  Dashboard und Doppel-Dashboard; auf allen anderen Seiten ist die
  Segment-Auswahl in der Topbar sichtbar, aber ohne Wirkung. Das
  entspricht dem Mockup (dort ist der Filter rein dekorativ/global ohne
  Seitenbezug) und ändert an bestehender Funktionalität nichts.
- **"Nicht angemeldet"-Hinweis entfernt**: Die alte Kopfzeile zeigte einen
  Text-Hinweis "Nicht angemeldet", wenn kein Spieler eingeloggt war. Der
  neue Rail-Fuss zeigt in diesem Fall stattdessen direkt einen
  "Anmelden"-Button (wie im Mockup), wodurch der separate Hinweistext
  überflüssig wird.
- **CTA "+ Spiel erfassen" öffnet weiterhin nur den Einzel-Dialog**: Der
  Mockup-Dialog hat ein "Modus"-Dropdown (Einzel/Doppel) in einem
  gemeinsamen Formular. Die App trennt Einzel- und Doppel-Erfassung
  strukturell (eigene ViewModels/Navigation, siehe "Doppel" in der
  Verwaltungs-Gruppe) - das zusammenzulegen wäre eine funktionale
  Änderung der Navigation, nicht nur eine optische, und bleibt daher
  bewusst ausserhalb dieser rein visuellen Überarbeitung. Beide Dialoge
  werden in der Erfassungs-Dialog-Phase optisch an den Mockup angeglichen,
  bleiben aber als zwei getrennte Flows bestehen.

## Phase D4 — Dashboard

- **"Match of the Day" nicht übernommen**: Der Mockup zeigt in der zweiten
  Kartenreihe ein Beispiel "knappste Partie heute" neben "Rivalität des
  Monats". Diese Kennzahl existiert nicht in der App (kein Service dafür)
  und wurde gemäss Vorgabe ("keine erfundenen Daten") nicht ergänzt.
  "Rivalität des Monats" steht daher allein, über die volle Breite, statt
  neben einer erfundenen zweiten Karte.
- **Bestes Comeback: nur ein echter Avatar**: Nur der Sieger ist als
  `Player`-Objekt verfügbar (`ComebackWinner`); der Gegnername
  (`ComebackOpponentName`) ist reiner Text ohne Spieler-Referenz. Der
  zweite Avatar-Platz bleibt daher bewusst ohne `Player` gebunden, was
  `AvatarControl` bereits als neutralen "?"-Kreis darstellt - dieselbe
  Konvention, die der Mockup selbst für unbekannte Turnier-Platzhalter
  verwendet ("Sieger HF 1/2"). Der Mockup zeigt hier zusätzlich eine
  kompakte "3:2"-Anzeige; ein solcher verdichteter Score ist in den
  vorhandenen Comeback-Daten nicht als eigenes Feld vorhanden (nur die
  volle Satzfolge und der "aufgeholte Rückstand"), daher werden beide
  vorhandenen Texte gezeigt statt eine neue Kennzahl zu berechnen.
- **Elo-Trendpfeil (▲/▼) aus vorhandenen Daten abgeleitet**: Der Mockup
  zeigt neben jedem Elo-Wert einen Trendpfeil. Es gibt keine gespeicherte
  Elo-Historie-Differenz pro Rangliste-Zeile; der Pfeil wird daher aus der
  bereits vorhandenen aktuellen Sieg-/Verlustserie abgeleitet (Sieg-Serie
  → ▲/Win-Farbe, Verlust-Serie → ▼/Lose-Farbe, keine Serie → neutral) -
  eine rein visuelle Ableitung aus bestehenden Daten (zwei neue
  App-Converter, keine neue Core-Logik), keine neue/erfundene Kennzahl.
- **"👤 du"-Badge nicht übernommen**: Der Mockup markiert die eigene Zeile
  in der Rangliste mit einem "du"-Pill. Das würde erfordern, dem
  Dashboard-ViewModel den eingeloggten Spieler durchzureichen (aktuell nur
  in `MainViewModel` bekannt) - eine kleine funktionale Verdrahtung, keine
  rein optische Änderung, daher ausserhalb dieser Phase belassen. Die
  Pill-Spalte zeigt stattdessen weiterhin die vorhandenen Abzeichen-Icons.
- **"Aktualisieren"-Button entfernt**: Die alte Kopfzeile hatte einen
  expliziten Refresh-Button. Der Mockup kennt keinen solchen Button; da
  jede Navigation zum Dashboard ohnehin `Load()` erneut aufruft (siehe
  `MainViewModel.Navigate`), ist ein manueller Zwischen-Refresh selten
  nötig. `DashboardViewModel.RefreshCommand` bleibt im ViewModel bestehen
  (aktuell ungenutzt), falls später wieder eine UI dafür gebraucht wird.
- **Detail-Tabelle statt gestrichener Spalten**: Die App hat mehr
  Pro-Spieler-Kennzahlen (Spiele, S/N, Siegquote, Siegquote 30T, Serie,
  Form) als der Mockup zeigt. Diese stehen weiterhin in einer zweiten,
  ausführlicheren `DataGrid`-Tabelle ("Ranking-Details") unterhalb der
  Mockup-genauen Elo-Rangliste, statt entfernt zu werden.
- **Bugfix: `AncestorType=UserControl`-Bindungen brechen jetzt, wo Inhalte
  in `CardControl` verschachtelt sind**: Mehrere bestehende Bindungen
  suchten den nächsten `UserControl`-Vorfahren, um von dort
  `DataContext.DataPath` zu lesen (z. B. für `AvatarControl.DataPath` in
  einer `ItemsControl`/`DataGrid`-Zelle). Seit `CardControl` (und
  `RowItem`) selbst `UserControl`s sind, liefert `AncestorType=UserControl`
  jetzt oft die falsche, nähere Karte/Zeile statt der Seite selbst - der
  Pfad `DataPath` existiert dort nicht, die Bindung schlägt still fehl und
  Avatare zeigen dauerhaft nur Initialen statt eines evtl. vorhandenen
  Fotos. Behoben in `DashboardView.xaml` durch ein `x:Name="Root"` auf dem
  Seiten-`UserControl` und `ElementName=Root` statt `AncestorType`. Andere
  Views mit demselben alten Muster (`BettingView`, `DoublesView`,
  `HallOfFameView`, `LeagueView`, `LoginView`, `MatchEditView`,
  `PlayersView`, `SettingsView`, `TournamentView`) werden bei ihrer
  eigenen D4-Überarbeitung auf dasselbe `ElementName`-Muster umgestellt,
  sobald/falls sie Inhalte in `CardControl` verschachteln.

## Phase D4 — Profil

- **Zwei kleine, ehrliche ViewModel-Ergänzungen** (keine neue Formel, nur
  bereits vorhandene Berechnungen/Objekte zusätzlich freigegeben):
  - `RankLabel` ("Rang X von Y"): dieselbe Elo-absteigende Sortierung, die
    das Dashboard bereits verwendet, angewendet auf `ProfileViewModel`
    (das bislang nur den eigenen Elo-Wert kannte, nicht die Platzierung).
  - `NemesisPlayer`/`FavoriteOpponentPlayer`: das volle `Player`-Objekt des
    Angst-/Lieblingsgegners war in `Load()` bereits über `playersById`
    verfügbar, nur nicht als Property exponiert - nötig, um den
    Mockup-Avatar neben Name/Bilanz zu zeigen.
- **Abzeichen ohne "gesperrt mit Fortschritt"-Anzeige**: Der Mockup zeigt
  ein Beispiel für ein noch nicht verdientes Abzeichen mit
  Fortschrittszähler ("🔒 Der Unbesiegte · 4/10"). Die Badge-Engine
  kennt nur bereits verdiente Abzeichen, keine Fortschritts-Metrik für
  ungerdiente - daher werden weiterhin nur verdiente Abzeichen gezeigt.
- **Bilanz-Countdown ohne Avatar**: `StatsService` liefert für die
  häufigsten Gegner nur den Anzeigenamen, kein `Player`-Objekt. Die Zeilen
  verwenden daher `RowItem` ohne `AvatarPlayer` (kein Platzhalter-Kreis,
  da hier - anders als beim Comeback/Angstgegner - noch nicht mal eine
  Spieler-Referenz vorliegt, nur ein String).
- **"Aktualisieren"-Button entfernt**: gleiche Begründung wie beim
  Dashboard - jede Navigation zu "Mein Profil" ruft ohnehin `Load()` neu
  auf.
- **7 statt 3 Stat-Kacheln**: Der Mockup zeigt nur Siegquote/Längste
  Serie/Satzdifferenz. Spiele/Siege/Niederlagen/Längste Niederlagenserie
  bleiben in einer zweiten Kachel-Reihe darunter, statt entfernt zu
  werden.

## Phase D4 — Liga

- **Einzel/Doppel als Segment-Toggle statt CheckBox**: `IsDoublesMode` war
  bereits ein einfaches bool auf `LeagueViewModel`. Für den
  mockup-typischen Zwei-Segment-Umschalter wurde ein trivialer
  `SetDoublesModeCommand` (`IRelayCommand<bool>`) ergänzt, der die
  bestehende Eigenschaft direkt setzt - keine neue Logik, nur ein anderer
  UI-Auslöser für dieselbe Eigenschaft.
- **"Aktualisieren"-Button entfernt**: gleiche Begründung wie bei
  Dashboard/Profil.
- Podium und Tabelle sind ansonsten sehr nah am Mockup 1:1 umsetzbar
  gewesen (die App hatte bereits Rank/Player/DisplayName/Played/Wins/
  Losses/SetDifferenceLabel/Points in exakt der vom Mockup benötigten
  Form).

## Phase D4 — Turnier

- **Kein Setup-Formular im Mockup**: Der Mockup zeigt nur ein bereits
  laufendes Turnier, kein Erstellungsformular. Das bestehende
  Setup-Formular (Name, Doppel-Checkbox, Teilnehmerauswahl, Team-Zuteilung)
  bleibt funktional unverändert, nur mit den neuen Bausteinen/Farben
  eingefärbt.
- **Kein Zahlen-Score pro Bracket-Seite**: Der Mockup zeigt pro Partie
  eine Satzzahl (z. B. "3"/"0"). `TournamentSlotRow` speichert nur
  `WinnerEntrantId`/`WinnerLabel`, keinen Satz-Score pro Seite. Statt eine
  neue Zahl zu erfinden, zeigt die gewonnene Partie stattdessen
  "Sieger: {Name}" in Win-Grün unterhalb der beiden Seiten - dieselbe
  Information, nur nicht als Zahlenspalte.
  Ist ein Slot spielbar (`IsPlayable`), erhält die Karte den
  Ball-Rahmen (mockup `.match.live`); ein `?`/kursiv-muted Name für TBD-
  Seiten (mockup `.match.next`) kommt über `EntrantX.IsTbd`.
- **Doppel-Bracket zeigt nur einen Avatar pro Team**: `EntrantX.Players`
  enthält bei Doppel zwei Spieler, `AvatarControl` kann aber nur einen
  Spieler darstellen. Ein neuer `FirstPlayerConverter` (App-Layer, keine
  Core-Änderung) wählt den ersten Spieler des Teams - eine rein optische
  Vereinfachung, der Name-Text zeigt weiterhin beide Namen
  ("Frei & Baumann" via `EntrantX.Label`).

## Phase D4 — Hall of Fame

- **Kein Rekord-Wert als reine Zahl**: `HallOfFameRecord.ValueLabel` ist
  ein vollständiger, beschreibender Satz ("9 Spiele an einem Tag", "Sieg
  mit nur 9% Prognose"), keine separate blosse Zahl wie im Mockup
  ("9", "9%"). Ohne eine neue, separate numerische Eigenschaft zu erfinden
  (oder den bestehenden String fragil zu parsen), wird `ValueLabel`
  weiterhin als vollständiger Text neben dem Spielernamen gezeigt statt
  als grosse Display-Schrift-Zahl.
- **Saison-Sieger-Liste ohne Kurzcode**: Der Mockup zeigt kompakte
  Saison-Codes wie "Q2/26". `Season.Name` ist ein freier Text ohne
  garantierte Kürze und passt nicht in `RowItem`s schmale 20px-Pos-Spalte
  - der Name steht daher zusammen mit dem Sieger-Namen in der
  Hauptspalte, statt in "Pos" abgeschnitten zu werden. Ebenso zeigt die
  Zeile die Gesamt-Spielzahl der Saison statt einer "Punkte"-Zahl, da
  Punkte auf dieser Zusammenfassungsebene nicht vorliegen (nur pro
  Spieler im Podest, siehe "Saison-Details" darunter).
- **Turnier-Sieger ohne Avatar/Saison-Zuordnung**: `TournamentSummaryRow`
  hat keine Spieler-Referenz für den Sieger (nur `WinnerLabel` als Text)
  und keine Zuordnung zu einer Saison (der Mockup zeigt "Frühling"/
  "Winter"/"Herbst" als Pos) - beides nicht vorhanden, daher weggelassen
  statt erfunden.
- **Saison-Details bleibt erhalten**: Die bereits bestehende, reichhaltigere
  Pro-Saison-Karte (Doppel-Sieger, Podest mit Punkten) zeigt mehr als der
  Mockup - wird nicht entfernt, sondern als zusätzlicher Abschnitt unter
  der kompakten Mockup-Liste weitergeführt.
- **Neue, triviale `HasCompletedTournaments`-Eigenschaft**: analog zum
  bereits vorhandenen `HasCompletedSeasons`, für den Leerzustand der
  Turnier-Sieger-Liste - keine neue Logik, nur `CompletedTournaments.Count
  > 0`.

## Phase D4 — Tippspiel

- **Offene Partien ohne Zwei-Avatar-"vs"-Layout**: `PendingMatchRow`
  exponiert nur ein bereits kombiniertes `Label` ("Marco Brunner vs.
  Dario Frei"), keine einzelnen Spieler-Referenzen pro Seite. Der Mockup
  zeigt zwei Avatare links/rechts von einem "vs". Ohne die zugrunde
  liegenden Spieler-IDs zusätzlich durchzureichen (eine strukturelle,
  nicht rein optische Änderung), bleibt die Karte bei der vorhandenen
  einzeiligen Beschriftung.
- **Tipp-Optionen als Segment-Buttons statt Sekundär-/Primär-Style-Swap**:
  Gleiche visuelle Wirkung wie vorher (aktiver Tipp = Ball-Fläche), jetzt
  über `SegmentButton` als Basis, um am mockup-typischen
  "Timo · 1 Pkt"/"Dario · 3 Pkt ⚡"-Look näher dran zu sein (der
  Undogod-Bonus-Hinweistext ist bereits als eigener Satz unterhalb
  vorhanden, siehe bestehenden Beschreibungstext oben auf der Seite).
- **"Aktualisieren"-Button entfernt**: gleiche Begründung wie bei den
  anderen Seiten.

## Phase D4 — Erfassungs-Dialog (MatchEditView / DoublesView)

- **Kein echter modaler Dialog**: Der Mockup zeigt die Spiel-Erfassung als
  `.scrim`/`.dialog` - ein Overlay über der ganzen App. Die App navigiert
  stattdessen zu einer vollflächigen Ersatz-View (`CurrentViewModel`-Swap
  wie jede andere Seite) - das war schon vor dieser Überarbeitung so und
  ist eine strukturelle Navigations-Entscheidung, keine rein optische;
  sie zu ändern (z. B. auf ein echtes `Popup`/eigenes Fenster) wäre eine
  Funktionsänderung und bleibt daher aussen vor. Die Karte selbst
  (Rahmen, Radius 14, `dlg-title`, `.field`-Label-Optik, Prognose-Balken,
  Ghost/Primary-Aktionspaar) ist optisch 1:1 an den Mockup angeglichen.
- **Kein `.card` für den Dialog**: Bewusst kein `CardControl` (keine
  Tischlinie am unteren Rand) für die Dialog-Karte, da der Mockup
  `.dialog` explizit von `.card` unterscheidet (kein `::after` bei
  `.dialog`).
- **Neue `PredictionAFraction`-Eigenschaft** auf `MatchEditViewModel` und
  `DoublesViewModel`: dieselbe Elo-Gewinnwahrscheinlichkeit, die
  `UpdatePrediction()` bereits für die beiden Beschriftungen berechnet,
  zusätzlich als rohe 0..1-Zahl - keine neue Formel, nur zusätzlich
  freigegeben, damit der Prognose-Balken (mockup `.prog-fill`) eine
  Breite hat. Ein neuer `FractionToGridLengthConverter` (App-Layer)
  setzt sie in zwei sternbemessene Grid-Spalten um, nach demselben
  Muster wie `BarRow` (Phase D3).
- **Doppel-Seite bleibt eine echte Navigationsseite**: "Doppel" ist im
  Mockup nicht enthalten (siehe Shell-Abschnitt); nur der eingebettete
  Erfassungs-Abschnitt wurde an den Dialog-Look angeglichen, der Rest der
  Seite (Statistiken, Team-Rangliste, Spielverlauf) bleibt strukturell
  unverändert, nur mit den neuen Bausteinen/Farben.

## Phase D4 — Sieg-Overlay

- **Konfetti-Bälle statt Rechtecke**: `MainWindow.xaml.cs`s `StartConfetti()`
  zeichnete bisher farbige, rotierende Rechtecke. Ersetzt durch `Ellipse`-
  Elemente mit einem `RadialGradientBrush` (Highlight oben-links → Ball-
  Orange), ca. 15% davon rein weiss statt orange (mockup `.pball`) - Fall-
  und Rotations-Animation (`TranslateTransform`/`RotateTransform`,
  gestaffelte Dauer/Verzögerung pro Ball) blieb unverändert, nur Form und
  Farbe geändert.
  Der zweite CSS-Gradient-Stop des Mockups liegt bei 65% statt 100% - für
  eine ~12-22px kleine Kreisfläche ist der Unterschied kaum wahrnehmbar,
  daher der einfachere 2-Stop-`RadialGradientBrush`-Konstruktor statt
  eines expliziten 3-Stop-Gradienten.
- **Reduced-Motion-Einstellung bereits vorhanden**: Die geforderte
  "Animationen müssen abschaltbar sein"-Vorgabe ist bereits durch die
  bestehende `Settings.ShowWinAnimation`-Option erfüllt - sie schaltet
  das gesamte Overlay (inkl. Konfetti) aus, `MainViewModel` prüft sie vor
  jedem Anzeigen. Keine neue Einstellung nötig.
- **XL-Avatar-Ring**: kommt automatisch aus `AvatarControl` (Phase D3,
  4px solider Ball-Rahmen bei Size="XL") - keine zusätzliche Änderung
  hier nötig.
- Kicker/Name/Score/COMEBACK-Badge/Zitat/Hinweistext sind 1:1 nach
  Mockup-Werten (Display 52px Name, Rubber-Badge, italic Zitat,
  Mono-Hinweistext) umgesetzt.

## Build-Fehler behoben (gemeldet vom ersten echten Windows-Build)

Diese Überarbeitung wurde vollständig in einer Linux-Sandbox erstellt, in der
`PingPongStats.App` (WPF) nicht kompiliert werden kann - nur `xmllint` und ein
Ressourcenschlüssel-Abgleich waren möglich. Der erste echte Build in Visual
Studio deckte drei reale Fehler auf, die diese statische Prüfung nicht
erkennen konnte:

1. **`CharacterSpacing` existiert in WPF überhaupt nicht** - weder auf
   `TextBlock` noch (Korrektur eines ersten, ebenfalls falschen
   Reparaturversuchs) als angehängte Eigenschaft von
   `System.Windows.Documents.TextElement`. Das war eine Verwechslung mit
   `Windows.UI.Xaml.Controls.TextBlock.CharacterSpacing` aus UWP/WinUI -
   einem anderen, unverwandten UI-Framework. WPF hat **keinen** eingebauten
   Mechanismus für Buchstabenabstand/Tracking auf `TextBlock`, in keiner
   .NET-Version. Der erste Reparaturversuch (`doc:TextElement.
   CharacterSpacing`) kompilierte deshalb ebenfalls nicht ("Die Eigenschaft
   'CharacterSpacing', die angehängt werden kann, wurde in Typ 'TextElement'
   nicht gefunden"). Endgültig behoben durch vollständiges Entfernen jeder
   `CharacterSpacing`-Verwendung (Attribute und Style-Setter) aus
   `DesignTokens.xaml`, `MainWindow.xaml`, `DashboardView.xaml`,
   `MatchEditView.xaml` und `DoublesView.xaml` - der Mockup-Buchstabenabstand
   auf H1/Eyebrow/Sieg-Overlay-Kicker/COMEBACK-Badge wird nicht nachgebildet,
   die Schriftgrösse/-familie/-farbe bleiben wie spezifiziert. Da
   `DesignTokens.xaml` von praktisch jeder anderen Ressourcen-Datei/View
   transitiv geladen wird, liess dieser eine Fehler dort das gesamte
   Ressourcen-Wörterbuch (und damit fast jede
   `{Static/DynamicResource}`-Auflösung in der ganzen App) fehlschlagen -
   das erklärt die grosse Zahl an "Ressource nicht gefunden"/"Typ nicht
   gefunden"/"Assembly kann nicht geladen werden"-Folgefehlern in beiden
   bisherigen Fehlerlisten.
2. **Doppelt gesetzte `Style`-Eigenschaft** (`MainWindow.xaml`, die vier
   Zeitraum-Segment-Buttons "7T"/"30T"/"90T"/"Alle"): Jeder Button hatte
   sowohl ein `Style="{StaticResource SegmentButton}"`-Attribut als auch
   einen `<Button.Style>`-Block - WPF erlaubt eine Eigenschaft nur einmal
   pro Element. Das Attribut war überflüssig (der `<Button.Style>`-Block
   verwendet ohnehin `BasedOn="{StaticResource SegmentButton}"`) und wurde
   entfernt.
3. **`DataTemplate` mit zwei Wurzel-Elementen** (`HallOfFameView.xaml`,
   Rekord-Tafel-Zeile): Die Rekord-Zeile (`Grid`) und der
   "noch kein Rekord"-Text (`TextBlock`) waren zwei nebeneinanderstehende
   Kind-Elemente desselben `DataTemplate` - ein `DataTemplate` akzeptiert
   aber nur genau ein Wurzelelement. Behoben, indem beide (über
   `Visibility` bereits gegenseitig ausschliessend) in ein gemeinsames
   `Grid` gepackt wurden.

Alle drei Fehler (inkl. der korrigierten `CharacterSpacing`-Entfernung) wurden
nach jeder Runde per statischer Analyse erneut verifiziert (u. a. ein
Python-Skript, das jedes `DataTemplate`/`Border`/`ContentControl` im
gesamten App-Projekt auf mehr als ein echtes Kind-Element prüft, ein
Abgleich aller Elemente mit gleichzeitigem `Style`-Attribut und
`*.Style`-Kind-Element, und ein erneuter Volltext-Grep auf
`CharacterSpacing`/`xmlns:doc`) - keine weiteren Vorkommen gefunden. Ein
tatsächlicher `dotnet build`/Kompilierlauf war in dieser Umgebung weiterhin
nicht möglich; alle übrigen Fehler der zweiten Fehlerliste (u. a. die
"PingPongStats.Core kann nicht geladen werden"/"Typ nicht gefunden"/
"Ressource hat inkompatiblen Typ"-Meldungen) betrafen ausschliesslich
`CharacterSpacing`-Folgefehler in `DesignTokens.xaml` bzw. Dateien, die es
transitiv laden - keine davon hatte eine eigenständige, andere Ursache. Die
nächste Windows-Rückmeldung sollte zeigen, ob damit alle gemeldeten Fehler
behoben sind.
