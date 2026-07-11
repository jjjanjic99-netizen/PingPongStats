using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;

namespace PingPongStats.Core.ViewModels;

/// <summary>
/// Start-screen tile login: pick a player, enter their PIN if they have one set.
/// This is convenience (avoids accidentally opening someone else's profile), not
/// real security - anyone with file access to the data folder can read/edit it
/// directly regardless of PIN.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly PingPongDataService _dataService;
    private readonly ISettingsRepository _settingsRepository;

    public string DataPath => _settingsRepository.Load().DataPath;

    public ObservableCollection<Player> Tiles { get; } = new();

    [ObservableProperty] private Player? pinPromptPlayer;
    [ObservableProperty] private string pinInput = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public bool IsPinPromptOpen => PinPromptPlayer is not null;

    public IRelayCommand<Player> SelectPlayerCommand { get; }
    public IRelayCommand SubmitPinCommand { get; }
    public IRelayCommand CancelPinCommand { get; }

    /// <summary>Raised with the chosen player's Id once login succeeds (no PIN set,
    /// or the correct PIN was entered).</summary>
    public event Action<Guid>? LoggedIn;

    public LoginViewModel(PingPongDataService dataService, ISettingsRepository settingsRepository)
    {
        _dataService = dataService;
        _settingsRepository = settingsRepository;

        SelectPlayerCommand = new RelayCommand<Player>(p => { if (p is not null) SelectPlayer(p); });
        SubmitPinCommand = new RelayCommand(SubmitPin);
        CancelPinCommand = new RelayCommand(ResetPinPrompt);

        Load();
    }

    partial void OnPinPromptPlayerChanged(Player? value) => OnPropertyChanged(nameof(IsPinPromptOpen));

    public void Load()
    {
        Tiles.Clear();
        foreach (var p in _dataService.Players.Where(p => p.IsActive).OrderBy(p => p.DisplayName))
        {
            Tiles.Add(p);
        }

        ResetPinPrompt();
    }

    private void ResetPinPrompt()
    {
        PinPromptPlayer = null;
        PinInput = string.Empty;
        ErrorMessage = string.Empty;
    }

    private void SelectPlayer(Player player)
    {
        ErrorMessage = string.Empty;

        if (_dataService.PlayerHasPin(player.Id))
        {
            PinPromptPlayer = player;
            PinInput = string.Empty;
        }
        else
        {
            LoggedIn?.Invoke(player.Id);
        }
    }

    private void SubmitPin()
    {
        if (PinPromptPlayer is null) return;

        if (_dataService.VerifyPlayerPin(PinPromptPlayer.Id, PinInput))
        {
            var playerId = PinPromptPlayer.Id;
            ResetPinPrompt();
            LoggedIn?.Invoke(playerId);
        }
        else
        {
            ErrorMessage = "Falscher PIN.";
            PinInput = string.Empty;
        }
    }
}
