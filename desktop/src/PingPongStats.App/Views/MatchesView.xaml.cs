using System.Windows;
using System.Windows.Controls;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class MatchesView : UserControl
{
    public MatchesView()
    {
        InitializeComponent();
    }

    private MatchesViewModel? ViewModel => DataContext as MatchesViewModel;

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MatchRow row } && ViewModel is { } vm)
        {
            vm.EditCommand.Execute(row);
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: MatchRow row } || ViewModel is not { } vm) return;

        var result = MessageBox.Show(
            $"Spiel {row.PlayerAName} vs. {row.PlayerBName} ({row.ResultLabel}) wirklich löschen?",
            "Spiel löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            vm.DeleteCommand.Execute(row);
        }
    }
}
