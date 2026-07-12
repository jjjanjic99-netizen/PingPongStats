# Sound-Effekte (Phase 14)

Die drei `.wav`-Dateien in diesem Ordner sind **Platzhalter (0 Byte)** -
absichtlich keine echten Audiodateien. Sie wurden nicht generiert oder aus
dem Internet heruntergeladen; das war für diese Phase explizit
ausgeschlossen. Ersetze sie durch eigene, kurze WAV-Dateien mit **genau
diesen Dateinamen**, damit die App sie findet:

| Datei | Wann sie abgespielt wird | Empfohlene Länge |
|---|---|---|
| `win.wav` | Nach jedem gespeicherten Sieg (Sieg-Overlay/Konfetti), Einzel und Doppel | ca. 1-2 Sekunden |
| `tournament-win.wav` | Wenn ein Turnier mit dem Finalsieg abgeschlossen wird (statt/zusätzlich zu `win.wav`) | ca. 3-5 Sekunden, etwas grösser/feierlicher |
| `badge-earned.wav` | Sobald ein Spieler neu ein Badge verdient | ca. 0,5-1 Sekunde, kurzer "Ding" |

Format: unkomprimiertes PCM-WAV (Mono oder Stereo, 16 Bit, 44.1 kHz reicht),
das ist das einzige Format, das `System.Windows.Media.MediaPlayer` garantiert
ohne zusätzliche Codecs abspielt.

**Fehlt eine Datei (oder ist sie wie hier 0 Byte), spielt die App einfach
keinen Ton ab - kein Fehler, kein Absturz.** Das ist bewusst so gebaut
(`WpfSoundService.PlaySound`), damit dieser Ordner mit Platzhaltern
eingecheckt werden kann, ohne dass irgendjemand ohne echte Sounddateien eine
Fehlermeldung bekommt.

Diese Dateien werden beim Build in den Ausgabeordner nach `sounds\` neben
die EXE kopiert (siehe `PingPongStats.App.csproj`) - der Pfad zur Laufzeit
ist `{Installationsordner}\sounds\{Dateiname}.wav`, unabhängig vom
(potenziell netzwerkbasierten) Datenpfad.
