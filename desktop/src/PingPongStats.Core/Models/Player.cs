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

    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? string.Empty
            : $"{FirstName} {LastName}".Trim();
}
