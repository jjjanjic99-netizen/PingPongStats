using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PingPongStats.Core.Models;
using PingPongStats.Core.Services;

namespace PingPongStats.App.Controls;

/// <summary>
/// Reusable player avatar: shows the saved avatar image if one resolves, otherwise
/// a colored circle with initials. Used in lists, the dashboard ranking, the player
/// edit dialog (live preview via OverrideImagePath) and the win confetti overlay.
/// </summary>
public partial class AvatarControl : UserControl
{
    public static readonly DependencyProperty PlayerProperty = DependencyProperty.Register(
        nameof(Player), typeof(Player), typeof(AvatarControl),
        new PropertyMetadata(null, OnAnyRelevantPropertyChanged));

    public static readonly DependencyProperty DataPathProperty = DependencyProperty.Register(
        nameof(DataPath), typeof(string), typeof(AvatarControl),
        new PropertyMetadata(null, OnAnyRelevantPropertyChanged));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(string), typeof(AvatarControl),
        new PropertyMetadata("M", OnAnyRelevantPropertyChanged));

    /// <summary>Absolute path to a not-yet-saved image file, used for live preview in
    /// the edit dialog before Save persists it via IAvatarImageService. Takes priority
    /// over the Player's stored AvatarFileName when set.</summary>
    public static readonly DependencyProperty OverrideImagePathProperty = DependencyProperty.Register(
        nameof(OverrideImagePath), typeof(string), typeof(AvatarControl),
        new PropertyMetadata(null, OnAnyRelevantPropertyChanged));

    public AvatarControl()
    {
        InitializeComponent();
    }

    public Player? Player
    {
        get => (Player?)GetValue(PlayerProperty);
        set => SetValue(PlayerProperty, value);
    }

    public string? DataPath
    {
        get => (string?)GetValue(DataPathProperty);
        set => SetValue(DataPathProperty, value);
    }

    public string Size
    {
        get => (string)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public string? OverrideImagePath
    {
        get => (string?)GetValue(OverrideImagePathProperty);
        set => SetValue(OverrideImagePathProperty, value);
    }

    private static void OnAnyRelevantPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AvatarControl)d).Refresh();
    }

    private void Refresh()
    {
        var diameter = Size switch
        {
            "S" => 32.0,
            "L" => 96.0,
            _ => 48.0,
        };
        var fontSize = Size switch
        {
            "S" => 12.0,
            "L" => 32.0,
            _ => 16.0,
        };

        RootGrid.Width = diameter;
        RootGrid.Height = diameter;
        FallbackEllipse.Width = diameter;
        FallbackEllipse.Height = diameter;
        ImageEllipse.Width = diameter;
        ImageEllipse.Height = diameter;
        InitialsText.FontSize = fontSize;

        var player = Player;
        var displayName = player?.DisplayName ?? string.Empty;
        InitialsText.Text = AvatarService.GetInitials(displayName);

        var colorHex = player is not null
            ? AvatarService.GetAvatarColorHex(player.Id)
            : "#94A3B8";
        FallbackEllipse.Fill = (Brush)new BrushConverter().ConvertFromString(colorHex)!;

        string? resolvedPath = null;
        if (!string.IsNullOrWhiteSpace(OverrideImagePath))
        {
            resolvedPath = OverrideImagePath;
        }
        else if (player is not null && !string.IsNullOrWhiteSpace(DataPath))
        {
            resolvedPath = AvatarService.TryResolveAvatarPath(DataPath, player);
        }

        if (resolvedPath is not null)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(resolvedPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            AvatarImageBrush.ImageSource = bitmap;
            ImageEllipse.Visibility = Visibility.Visible;
            FallbackEllipse.Visibility = Visibility.Collapsed;
            InitialsText.Visibility = Visibility.Collapsed;
        }
        else
        {
            ImageEllipse.Visibility = Visibility.Collapsed;
            FallbackEllipse.Visibility = Visibility.Visible;
            InitialsText.Visibility = Visibility.Visible;
        }
    }
}
