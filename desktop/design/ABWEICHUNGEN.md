# Abweichungen vom Mockup

Diese Datei dokumentiert jede Stelle, an der `desktop/design/mockup.html`
nicht 1:1 in WPF umsetzbar war oder eine bewusste Interpretationsentscheidung
nötig war. Reihenfolge: chronologisch nach Phase (D1–D4).

## Phase D1 — Design-Tokens & Fonts

- **Kein Light-Theme**: Der Mockup definiert nur eine einzige, dunkle
  Farbpalette. Die App hatte bereits einen Hell/Dunkel-Umschalter
  (`SettingsViewModel.DarkMode`, `ThemeManager`). Um keine bestehende
  Funktionalität zu entfernen, bleibt der Schalter technisch erhalten,
  aber `Themes/Light.xaml` und `Themes/Dark.xaml` verweisen jetzt beide
  ausschliesslich auf dieselben `Themes/DesignTokens.xaml`-Werte - der
  Schalter hat also aktuell keinen sichtbaren Effekt mehr. Eine echte
  helle Variante müsste eigens entworfen werden; das war nicht Teil des
  Auftrags.
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
