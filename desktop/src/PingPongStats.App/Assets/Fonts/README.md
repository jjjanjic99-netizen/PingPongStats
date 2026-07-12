# Schriftarten (Phase D1)

Dieser Ordner ist **absichtlich leer eingecheckt** (nur diese README). Die
eigentlichen Font-Dateien wurden nicht heruntergeladen - das war für diese
Phase explizit ausgeschlossen. Lege die folgenden Dateien selbst hier ab,
damit das Design exakt wie im Mockup (`desktop/design/mockup.html`)
aussieht. Fehlen die Dateien, fällt die App automatisch auf **Segoe UI**
zurück (siehe unten) - kein Crash, nur ein anderer Schriftschnitt.

Quelle: [Google Fonts](https://fonts.google.com/), jeweils unter der
**SIL Open Font License 1.1** lizenziert (frei für den Einsatz hier, auch
kommerziell, Namensnennung nicht erforderlich).

## Benötigte Dateien

| Font-Familie | Verwendung | Google-Fonts-Seite | Dateien (genau diese Namen) |
|---|---|---|---|
| **Big Shoulders Display** | Überschriften, grosse Kennzahlen, Spielername im Hero/Sieg-Overlay | https://fonts.google.com/specimen/Big+Shoulders+Display | `BigShouldersDisplay-Bold.ttf` (700), `BigShouldersDisplay-ExtraBold.ttf` (800) |
| **Space Grotesk** | Fliesstext, Labels, UI-Text | https://fonts.google.com/specimen/Space+Grotesk | `SpaceGrotesk-Regular.ttf` (400), `SpaceGrotesk-Medium.ttf` (500), `SpaceGrotesk-Bold.ttf` (700) |
| **JetBrains Mono** | Zahlen, Scores, Eyebrow-Labels (Mono-Ziffern) | https://fonts.google.com/specimen/JetBrains+Mono | `JetBrainsMono-Regular.ttf` (400), `JetBrainsMono-Bold.ttf` (700) |

Beim Herunterladen von Google Fonts als "static" TTF-Set liefert Google
genau diese Dateinamen (Static-Ordner, nicht die Variable-Font-Variante) -
einfach in diesen Ordner kopieren, sonst nichts weiter nötig.

## Wie die Einbindung technisch funktioniert

Die Dateien werden über `PingPongStats.App.csproj` als eingebettete
Assembly-Ressourcen (`<Resource Include="Assets\Fonts\*.ttf" />`)
kompiliert und in `Themes/DesignTokens.xaml` über
`pack://application:,,,/Assets/Fonts/#<Family Name>` referenziert - exakt
die vom Auftrag geforderte Pack-URI-Syntax, keine Netzwerk-/System-Fonts.

Jede `FontFamily`-Ressource ist als **kommagetrennte Fallback-Liste**
definiert (`"pack://application:,,,/Assets/Fonts/#Big Shoulders Display,
Segoe UI"`) - das ist WPFs eingebauter, dokumentierter Mechanismus für
genau diesen Fall: Findet WPF die erste Familie an diesem Pack-Pfad nicht
(weil die Datei fehlt oder der Ordner leer ist), verwendet es automatisch
die nächste in der Liste, ohne Ausnahme/Absturz. Mit leerem `Assets/Fonts/`
(wie im eingecheckten Zustand) läuft die App also normal, nur eben mit
Segoe UI statt der Mockup-Schriften.
