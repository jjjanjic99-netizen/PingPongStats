using System.Windows;
using System.Windows.Controls;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class TournamentView : UserControl
{
    public TournamentView()
    {
        InitializeComponent();
    }

    private TournamentViewModel? ViewModel => DataContext as TournamentViewModel;

    private void OnAbortClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;

        var result = MessageBox.Show(
            "Laufendes Turnier wirklich abbrechen? Bereits gespielte Partien bleiben in der Statistik erhalten.",
            "Turnier abbrechen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            vm.AbortTournamentCommand.Execute(null);
        }
    }
}
