using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>Drives the first-run "choose a data folder" dialog.</summary>
public partial class DataPathSetupViewModel : ObservableObject
{
    private readonly DataPathService _dataPathService;
    private readonly IFolderPickerService _folderPicker;

    [ObservableProperty] private string selectedPath = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public IRelayCommand BrowseCommand { get; }
    public IRelayCommand ConfirmCommand { get; }

    /// <summary>Set to true once ConfirmCommand successfully validates and prepares the path.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>Raised once the path has been successfully validated and prepared, so the
    /// hosting Window can close itself with DialogResult = true.</summary>
    public event Action? ConfirmedSuccessfully;

    public DataPathSetupViewModel(DataPathService dataPathService, IFolderPickerService folderPicker, string? suggestedPath = null)
    {
        _dataPathService = dataPathService;
        _folderPicker = folderPicker;
        SelectedPath = suggestedPath ?? string.Empty;

        BrowseCommand = new RelayCommand(Browse);
        ConfirmCommand = new RelayCommand(Confirm);
    }

    private void Browse()
    {
        var picked = _folderPicker.PickFolder("Datenordner für PingPongStats auswählen", SelectedPath);
        if (!string.IsNullOrWhiteSpace(picked)) SelectedPath = picked;
    }

    private void Confirm()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            ErrorMessage = "Bitte einen Datenordner auswählen oder eingeben.";
            return;
        }

        try
        {
            _dataPathService.ValidateAndPrepare(SelectedPath);
            Confirmed = true;
            ConfirmedSuccessfully?.Invoke();
        }
        catch (DataPathUnavailableException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
