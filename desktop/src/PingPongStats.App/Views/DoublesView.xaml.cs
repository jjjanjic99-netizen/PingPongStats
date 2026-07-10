using System.Windows;
using System.Windows.Controls;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class DoublesView : UserControl
{
    public DoublesView()
    {
        InitializeComponent();
    }

    private DoublesViewModel? ViewModel => DataContext as DoublesViewModel;

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DoubleMatchRow row } || ViewModel is not { } vm) return;

        var result = MessageBox.Show(
            $"Doppel-Spiel {row.TeamALabel} vs. {row.TeamBLabel} ({row.ResultLabel}) wirklich löschen?",
            "Doppel-Spiel löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            vm.DeleteCommand.Execute(row);
        }
    }
}
