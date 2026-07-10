using System.ComponentModel;
using System.Windows;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App;

public partial class MainWindow : Window
{
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
}
