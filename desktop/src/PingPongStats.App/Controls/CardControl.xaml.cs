using System.Windows.Controls;

namespace PingPongStats.App.Controls;

/// <summary>Panel-surfaced card with the app's signature bottom "table line" -
/// see CardControl.xaml. Used as a drop-in replacement for a plain
/// Border+CardBorder-style wherever the extra visual is wanted.</summary>
public partial class CardControl : UserControl
{
    public CardControl()
    {
        InitializeComponent();
    }
}
