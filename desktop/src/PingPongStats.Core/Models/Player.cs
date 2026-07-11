namespace PingPongStats.Core.Models;

/// <summary>
/// A player. Optional text fields default to empty string (not null) so the
/// XML serialization always emits a predictable, empty element instead of
/// omitting it - matching the documented XML sample format.
/// </summary>
public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>File name (not full path) of the player's avatar image under
    /// {DataPath}/avatars/, e.g. "&lt;PlayerId&gt;.png". Empty = no avatar, fall
    /// back to initials.</summary>
    public string AvatarFileName { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash of the optional 4-digit login PIN, Base64-encoded.
    /// Empty = no PIN set, login for this player requires no PIN. This is a
    /// convenience gate, not real security - see README.</summary>
    public string PinHash { get; set; } = string.Empty;

    /// <summary>Base64-encoded random salt used with PinHash. Empty if no PIN set.</summary>
    public string PinSalt { get; set; } = string.Empty;

    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? string.Empty
            : $"{FirstName} {LastName}".Trim();
}
