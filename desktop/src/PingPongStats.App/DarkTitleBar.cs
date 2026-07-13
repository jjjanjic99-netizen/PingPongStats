using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PingPongStats.App;

/// <summary>
/// Phase D2: the mockup calls for a dark window frame. A fully custom
/// (WindowStyle="None" + WindowChrome) titlebar was judged too risky to ship
/// un-tested from this environment (no Windows machine available to verify
/// drag/resize/snap/multi-monitor behavior) - see desktop/design/ABWEICHUNGEN.md.
/// Instead this tints the *native* Windows 10/11 titlebar dark via the
/// documented DWM "immersive dark mode" attribute, leaving all standard
/// chrome (minimize/maximize/close, resize, Aero Snap) exactly as before.
/// A no-op (silently ignored) on Windows versions that don't support the
/// attribute - never throws.
/// </summary>
public static class DarkTitleBar
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // pre-20H1 builds

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Call once the window's handle exists (SourceInitialized).</summary>
    public static void Apply(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var useDark = 1;
            var result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            if (result != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));
            }
        }
        catch
        {
            // Best-effort cosmetic touch only - never allowed to affect startup.
        }
    }
}
