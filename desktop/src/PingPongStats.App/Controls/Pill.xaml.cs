using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PingPongStats.App.Controls;

/// <summary>Small rounded badge (mockup .pill). Variant "Win"/"Bad" recolors
/// it green/red (e.g. Angstgegner/Lieblingsgegner emoji pills); any other
/// value (including the default, unset) stays the neutral muted look.</summary>
public partial class Pill : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(Pill), new PropertyMetadata(string.Empty, OnAnyChanged));

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(string), typeof(Pill), new PropertyMetadata("Default", OnAnyChanged));

    public Pill()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>"Default", "Win", or "Bad".</summary>
    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Pill)d).Refresh();

    private void Refresh()
    {
        Label.Text = Text;

        var resources = Application.Current.Resources;
        switch (Variant)
        {
            case "Win":
                Label.Foreground = resources["Brush.Win"] as Brush ?? Label.Foreground;
                Bd.BorderBrush = new SolidColorBrush(Color.FromArgb(89, 63, 207, 142)); // rgba(63,207,142,.35)
                break;
            case "Bad":
                Label.Foreground = resources["Brush.Lose"] as Brush ?? Label.Foreground;
                Bd.BorderBrush = new SolidColorBrush(Color.FromArgb(89, 228, 103, 94)); // rgba(228,103,94,.35)
                break;
            default:
                Label.Foreground = resources["Brush.Muted"] as Brush ?? Label.Foreground;
                Bd.BorderBrush = resources["Brush.Line"] as Brush ?? Bd.BorderBrush;
                break;
        }
    }
}
