using System.Windows;

namespace PingPongStats.App;

/// <summary>Swaps the theme ResourceDictionary (index 0 in App.xaml's
/// MergedDictionaries) between Light and Dark at runtime.</summary>
public static class ThemeManager
{
    public static void Apply(bool darkMode) => SetTheme(darkMode ? "Dark" : "Light");

    private static void SetTheme(string name)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var themeDictionary = new ResourceDictionary
        {
            Source = new Uri($"Themes/{name}.xaml", UriKind.Relative),
        };
        dictionaries[0] = themeDictionary;
    }
}
