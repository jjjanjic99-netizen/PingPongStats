using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PingPongStats.App.Controls;

/// <summary>Standalone KPI card: eyebrow label, one big number, optional small
/// caption below (mockup .card.center). ValueBrush lets a caller highlight the
/// number (e.g. Ball orange for a win-rate, Win green for a set difference) -
/// left null/unset for the default Chalk color.</summary>
public partial class StatTile : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty, OnValueChanged));

    public static readonly DependencyProperty SubTextProperty = DependencyProperty.Register(
        nameof(SubText), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty, OnSubTextChanged));

    public static readonly DependencyProperty ValueBrushProperty = DependencyProperty.Register(
        nameof(ValueBrush), typeof(Brush), typeof(StatTile), new PropertyMetadata(null, OnValueBrushChanged));

    public StatTile()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string SubText
    {
        get => (string)GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public Brush? ValueBrush
    {
        get => (Brush?)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatTile)d).EyebrowControl.Text = ((string)e.NewValue).ToUpper(CultureInfo.CurrentCulture);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatTile)d).ValueText.Text = (string)e.NewValue;

    private static void OnSubTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatTile)d).SubTextBlock.Text = (string)e.NewValue;

    private static void OnValueBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StatTile)d;
        control.ValueText.Foreground = (Brush?)e.NewValue
            ?? (Brush)Application.Current.Resources["Brush.Chalk"];
    }
}
