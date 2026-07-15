using System.Windows.Media;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Models;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.ViewModels;
using System.IO;

namespace PingPongStats.App.Services;

/// <summary>
/// Plays short WAV sound effects via WPF's MediaPlayer (no external library).
/// Files are expected under {AppContext.BaseDirectory}/sounds/ (see
/// desktop/assets/sounds/ in the repo, copied there at build time). A missing
/// or unplayable file is silently skipped - never a crash, never a UI-thread
/// block (MediaPlayer.Play() is asynchronous).
/// </summary>
public class WpfSoundService : ISoundService
{
    private static readonly Dictionary<SoundEvent, string> FileNames = new()
    {
        [SoundEvent.Win] = "win.wav",
        [SoundEvent.TournamentWin] = "tournament-win.wav",
        [SoundEvent.BadgeEarned] = "badge-earned.wav",
    };

    private readonly ISettingsRepository _settingsRepository;

    // MediaPlayer instances must be kept alive (rooted) until playback ends,
    // otherwise WPF's media pipeline can stop them mid-playback once the GC
    // collects an otherwise-unreferenced local variable.
    private readonly List<MediaPlayer> _activePlayers = new();

    public WpfSoundService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public void PlaySound(SoundEvent soundEvent)
    {
        try
        {
            var settings = _settingsRepository.Load();
            if (!settings.SoundEnabled) return;
            if (!FileNames.TryGetValue(soundEvent, out var fileName)) return;

            var path = Path.Combine(AppContext.BaseDirectory, "sounds", fileName);
            if (!File.Exists(path)) return;

            var player = new MediaPlayer { Volume = Math.Clamp(settings.SoundVolume, 0.0, 1.0) };

            void Cleanup(object? sender, EventArgs e)
            {
                player.Close();
                lock (_activePlayers) _activePlayers.Remove(player);
            }

            player.MediaEnded += Cleanup;
            player.MediaFailed += (sender, e) => Cleanup(sender, e);
            // (MediaFailed's ExceptionEventArgs is itself an EventArgs, so the
            // Cleanup(object?, EventArgs) local function above accepts it directly.)

            lock (_activePlayers) _activePlayers.Add(player);

            player.Open(new Uri(path, UriKind.Absolute));
            player.Play();
        }
        catch (Exception ex)
        {
            // Sound is a nice-to-have, never allowed to disrupt the actual feature
            // (saving a match, awarding a badge, ...) it accompanies.
            Logger.Warn($"Sound-Effekt konnte nicht abgespielt werden ({soundEvent}): {ex.Message}");
        }
    }
}
