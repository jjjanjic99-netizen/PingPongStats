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
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.DarkMode) && sender is SettingsViewModel settingsViewModel)
        {
            ThemeManager.Apply(settingsViewModel.DarkMode);
        }
    }
}
