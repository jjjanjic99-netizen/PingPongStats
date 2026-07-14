using System.Windows;
using System.Windows.Controls;

namespace PingPongStats.App.Controls;

/// <summary>Mockup's .card surface (Panel fill, 1px Line border, 12px radius,
/// signature bottom "table line" gradient) - see the implicit Style/
/// ControlTemplate in Themes/Controls.xaml for the visual tree.
///
/// Deliberately NOT a UserControl. A UserControl has its own compiled XAML
/// page and establishes its own XAML namescope at construction; nesting one
/// UserControl (e.g. EyebrowLabel) with an x:Name inside another
/// UserControl's content (e.g. CardControl, as StatTile.xaml did) collides
/// with that namescope and fails to compile (MC3093). A lookless Control
/// with only a ControlTemplate has no such per-instance namescope, so this
/// class of bug cannot occur. Content is ContentControl's own built-in
/// property, so every existing
/// &lt;controls:CardControl&gt;...&lt;/controls:CardControl&gt; call site
/// needs no changes.</summary>
public class CardControl : ContentControl
{
    static CardControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CardControl), new FrameworkPropertyMetadata(typeof(CardControl)));
    }
}
