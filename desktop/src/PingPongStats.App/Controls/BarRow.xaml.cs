using System.Windows;
using System.Windows.Controls;

namespace PingPongStats.App.Controls;

/// <summary>One labeled, gradient-filled bar (mockup .bar-row) - used for
/// "Siegquote nach Tageszeit" and similar hand-rolled bar lists. IsDim mirrors
/// .bar-row.dim (low-sample-size rows, shown greyed-out rather than hidden).</summary>
public partial class BarRow : UserControl
{
    public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
        nameof(LabelText), typeof(string), typeof(BarRow), new PropertyMetadata(string.Empty, OnLabelTextChanged));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(BarRow), new PropertyMetadata(string.Empty, OnValueTextChanged));

    public static readonly DependencyProperty NormalizedValueProperty = DependencyProperty.Register(
        nameof(NormalizedValue), typeof(double), typeof(BarRow), new PropertyMetadata(0.0, OnNormalizedValueChanged));

    public static readonly DependencyProperty IsDimProperty = DependencyProperty.Register(
        nameof(IsDim), typeof(bool), typeof(BarRow), new PropertyMetadata(false, OnIsDimChanged));

    public BarRow()
    {
        InitializeComponent();
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    /// <summary>0..1 fraction of the track the gradient fill covers.</summary>
    public double NormalizedValue
    {
        get => (double)GetValue(NormalizedValueProperty);
        set => SetValue(NormalizedValueProperty, value);
    }

    public bool IsDim
    {
        get => (bool)GetValue(IsDimProperty);
        set => SetValue(IsDimProperty, value);
    }

    private static void OnLabelTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BarRow)d).LabelBlock.Text = (string)e.NewValue;

    private static void OnValueTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BarRow)d).ValueBlock.Text = (string)e.NewValue;

    private static void OnIsDimChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BarRow)d).Opacity = (bool)e.NewValue ? 0.35 : 1.0;

    private static void OnNormalizedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (BarRow)d;
        var fraction = Math.Clamp((double)e.NewValue, 0.0, 1.0);
        // GridLength(0, Star) is valid but a hair above zero avoids any
        // corner-radius rendering artifact on a truly zero-width column.
        control.FillColumn.Width = new GridLength(Math.Max(fraction, 0.0001), GridUnitType.Star);
        control.RemainderColumn.Width = new GridLength(Math.Max(1.0 - fraction, 0.0001), GridUnitType.Star);
    }
}
