namespace PingPongStats.Core.Models;

public enum StreakType
{
    None,
    Win,
    Loss,
}

/// <summary>Sound-effect events (Phase 14) a ViewModel can request via
/// ISoundService. Mapped to specific WAV files by the WPF-side implementation.</summary>
public enum SoundEvent
{
    Win,
    TournamentWin,
    BadgeEarned,
}
