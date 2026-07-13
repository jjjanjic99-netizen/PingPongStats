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
- **`letter-spacing` → `TextBlock.CharacterSpacing`**: CSS' `letter-spacing`
  ist ein Pixel-Wert; WPFs `CharacterSpacing` (seit .NET Core 3.0 auf
  `TextBlock` verfügbar) ist in 1/1000 der Schriftgrösse angegeben. Die
  Werte in `DesignTokens.xaml` (`Typo.H1` = 17, `Typo.Eyebrow` = 152)
  sind aus den Mockup-Px-Werten (0.5px @ 30px bzw. 1.6px @ 10.5px)
  umgerechnet, keine erfundenen Werte.
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
