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
    // Ping-pong-ball confetti colors (Phase D4, mockup .pball): orange balls
    // with a light highlight, ~15% plain white ones mixed in - not app
    // branding data, so kept local to the view rather than in Core.
    private static readonly Color BallHighlight = (Color)ColorConverter.ConvertFromString("#FFD7AC")!;
    private static readonly Color BallBase = (Color)ColorConverter.ConvertFromString("#FF7A1A")!;
    private static readonly Color WhiteBallHighlight = Colors.White;
    private static readonly Color WhiteBallBase = (Color)ColorConverter.ConvertFromString("#E7ECEC")!;

    private readonly Random _confettiRandom = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
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

        if (e.PropertyName == nameof(SettingsViewModel.SelectedUiScale))
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

    /// <summary>Chance (mockup: "~15%") that a falling ball is plain white
    /// instead of the usual orange.</summary>
    private const double WhiteBallChance = 0.15;

    /// <summary>Pure WPF/XAML confetti: no external animation library. Each ball
    /// falls/spins via RenderTransform DoubleAnimations (composition thread, not
    /// layout), so this doesn't block the UI thread. If frame drops show up, lower
    /// ParticleCount first rather than touching the fall/rotation logic. Balls are
    /// Ellipses with a radial-gradient highlight (mockup .pball), not the plain
    /// rectangles this used before Phase D4.</summary>
    private void StartConfetti()
    {
        ConfettiCanvas.Children.Clear();

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        for (var i = 0; i < ParticleCount; i++)
        {
            var size = _confettiRandom.Next(12, 22);
            var isWhite = _confettiRandom.NextDouble() < WhiteBallChance;
            var ball = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new RadialGradientBrush(
                    isWhite ? WhiteBallHighlight : BallHighlight,
                    isWhite ? WhiteBallBase : BallBase)
                {
                    Center = new Point(0.32, 0.3),
                    GradientOrigin = new Point(0.32, 0.3),
                    RadiusX = 0.75,
                    RadiusY = 0.75,
                },
            };

            var startX = _confettiRandom.NextDouble() * width;
            var startY = -20.0 - _confettiRandom.Next(0, 200);
            Canvas.SetLeft(ball, startX);
            Canvas.SetTop(ball, startY);

            var translate = new TranslateTransform();
            var rotate = new RotateTransform(0, size / 2.0, size / 2.0);
            ball.RenderTransform = new TransformGroup { Children = { translate, rotate } };

            ConfettiCanvas.Children.Add(ball);

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
