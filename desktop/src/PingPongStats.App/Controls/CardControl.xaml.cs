using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PingPongStats.App.Controls;

/// <summary>Panel-surfaced card with the app's signature bottom "table line" -
/// see CardControl.xaml. Used as a drop-in replacement for a plain
/// Border+CardBorder-style wherever the extra visual is wanted.
///
/// Exposes its passed-in child content as <see cref="CardContent"/> rather
/// than the inherited UserControl.Content: CardControl.xaml's own root
/// element already uses Content for its own visual tree (the Border/line
/// decoration), so a caller-supplied child would silently overwrite that
/// tree instead of flowing into the inner ContentPresenter. ContentProperty
/// redirects the implicit-child-element XAML syntax
/// (&lt;controls:CardControl&gt;...&lt;/controls:CardControl&gt;) to
/// CardContent, so call sites don't need to name the property explicitly.</summary>
[ContentProperty(nameof(CardContent))]
public partial class CardControl : UserControl
{
    public static readonly DependencyProperty CardContentProperty = DependencyProperty.Register(
        nameof(CardContent), typeof(object), typeof(CardControl), new PropertyMetadata(null));

    public CardControl()
    {
        InitializeComponent();
    }

    public object? CardContent
    {
        get => GetValue(CardContentProperty);
        set => SetValue(CardContentProperty, value);
    }
}
