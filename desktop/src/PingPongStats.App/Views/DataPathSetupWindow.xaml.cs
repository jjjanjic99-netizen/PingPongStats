using System.Windows;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App.Views;

public partial class DataPathSetupWindow : Window
{
    public DataPathSetupWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DataPathSetupViewModel oldViewModel)
        {
            oldViewModel.ConfirmedSuccessfully -= OnConfirmedSuccessfully;
        }

        if (e.NewValue is DataPathSetupViewModel newViewModel)
        {
            newViewModel.ConfirmedSuccessfully += OnConfirmedSuccessfully;
        }
    }

    private void OnConfirmedSuccessfully()
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
