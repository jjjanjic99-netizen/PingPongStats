using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PingPongStats.Core.Models;

namespace PingPongStats.App.Controls;

/// <summary>One row of a simple ranked/labeled list (mockup .row) - see
/// RowItem.xaml for the exact layout and its documented scope.</summary>
public partial class RowItem : UserControl
{
    public static readonly DependencyProperty PosProperty = DependencyProperty.Register(
        nameof(Pos), typeof(string), typeof(RowItem), new PropertyMetadata(string.Empty, OnPosChanged));

    public static readonly DependencyProperty AvatarPlayerProperty = DependencyProperty.Register(
        nameof(AvatarPlayer), typeof(Player), typeof(RowItem), new PropertyMetadata(null));

    public static readonly DependencyProperty AvatarDataPathProperty = DependencyProperty.Register(
        nameof(AvatarDataPath), typeof(string), typeof(RowItem), new PropertyMetadata(null));

    public static readonly DependencyProperty NameTextProperty = DependencyProperty.Register(
        nameof(NameText), typeof(string), typeof(RowItem), new PropertyMetadata(string.Empty, OnNameTextChanged));

    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(RowItem), new PropertyMetadata(string.Empty, OnValueTextChanged));

    public static readonly DependencyProperty TrailingContentProperty = DependencyProperty.Register(
        nameof(TrailingContent), typeof(object), typeof(RowItem), new PropertyMetadata(null));

    public static readonly DependencyProperty ShowDividerProperty = DependencyProperty.Register(
        nameof(ShowDivider), typeof(bool), typeof(RowItem), new PropertyMetadata(true, OnShowDividerChanged));

    public static readonly DependencyProperty ValueBrushProperty = DependencyProperty.Register(
        nameof(ValueBrush), typeof(Brush), typeof(RowItem), new PropertyMetadata(null, OnValueBrushChanged));

    public RowItem()
    {
        InitializeComponent();
    }

    /// <summary>Optional leading rank/label (e.g. "1", "Q2/26"). Empty leaves
    /// the column blank rather than collapsing it, for consistent alignment
    /// across rows in the same list.</summary>
    public string Pos
    {
        get => (string)GetValue(PosProperty);
        set => SetValue(PosProperty, value);
    }

    public Player? AvatarPlayer
    {
        get => (Player?)GetValue(AvatarPlayerProperty);
        set => SetValue(AvatarPlayerProperty, value);
    }

    public string? AvatarDataPath
    {
        get => (string?)GetValue(AvatarDataPathProperty);
        set => SetValue(AvatarDataPathProperty, value);
    }

    public string NameText
    {
        get => (string)GetValue(NameTextProperty);
        set => SetValue(NameTextProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    /// <summary>Optional content (e.g. a Pill) shown between the name and the
    /// value column.</summary>
    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    /// <summary>Set false on the last row of a list to match the mockup's
    /// .row:last-child{border-bottom:none}.</summary>
    public bool ShowDivider
    {
        get => (bool)GetValue(ShowDividerProperty);
        set => SetValue(ShowDividerProperty, value);
    }

    /// <summary>Optional override for the value column's color (mockup's
    /// .row-val.up/.down) - e.g. Win/Lose brush for an Elo trend. Left
    /// null/unset for the default Chalk color.</summary>
    public Brush? ValueBrush
    {
        get => (Brush?)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    private static void OnPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RowItem)d).PosBlock.Text = (string)e.NewValue;

    private static void OnNameTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RowItem)d).NameBlock.Text = (string)e.NewValue;

    private static void OnValueTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RowItem)d).ValueBlock.Text = (string)e.NewValue;

    private static void OnShowDividerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RowItem)d).Divider.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

    private static void OnValueBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RowItem)d;
        control.ValueBlock.Foreground = (Brush?)e.NewValue
            ?? (Brush)Application.Current.Resources["Brush.Chalk"];
    }
}
