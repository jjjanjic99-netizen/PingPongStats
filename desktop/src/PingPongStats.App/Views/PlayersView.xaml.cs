using System.Windows;
using System.Windows.Controls;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class PlayersView : UserControl
{
    public PlayersView()
    {
        InitializeComponent();
    }

    private PlayersViewModel? ViewModel => DataContext as PlayersViewModel;

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PlayerRow row } && ViewModel is { } vm)
        {
            vm.EditPlayerCommand.Execute(row);
        }
    }

    private void OnToggleActiveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PlayerRow row } && ViewModel is { } vm)
        {
            vm.ToggleActiveCommand.Execute(row);
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PlayerRow row } || ViewModel is not { } vm) return;

        var result = MessageBox.Show(
            $"Spieler \"{row.DisplayName}\" wirklich löschen? Das ist nur möglich, wenn keine Spiele mit diesem Spieler erfasst wurden.",
            "Spieler löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            vm.DeleteCommand.Execute(row);
        }
    }
}
