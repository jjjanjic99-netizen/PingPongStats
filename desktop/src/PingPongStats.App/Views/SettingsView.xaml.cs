using System.Windows;
using System.Windows.Controls;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnSeedDataClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        var result = MessageBox.Show(
            "Dies ersetzt ALLE bestehenden Spieler und Spiele am aktuellen Datenpfad durch generierte Testdaten. " +
            "Diese Aktion kann nicht rückgängig gemacht werden (ein Backup der vorherigen Dateien wird aber automatisch angelegt). Fortfahren?",
            "Testdaten erzeugen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            vm.SeedDataCommand.Execute(null);
        }
    }
}
