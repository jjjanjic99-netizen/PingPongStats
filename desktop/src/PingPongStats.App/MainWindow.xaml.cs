using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App;

public partial class MainWindow : Window
{
    // Purely presentational (Phase 6 win overlay); not app branding, so kept local
    // to the view rather than in Core.
    private static readonly string[] ConfettiColors =
        { "#F87171", "#FBBF24", "#34D399", "#60A5FA", "#A78BFA", "#F472B6" };

    private readonly Random _confettiRandom = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldViewModel)
        {
            oldViewModel.Settings.PropertyChanged -= OnSettingsPropertyChanged;
        }

        if (e.NewValue is MainViewModel newViewModel)
        {
            newViewModel.Settings.PropertyChanged += OnSettingsPropertyChanged;
            ApplyUiScale(newViewModel.Settings.SelectedUiScale);
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SettingsViewModel settingsViewModel) return;

        if (e.PropertyName == nameof(SettingsViewModel.DarkMode))
        {
            ThemeManager.Apply(settingsViewModel.DarkMode);
        }
        else if (e.PropertyName == nameof(SettingsViewModel.SelectedUiScale))
        {
            ApplyUiScale(settingsViewModel.SelectedUiScale);
        }
    }

    private void ApplyUiScale(string uiScale)
    {
        var factor = UiScaleManager.ToScaleFactor(uiScale);
        RootScaleTransform.ScaleX = factor;
        RootScaleTransform.ScaleY = factor;
    }

    private void OnWinAnimationOverlayClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.DismissWinAnimationCommand.Execute(null);
    }

    private void OnWinAnimationOverlayIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue) StartConfetti();
        else ConfettiCanvas.Children.Clear();
    }

    private const int ParticleCount = 60;

    /// <summary>Pure WPF/XAML confetti: no external animation library. Each piece
    /// falls/spins via RenderTransform DoubleAnimations (composition thread, not
    /// layout), so this doesn't block the UI thread. If frame drops show up, lower
    /// ParticleCount first rather than touching the fall/rotation logic.</summary>
    private void StartConfetti()
    {
        ConfettiCanvas.Children.Clear();

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        for (var i = 0; i < ParticleCount; i++)
        {
            var size = _confettiRandom.Next(6, 12);
            var piece = new Rectangle
            {
                Width = size,
                Height = size * 0.6,
                RadiusX = 1,
                RadiusY = 1,
                Fill = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(ConfettiColors[_confettiRandom.Next(ConfettiColors.Length)])!),
            };

            var startX = _confettiRandom.NextDouble() * width;
            var startY = -20.0 - _confettiRandom.Next(0, 200);
            Canvas.SetLeft(piece, startX);
            Canvas.SetTop(piece, startY);

            var translate = new TranslateTransform();
            var rotate = new RotateTransform(0, size / 2.0, size / 2.0);
            piece.RenderTransform = new TransformGroup { Children = { translate, rotate } };

            ConfettiCanvas.Children.Add(piece);

            var duration = TimeSpan.FromSeconds(1.6 + _confettiRandom.NextDouble() * 1.2);
            var delay = TimeSpan.FromSeconds(_confettiRandom.NextDouble() * 0.4);
            var fallDistance = height - startY + 40;
            var spinDegrees = _confettiRandom.Next(180, 720) * (_confettiRandom.Next(2) == 0 ? 1 : -1);

            var fallAnimation = new DoubleAnimation(0, fallDistance, duration) { BeginTime = delay };
            var spinAnimation = new DoubleAnimation(0, spinDegrees, duration) { BeginTime = delay };

            translate.BeginAnimation(TranslateTransform.YProperty, fallAnimation);
            rotate.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);
        }
    }
}
