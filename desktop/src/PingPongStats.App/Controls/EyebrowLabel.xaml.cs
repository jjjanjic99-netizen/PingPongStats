using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PingPongStats.App.Controls;

/// <summary>Small mono, uppercase, tracked card-section label (mockup
/// .eyebrow). Uppercasing happens here in code since WPF has no declarative
/// text-transform for arbitrary bound text.</summary>
public partial class EyebrowLabel : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(EyebrowLabel),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public EyebrowLabel()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EyebrowLabel)d;
        control.Label.Text = ((string)e.NewValue ?? string.Empty).ToUpper(CultureInfo.CurrentCulture);
    }
}
