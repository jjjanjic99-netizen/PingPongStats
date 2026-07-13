using System.Security.Cryptography;
using System.Text;
using PingPongStats.Core.Models;

namespace PingPongStats.Core.Services;

/// <summary>
/// Pure, platform-independent avatar logic: file path conventions, initials,
/// and a deterministic fallback color derived from the player's id. Actual
/// image decoding/resizing/cropping requires a UI imaging stack and lives in
/// the WPF App project (PingPongStats.App.Services.AvatarImageService) -
/// there is nothing to "calculate" there, just platform IO.
/// </summary>
public static class AvatarService
{
    public const int MaxAvatarPixelSize = 512;

    /// <summary>Deterministic, evenly distributed hues for the fallback avatar
    /// background, picked by hashing the player id - same player always gets
    /// the same color, independent of insertion order. Exact palette from
    /// desktop/design/mockup.html's example avatars (soft pastels, readable
    /// with the dark #08222C initials text the mockup pairs them with).</summary>
    private static readonly string[] FallbackColors =
    {
        "#F2B25C", "#8FB8F0", "#7ED0C0", "#C6A0F5",
        "#F0A0B4", "#9FD98C", "#EFD07A", "#A8BCC4",
    };

    public static string GetAvatarsDirectory(string dataPath) => Path.Combine(dataPath, "avatars");

    /// <summary>The fixed file-name convention: "{PlayerId}.png". The full path
    /// this resolves to may or may not exist on disk yet.</summary>
    public static string GetAvatarFilePath(string dataPath, Guid playerId) =>
        Path.Combine(GetAvatarsDirectory(dataPath), $"{playerId}.png");

    /// <summary>Resolves the player's avatar file path only if the player actually
    /// has one recorded AND the file still exists on disk; otherwise null, so
    /// callers know to fall back to the initials avatar.</summary>
    public static string? TryResolveAvatarPath(string dataPath, Player player)
    {
        if (string.IsNullOrWhiteSpace(player.AvatarFileName)) return null;

        var path = GetAvatarFilePath(dataPath, player.Id);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Up to two initial letters for the fallback avatar, e.g. "Anna Berger"
    /// -&gt; "AB", "Anna" -&gt; "A", "" -&gt; "?".</summary>
    public static string GetInitials(string displayName)
    {
        var words = (displayName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0) return "?";
        if (words.Length == 1) return char.ToUpperInvariant(words[0][0]).ToString();

        return string.Concat(
            char.ToUpperInvariant(words[0][0]),
            char.ToUpperInvariant(words[^1][0]));
    }

    /// <summary>Deterministic hex color (from a small fixed, readable palette) for
    /// a player's fallback avatar background, derived from their id.</summary>
    public static string GetAvatarColorHex(Guid playerId)
    {
        // MD5 here purely as a fast, deterministic hash to pick a palette index -
        // not used for any security purpose.
        var hash = MD5.HashData(playerId.ToByteArray());
        var index = hash[0] % FallbackColors.Length;
        return FallbackColors[index];
    }
}
